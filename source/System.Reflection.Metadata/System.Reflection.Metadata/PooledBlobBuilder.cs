using System.Reflection.Internal;

namespace System.Reflection.Metadata;

internal sealed class PooledBlobBuilder : BlobBuilder
{
	private static readonly ObjectPool<PooledBlobBuilder> s_chunkPool = new ObjectPool<PooledBlobBuilder>(() => new PooledBlobBuilder(1024), 128);

	private PooledBlobBuilder(int size)
		: base(size)
	{
	}

	public static PooledBlobBuilder GetInstance()
	{
		return s_chunkPool.Allocate();
	}

	protected override BlobBuilder AllocateChunk(int minimalSize)
	{
		if (minimalSize <= 1024)
		{
			return s_chunkPool.Allocate();
		}
		return new BlobBuilder(minimalSize);
	}

	protected override void FreeChunk()
	{
		s_chunkPool.Free(this);
	}

	public new void Free()
	{
		base.Free();
	}
}
