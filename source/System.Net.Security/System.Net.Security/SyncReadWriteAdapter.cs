using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security;

internal readonly struct SyncReadWriteAdapter : IReadWriteAdapter
{
	private readonly Stream _stream;

	public CancellationToken CancellationToken => default(CancellationToken);

	public SyncReadWriteAdapter(Stream stream)
	{
		_stream = stream;
	}

	public ValueTask<int> ReadAsync(Memory<byte> buffer)
	{
		return new ValueTask<int>(_stream.Read(buffer.Span));
	}

	public ValueTask WriteAsync(byte[] buffer, int offset, int count)
	{
		_stream.Write(buffer, offset, count);
		return default(ValueTask);
	}

	public Task WaitAsync(TaskCompletionSource<bool> waiter)
	{
		waiter.Task.GetAwaiter().GetResult();
		return Task.CompletedTask;
	}

	public Task FlushAsync()
	{
		_stream.Flush();
		return Task.CompletedTask;
	}
}
