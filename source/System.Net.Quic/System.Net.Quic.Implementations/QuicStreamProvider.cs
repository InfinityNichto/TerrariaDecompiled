using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations;

internal abstract class QuicStreamProvider : IDisposable, IAsyncDisposable
{
	internal abstract long StreamId { get; }

	internal abstract bool CanTimeout { get; }

	internal abstract bool CanRead { get; }

	internal abstract bool ReadsCompleted { get; }

	internal abstract int ReadTimeout { get; set; }

	internal abstract bool CanWrite { get; }

	internal abstract int WriteTimeout { get; set; }

	internal abstract int Read(Span<byte> buffer);

	internal abstract ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken));

	internal abstract void AbortRead(long errorCode);

	internal abstract void AbortWrite(long errorCode);

	internal abstract void Write(ReadOnlySpan<byte> buffer);

	internal abstract ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken));

	internal abstract ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default(CancellationToken));

	internal abstract ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default(CancellationToken));

	internal abstract ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default(CancellationToken));

	internal abstract ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default(CancellationToken));

	internal abstract ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default(CancellationToken));

	internal abstract ValueTask ShutdownCompleted(CancellationToken cancellationToken = default(CancellationToken));

	internal abstract ValueTask WaitForWriteCompletionAsync(CancellationToken cancellationToken = default(CancellationToken));

	internal abstract void Shutdown();

	internal abstract void Flush();

	internal abstract Task FlushAsync(CancellationToken cancellationToken);

	public abstract void Dispose();

	public abstract ValueTask DisposeAsync();
}
