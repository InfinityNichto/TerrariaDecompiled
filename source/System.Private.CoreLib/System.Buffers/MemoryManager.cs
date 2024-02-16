using System.Runtime.CompilerServices;

namespace System.Buffers;

public abstract class MemoryManager<T> : IMemoryOwner<T>, IDisposable, IPinnable
{
	public virtual Memory<T> Memory => new Memory<T>(this, GetSpan().Length);

	public abstract Span<T> GetSpan();

	public abstract MemoryHandle Pin(int elementIndex = 0);

	public abstract void Unpin();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected Memory<T> CreateMemory(int length)
	{
		return new Memory<T>(this, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected Memory<T> CreateMemory(int start, int length)
	{
		return new Memory<T>(this, start, length);
	}

	protected internal virtual bool TryGetArray(out ArraySegment<T> segment)
	{
		segment = default(ArraySegment<T>);
		return false;
	}

	void IDisposable.Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected abstract void Dispose(bool disposing);
}
