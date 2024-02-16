using System.IO;

namespace System.Reflection.Internal;

internal abstract class MemoryBlockProvider : IDisposable
{
	public abstract int Size { get; }

	public AbstractMemoryBlock GetMemoryBlock()
	{
		return GetMemoryBlockImpl(0, Size);
	}

	public AbstractMemoryBlock GetMemoryBlock(int start, int size)
	{
		if ((ulong)((long)(uint)start + (long)(uint)size) > (ulong)Size)
		{
			Throw.ImageTooSmallOrContainsInvalidOffsetOrCount();
		}
		return GetMemoryBlockImpl(start, size);
	}

	protected abstract AbstractMemoryBlock GetMemoryBlockImpl(int start, int size);

	public abstract Stream GetStream(out StreamConstraints constraints);

	protected abstract void Dispose(bool disposing);

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
