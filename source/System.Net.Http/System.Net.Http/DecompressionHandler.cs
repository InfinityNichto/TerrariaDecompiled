using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class DecompressionHandler : HttpMessageHandlerStage
{
	private abstract class DecompressedContent : HttpContent
	{
		private readonly HttpContent _originalContent;

		private bool _contentConsumed;

		internal override bool AllowDuplex => false;

		public DecompressedContent(HttpContent originalContent)
		{
			_originalContent = originalContent;
			_contentConsumed = false;
			base.Headers.AddHeaders(originalContent.Headers);
			base.Headers.ContentLength = null;
			base.Headers.ContentEncoding.Clear();
			string text = null;
			foreach (string item in originalContent.Headers.ContentEncoding)
			{
				if (text != null)
				{
					base.Headers.ContentEncoding.Add(text);
				}
				text = item;
			}
		}

		protected abstract Stream GetDecompressedStream(Stream originalStream);

		protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken)
		{
			using Stream stream2 = CreateContentReadStream(cancellationToken);
			stream2.CopyTo(stream);
		}

		protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
		{
			return SerializeToStreamAsync(stream, context, CancellationToken.None);
		}

		protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
		{
			Stream stream2 = TryCreateContentReadStream();
			Stream stream3 = stream2;
			if (stream3 == null)
			{
				stream3 = await CreateContentReadStreamAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			using Stream decompressedStream = stream3;
			await decompressedStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}

		protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
		{
			return CreateContentReadStreamAsyncCore(async: false, cancellationToken).GetAwaiter().GetResult();
		}

		protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
		{
			return CreateContentReadStreamAsyncCore(async: true, cancellationToken).AsTask();
		}

		private async ValueTask<Stream> CreateContentReadStreamAsyncCore(bool async, CancellationToken cancellationToken)
		{
			if (_contentConsumed)
			{
				throw new InvalidOperationException(System.SR.net_http_content_stream_already_read);
			}
			_contentConsumed = true;
			Stream originalStream;
			if (async)
			{
				Stream stream = _originalContent.TryReadAsStream();
				Stream stream2 = stream;
				if (stream2 == null)
				{
					stream2 = await _originalContent.ReadAsStreamAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				originalStream = stream2;
			}
			else
			{
				originalStream = _originalContent.ReadAsStream(cancellationToken);
			}
			return GetDecompressedStream(originalStream);
		}

		internal override Stream TryCreateContentReadStream()
		{
			Stream stream = _originalContent.TryReadAsStream();
			if (stream != null)
			{
				return GetDecompressedStream(stream);
			}
			return null;
		}

		protected internal override bool TryComputeLength(out long length)
		{
			length = 0L;
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_originalContent.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	private sealed class GZipDecompressedContent : DecompressedContent
	{
		public GZipDecompressedContent(HttpContent originalContent)
			: base(originalContent)
		{
		}

		protected override Stream GetDecompressedStream(Stream originalStream)
		{
			return new GZipStream(originalStream, CompressionMode.Decompress);
		}
	}

	private sealed class DeflateDecompressedContent : DecompressedContent
	{
		private sealed class ZLibOrDeflateStream : HttpBaseStream
		{
			private sealed class PeekFirstByteReadStream : HttpBaseStream
			{
				private enum FirstByteStatus : byte
				{
					None,
					Available,
					Consumed
				}

				private readonly Stream _stream;

				private byte _firstByte;

				private FirstByteStatus _firstByteStatus;

				public override bool CanRead => true;

				public override bool CanWrite => false;

				public PeekFirstByteReadStream(Stream stream)
				{
					_stream = stream;
				}

				protected override void Dispose(bool disposing)
				{
					if (disposing)
					{
						_stream.Dispose();
					}
					base.Dispose(disposing);
				}

				public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
				{
					throw new NotSupportedException();
				}

				public int PeekFirstByte()
				{
					int num = _stream.ReadByte();
					if (num == -1)
					{
						_firstByteStatus = FirstByteStatus.Consumed;
						return -1;
					}
					_firstByte = (byte)num;
					_firstByteStatus = FirstByteStatus.Available;
					return num;
				}

				public async ValueTask<int> PeekFirstByteAsync(CancellationToken cancellationToken)
				{
					byte[] buffer = new byte[1];
					if (await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false) == 0)
					{
						_firstByteStatus = FirstByteStatus.Consumed;
						return -1;
					}
					_firstByte = buffer[0];
					_firstByteStatus = FirstByteStatus.Available;
					return buffer[0];
				}

				public override int Read(Span<byte> buffer)
				{
					if (_firstByteStatus == FirstByteStatus.Available)
					{
						if (buffer.Length != 0)
						{
							buffer[0] = _firstByte;
							_firstByteStatus = FirstByteStatus.Consumed;
							return 1;
						}
						return 0;
					}
					return _stream.Read(buffer);
				}

				public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
				{
					if (_firstByteStatus == FirstByteStatus.Available)
					{
						if (buffer.Length != 0)
						{
							buffer.Span[0] = _firstByte;
							_firstByteStatus = FirstByteStatus.Consumed;
							return new ValueTask<int>(1);
						}
						return new ValueTask<int>(0);
					}
					return _stream.ReadAsync(buffer, cancellationToken);
				}

				public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
				{
					Stream.ValidateCopyToArguments(destination, bufferSize);
					if (_firstByteStatus == FirstByteStatus.Available)
					{
						await destination.WriteAsync(new byte[1] { _firstByte }, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						_firstByteStatus = FirstByteStatus.Consumed;
					}
					await _stream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}

			private readonly PeekFirstByteReadStream _stream;

			private Stream _decompressionStream;

			public override bool CanRead => true;

			public override bool CanWrite => false;

			public ZLibOrDeflateStream(Stream stream)
			{
				_stream = new PeekFirstByteReadStream(stream);
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					_decompressionStream?.Dispose();
					_stream.Dispose();
				}
				base.Dispose(disposing);
			}

			public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
			{
				throw new NotSupportedException();
			}

			public override int Read(Span<byte> buffer)
			{
				if (_decompressionStream == null)
				{
					int firstByte = _stream.PeekFirstByte();
					_decompressionStream = CreateDecompressionStream(firstByte, _stream);
				}
				return _decompressionStream.Read(buffer);
			}

			public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
			{
				if (_decompressionStream == null)
				{
					return CreateAndReadAsync(this, buffer, cancellationToken);
				}
				return _decompressionStream.ReadAsync(buffer, cancellationToken);
				static async ValueTask<int> CreateAndReadAsync(ZLibOrDeflateStream thisRef, Memory<byte> buffer, CancellationToken cancellationToken)
				{
					thisRef._decompressionStream = CreateDecompressionStream(await thisRef._stream.PeekFirstByteAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false), thisRef._stream);
					return await thisRef._decompressionStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}

			public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
			{
				Stream.ValidateCopyToArguments(destination, bufferSize);
				return Core(destination, bufferSize, cancellationToken);
				async Task Core(Stream destination, int bufferSize, CancellationToken cancellationToken)
				{
					if (_decompressionStream == null)
					{
						_decompressionStream = CreateDecompressionStream(await _stream.PeekFirstByteAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false), _stream);
					}
					await _decompressionStream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}

			private static Stream CreateDecompressionStream(int firstByte, Stream stream)
			{
				if ((firstByte & 0xF) != 8)
				{
					return new DeflateStream(stream, CompressionMode.Decompress);
				}
				return new ZLibStream(stream, CompressionMode.Decompress);
			}
		}

		public DeflateDecompressedContent(HttpContent originalContent)
			: base(originalContent)
		{
		}

		protected override Stream GetDecompressedStream(Stream originalStream)
		{
			return new ZLibOrDeflateStream(originalStream);
		}
	}

	private sealed class BrotliDecompressedContent : DecompressedContent
	{
		public BrotliDecompressedContent(HttpContent originalContent)
			: base(originalContent)
		{
		}

		protected override Stream GetDecompressedStream(Stream originalStream)
		{
			return new BrotliStream(originalStream, CompressionMode.Decompress);
		}
	}

	private readonly HttpMessageHandlerStage _innerHandler;

	private readonly DecompressionMethods _decompressionMethods;

	private static readonly StringWithQualityHeaderValue s_gzipHeaderValue = new StringWithQualityHeaderValue("gzip");

	private static readonly StringWithQualityHeaderValue s_deflateHeaderValue = new StringWithQualityHeaderValue("deflate");

	private static readonly StringWithQualityHeaderValue s_brotliHeaderValue = new StringWithQualityHeaderValue("br");

	internal bool GZipEnabled => (_decompressionMethods & DecompressionMethods.GZip) != 0;

	internal bool DeflateEnabled => (_decompressionMethods & DecompressionMethods.Deflate) != 0;

	internal bool BrotliEnabled => (_decompressionMethods & DecompressionMethods.Brotli) != 0;

	public DecompressionHandler(DecompressionMethods decompressionMethods, HttpMessageHandlerStage innerHandler)
	{
		_decompressionMethods = decompressionMethods;
		_innerHandler = innerHandler;
	}

	private static bool EncodingExists(HttpHeaderValueCollection<StringWithQualityHeaderValue> acceptEncodingHeader, string encoding)
	{
		foreach (StringWithQualityHeaderValue item in acceptEncodingHeader)
		{
			if (string.Equals(item.Value, encoding, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	internal override async ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		if (GZipEnabled && !EncodingExists(request.Headers.AcceptEncoding, "gzip"))
		{
			request.Headers.AcceptEncoding.Add(s_gzipHeaderValue);
		}
		if (DeflateEnabled && !EncodingExists(request.Headers.AcceptEncoding, "deflate"))
		{
			request.Headers.AcceptEncoding.Add(s_deflateHeaderValue);
		}
		if (BrotliEnabled && !EncodingExists(request.Headers.AcceptEncoding, "br"))
		{
			request.Headers.AcceptEncoding.Add(s_brotliHeaderValue);
		}
		HttpResponseMessage httpResponseMessage = await _innerHandler.SendAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		ICollection<string> contentEncoding = httpResponseMessage.Content.Headers.ContentEncoding;
		if (contentEncoding.Count > 0)
		{
			string text = null;
			foreach (string item in contentEncoding)
			{
				text = item;
			}
			if (GZipEnabled && text == "gzip")
			{
				httpResponseMessage.Content = new GZipDecompressedContent(httpResponseMessage.Content);
			}
			else if (DeflateEnabled && text == "deflate")
			{
				httpResponseMessage.Content = new DeflateDecompressedContent(httpResponseMessage.Content);
			}
			else if (BrotliEnabled && text == "br")
			{
				httpResponseMessage.Content = new BrotliDecompressedContent(httpResponseMessage.Content);
			}
		}
		return httpResponseMessage;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_innerHandler.Dispose();
		}
		base.Dispose(disposing);
	}
}
