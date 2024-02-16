using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class HttpConnectionResponseContent : HttpContent
{
	private Stream _stream;

	private bool _consumedStream;

	internal override bool AllowDuplex => false;

	public void SetStream(Stream stream)
	{
		_stream = stream;
	}

	private Stream ConsumeStream()
	{
		if (_consumedStream || _stream == null)
		{
			throw new InvalidOperationException(System.SR.net_http_content_stream_already_read);
		}
		_consumedStream = true;
		return _stream;
	}

	protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		using Stream stream2 = ConsumeStream();
		stream2.CopyTo(stream, 8192);
	}

	protected sealed override Task SerializeToStreamAsync(Stream stream, TransportContext context)
	{
		return SerializeToStreamAsync(stream, context, CancellationToken.None);
	}

	protected sealed override async Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		using Stream contentStream = ConsumeStream();
		await contentStream.CopyToAsync(stream, 8192, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	protected internal sealed override bool TryComputeLength(out long length)
	{
		length = 0L;
		return false;
	}

	protected sealed override Stream CreateContentReadStream(CancellationToken cancellationToken)
	{
		return ConsumeStream();
	}

	protected sealed override Task<Stream> CreateContentReadStreamAsync()
	{
		return Task.FromResult(ConsumeStream());
	}

	internal sealed override Stream TryCreateContentReadStream()
	{
		return ConsumeStream();
	}

	protected sealed override void Dispose(bool disposing)
	{
		if (disposing && _stream != null)
		{
			_stream.Dispose();
			_stream = null;
		}
		base.Dispose(disposing);
	}
}
