using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class MultipartContent : HttpContent, IEnumerable<HttpContent>, IEnumerable
{
	private sealed class ContentReadStream : Stream
	{
		private readonly Stream[] _streams;

		private readonly long _length;

		private int _next;

		private Stream _current;

		private long _position;

		public override bool CanRead => true;

		public override bool CanSeek => true;

		public override bool CanWrite => false;

		public override long Position
		{
			get
			{
				return _position;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				long num = 0L;
				for (int i = 0; i < _streams.Length; i++)
				{
					Stream stream = _streams[i];
					long length = stream.Length;
					if (value < num + length)
					{
						_current = stream;
						i = (_next = i + 1);
						stream.Position = value - num;
						for (; i < _streams.Length; i++)
						{
							_streams[i].Position = 0L;
						}
						_position = value;
						return;
					}
					num += length;
				}
				_current = null;
				_next = _streams.Length;
				_position = value;
			}
		}

		public override long Length => _length;

		internal ContentReadStream(Stream[] streams)
		{
			_streams = streams;
			foreach (Stream stream in streams)
			{
				_length += stream.Length;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Stream[] streams = _streams;
				foreach (Stream stream in streams)
				{
					stream.Dispose();
				}
			}
		}

		public override async ValueTask DisposeAsync()
		{
			Stream[] streams = _streams;
			foreach (Stream stream in streams)
			{
				await stream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			Stream.ValidateBufferArguments(buffer, offset, count);
			if (count == 0)
			{
				return 0;
			}
			while (true)
			{
				if (_current != null)
				{
					int num = _current.Read(buffer, offset, count);
					if (num != 0)
					{
						_position += num;
						return num;
					}
					_current = null;
				}
				if (_next >= _streams.Length)
				{
					break;
				}
				_current = _streams[_next++];
			}
			return 0;
		}

		public override int Read(Span<byte> buffer)
		{
			if (buffer.Length == 0)
			{
				return 0;
			}
			while (true)
			{
				if (_current != null)
				{
					int num = _current.Read(buffer);
					if (num != 0)
					{
						_position += num;
						return num;
					}
					_current = null;
				}
				if (_next >= _streams.Length)
				{
					break;
				}
				_current = _streams[_next++];
			}
			return 0;
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			Stream.ValidateBufferArguments(buffer, offset, count);
			return ReadAsyncPrivate(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ReadAsyncPrivate(buffer, cancellationToken);
		}

		public override IAsyncResult BeginRead(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState)
		{
			return System.Threading.Tasks.TaskToApm.Begin(ReadAsync(array, offset, count, CancellationToken.None), asyncCallback, asyncState);
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
		}

		public async ValueTask<int> ReadAsyncPrivate(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			if (buffer.Length == 0)
			{
				return 0;
			}
			while (true)
			{
				if (_current != null)
				{
					int num = await _current.ReadAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					if (num != 0)
					{
						_position += num;
						return num;
					}
					_current = null;
				}
				if (_next >= _streams.Length)
				{
					break;
				}
				_current = _streams[_next++];
			}
			return 0;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
			case SeekOrigin.Begin:
				Position = offset;
				break;
			case SeekOrigin.Current:
				Position += offset;
				break;
			case SeekOrigin.End:
				Position = _length + offset;
				break;
			default:
				throw new ArgumentOutOfRangeException("origin");
			}
			return Position;
		}

		public override void Flush()
		{
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			throw new NotSupportedException();
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotSupportedException();
		}
	}

	private readonly List<HttpContent> _nestedContent;

	private readonly string _boundary;

	public HeaderEncodingSelector<HttpContent>? HeaderEncodingSelector { get; set; }

	internal override bool AllowDuplex => false;

	public MultipartContent()
		: this("mixed", GetDefaultBoundary())
	{
	}

	public MultipartContent(string subtype)
		: this(subtype, GetDefaultBoundary())
	{
	}

	public MultipartContent(string subtype, string boundary)
	{
		if (string.IsNullOrWhiteSpace(subtype))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, "subtype");
		}
		ValidateBoundary(boundary);
		_boundary = boundary;
		string text = boundary;
		if (!text.StartsWith('"'))
		{
			text = "\"" + text + "\"";
		}
		MediaTypeHeaderValue contentType = new MediaTypeHeaderValue("multipart/" + subtype)
		{
			Parameters = 
			{
				new NameValueHeaderValue("boundary", text)
			}
		};
		base.Headers.ContentType = contentType;
		_nestedContent = new List<HttpContent>();
	}

	private static void ValidateBoundary(string boundary)
	{
		if (string.IsNullOrWhiteSpace(boundary))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, "boundary");
		}
		if (boundary.Length > 70)
		{
			throw new ArgumentOutOfRangeException("boundary", boundary, System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_content_field_too_long, 70));
		}
		if (boundary.EndsWith(' '))
		{
			throw new ArgumentException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, boundary), "boundary");
		}
		foreach (char c in boundary)
		{
			if (('0' > c || c > '9') && ('a' > c || c > 'z') && ('A' > c || c > 'Z') && !"'()+_,-./:=? ".Contains(c))
			{
				throw new ArgumentException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, boundary), "boundary");
			}
		}
	}

	private static string GetDefaultBoundary()
	{
		return Guid.NewGuid().ToString();
	}

	public virtual void Add(HttpContent content)
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		_nestedContent.Add(content);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			foreach (HttpContent item in _nestedContent)
			{
				item.Dispose();
			}
			_nestedContent.Clear();
		}
		base.Dispose(disposing);
	}

	public IEnumerator<HttpContent> GetEnumerator()
	{
		return _nestedContent.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _nestedContent.GetEnumerator();
	}

	protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		try
		{
			WriteToStream(stream, "--" + _boundary + "\r\n");
			for (int i = 0; i < _nestedContent.Count; i++)
			{
				HttpContent httpContent = _nestedContent[i];
				SerializeHeadersToStream(stream, httpContent, i != 0);
				httpContent.CopyTo(stream, context, cancellationToken);
			}
			WriteToStream(stream, "\r\n--" + _boundary + "--\r\n");
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "SerializeToStream");
			}
			throw;
		}
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
	{
		return SerializeToStreamAsyncCore(stream, context, default(CancellationToken));
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		if (!(GetType() == typeof(MultipartContent)))
		{
			return base.SerializeToStreamAsync(stream, context, cancellationToken);
		}
		return SerializeToStreamAsyncCore(stream, context, cancellationToken);
	}

	private protected async Task SerializeToStreamAsyncCore(Stream stream, TransportContext context, CancellationToken cancellationToken)
	{
		_ = 3;
		try
		{
			await EncodeStringToStreamAsync(stream, "--" + _boundary + "\r\n", cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			MemoryStream output = new MemoryStream();
			for (int contentIndex = 0; contentIndex < _nestedContent.Count; contentIndex++)
			{
				HttpContent content = _nestedContent[contentIndex];
				output.SetLength(0L);
				SerializeHeadersToStream(output, content, contentIndex != 0);
				output.Position = 0L;
				await output.CopyToAsync(stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				await content.CopyToAsync(stream, context, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			await EncodeStringToStreamAsync(stream, "\r\n--" + _boundary + "--\r\n", cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "SerializeToStreamAsyncCore");
			}
			throw;
		}
	}

	protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
	{
		return CreateContentReadStreamAsyncCore(async: false, cancellationToken).GetAwaiter().GetResult();
	}

	protected override Task<Stream> CreateContentReadStreamAsync()
	{
		return CreateContentReadStreamAsyncCore(async: true, CancellationToken.None).AsTask();
	}

	protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
	{
		if (!(GetType() == typeof(MultipartContent)))
		{
			return base.CreateContentReadStreamAsync(cancellationToken);
		}
		return CreateContentReadStreamAsyncCore(async: true, cancellationToken).AsTask();
	}

	private async ValueTask<Stream> CreateContentReadStreamAsyncCore(bool async, CancellationToken cancellationToken)
	{
		_ = 1;
		try
		{
			Stream[] streams = new Stream[2 + _nestedContent.Count * 2];
			int streamIndex = 0;
			streams[streamIndex++] = EncodeStringToNewStream("--" + _boundary + "\r\n");
			for (int contentIndex = 0; contentIndex < _nestedContent.Count; contentIndex++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				HttpContent httpContent = _nestedContent[contentIndex];
				streams[streamIndex++] = EncodeHeadersToNewStream(httpContent, contentIndex != 0);
				Stream stream3;
				if (async)
				{
					Stream stream = httpContent.TryReadAsStream();
					Stream stream2 = stream;
					if (stream2 == null)
					{
						stream2 = await httpContent.ReadAsStreamAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
					stream3 = stream2;
				}
				else
				{
					stream3 = httpContent.ReadAsStream(cancellationToken);
				}
				if (stream3 == null)
				{
					stream3 = new MemoryStream();
				}
				if (!stream3.CanSeek)
				{
					return (!async) ? base.CreateContentReadStream(cancellationToken) : (await base.CreateContentReadStreamAsync().ConfigureAwait(continueOnCapturedContext: false));
				}
				streams[streamIndex++] = stream3;
			}
			streams[streamIndex] = EncodeStringToNewStream("\r\n--" + _boundary + "--\r\n");
			return new ContentReadStream(streams);
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "CreateContentReadStreamAsyncCore");
			}
			throw;
		}
	}

	private void SerializeHeadersToStream(Stream stream, HttpContent content, bool writeDivider)
	{
		if (writeDivider)
		{
			WriteToStream(stream, "\r\n--");
			WriteToStream(stream, _boundary);
			WriteToStream(stream, "\r\n");
		}
		foreach (KeyValuePair<string, HeaderStringValues> item in content.Headers.NonValidated)
		{
			Encoding encoding = HeaderEncodingSelector?.Invoke(item.Key, content) ?? HttpRuleParser.DefaultHttpEncoding;
			WriteToStream(stream, item.Key);
			WriteToStream(stream, ": ");
			string content2 = string.Empty;
			foreach (string item2 in item.Value)
			{
				WriteToStream(stream, content2);
				WriteToStream(stream, item2, encoding);
				content2 = ", ";
			}
			WriteToStream(stream, "\r\n");
		}
		WriteToStream(stream, "\r\n");
	}

	private static ValueTask EncodeStringToStreamAsync(Stream stream, string input, CancellationToken cancellationToken)
	{
		byte[] bytes = HttpRuleParser.DefaultHttpEncoding.GetBytes(input);
		return stream.WriteAsync(new ReadOnlyMemory<byte>(bytes), cancellationToken);
	}

	private static Stream EncodeStringToNewStream(string input)
	{
		return new MemoryStream(HttpRuleParser.DefaultHttpEncoding.GetBytes(input), writable: false);
	}

	private Stream EncodeHeadersToNewStream(HttpContent content, bool writeDivider)
	{
		MemoryStream memoryStream = new MemoryStream();
		SerializeHeadersToStream(memoryStream, content, writeDivider);
		memoryStream.Position = 0L;
		return memoryStream;
	}

	protected internal override bool TryComputeLength(out long length)
	{
		long num = 2 + _boundary.Length + 2;
		if (_nestedContent.Count > 1)
		{
			num += (_nestedContent.Count - 1) * (4 + _boundary.Length + 2);
		}
		foreach (HttpContent item in _nestedContent)
		{
			foreach (KeyValuePair<string, HeaderStringValues> item2 in item.Headers.NonValidated)
			{
				num += item2.Key.Length + 2;
				Encoding encoding = HeaderEncodingSelector?.Invoke(item2.Key, item) ?? HttpRuleParser.DefaultHttpEncoding;
				int num2 = 0;
				foreach (string item3 in item2.Value)
				{
					num += encoding.GetByteCount(item3);
					num2++;
				}
				if (num2 > 1)
				{
					num += (num2 - 1) * 2;
				}
				num += 2;
			}
			num += 2;
			if (!item.TryComputeLength(out var length2))
			{
				length = 0L;
				return false;
			}
			num += length2;
		}
		num += 4 + _boundary.Length + 2 + 2;
		length = num;
		return true;
	}

	private static void WriteToStream(Stream stream, string content)
	{
		WriteToStream(stream, content, HttpRuleParser.DefaultHttpEncoding);
	}

	private static void WriteToStream(Stream stream, string content, Encoding encoding)
	{
		int maxByteCount = encoding.GetMaxByteCount(content.Length);
		byte[] array = null;
		Span<byte> span = ((maxByteCount > 1024) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxByteCount))) : stackalloc byte[1024]);
		Span<byte> bytes = span;
		try
		{
			stream.Write(bytes[..encoding.GetBytes(content, bytes)]);
		}
		finally
		{
			if (array != null)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}
}
