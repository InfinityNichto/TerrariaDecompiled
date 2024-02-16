using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security;

internal readonly struct AsyncReadWriteAdapter : IReadWriteAdapter
{
	private readonly Stream _stream;

	public CancellationToken CancellationToken { get; }

	public AsyncReadWriteAdapter(Stream stream, CancellationToken cancellationToken)
	{
		_stream = stream;
		CancellationToken = cancellationToken;
	}

	public ValueTask<int> ReadAsync(Memory<byte> buffer)
	{
		return _stream.ReadAsync(buffer, CancellationToken);
	}

	public ValueTask WriteAsync(byte[] buffer, int offset, int count)
	{
		return _stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken);
	}

	public Task WaitAsync(TaskCompletionSource<bool> waiter)
	{
		return waiter.Task;
	}

	public Task FlushAsync()
	{
		return _stream.FlushAsync(CancellationToken);
	}
}
