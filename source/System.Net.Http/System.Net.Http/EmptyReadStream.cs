using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class EmptyReadStream : HttpBaseStream
{
	internal static EmptyReadStream Instance { get; } = new EmptyReadStream();


	public override bool CanRead => true;

	public override bool CanWrite => false;

	private EmptyReadStream()
	{
	}

	protected override void Dispose(bool disposing)
	{
	}

	public override void Close()
	{
	}

	public override int Read(Span<byte> buffer)
	{
		return 0;
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return new ValueTask<int>(0);
		}
		return ValueTask.FromCanceled<int>(cancellationToken);
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		Stream.ValidateCopyToArguments(destination, bufferSize);
		return HttpBaseStream.NopAsync(cancellationToken);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		throw new NotSupportedException(System.SR.net_http_content_readonly_stream);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken)
	{
		throw new NotSupportedException();
	}
}
