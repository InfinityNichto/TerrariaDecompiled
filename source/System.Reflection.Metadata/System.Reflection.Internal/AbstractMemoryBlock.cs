using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace System.Reflection.Internal;

internal abstract class AbstractMemoryBlock : IDisposable
{
	public unsafe abstract byte* Pointer { get; }

	public abstract int Size { get; }

	public unsafe BlobReader GetReader()
	{
		return new BlobReader(Pointer, Size);
	}

	public unsafe virtual ImmutableArray<byte> GetContentUnchecked(int start, int length)
	{
		ImmutableArray<byte> result = BlobUtilities.ReadImmutableBytes(Pointer + start, length);
		GC.KeepAlive(this);
		return result;
	}

	public abstract void Dispose();
}
