using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security;

internal interface IReadWriteAdapter
{
	CancellationToken CancellationToken { get; }

	ValueTask<int> ReadAsync(Memory<byte> buffer);

	ValueTask WriteAsync(byte[] buffer, int offset, int count);

	Task WaitAsync(TaskCompletionSource<bool> waiter);

	Task FlushAsync();
}
