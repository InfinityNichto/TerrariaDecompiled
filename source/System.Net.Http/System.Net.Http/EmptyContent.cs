using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class EmptyContent : HttpContent
{
	internal override bool AllowDuplex => false;

	protected internal override bool TryComputeLength(out long length)
	{
		length = 0L;
		return true;
	}

	protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken)
	{
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
	{
		return Task.CompletedTask;
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return SerializeToStreamAsync(stream, context);
		}
		return Task.FromCanceled(cancellationToken);
	}

	protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
	{
		return EmptyReadStream.Instance;
	}

	protected override Task<Stream> CreateContentReadStreamAsync()
	{
		return Task.FromResult((Stream)EmptyReadStream.Instance);
	}

	protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return CreateContentReadStreamAsync();
		}
		return Task.FromCanceled<Stream>(cancellationToken);
	}

	internal override Stream TryCreateContentReadStream()
	{
		return EmptyReadStream.Instance;
	}
}
