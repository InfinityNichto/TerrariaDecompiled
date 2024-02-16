using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct GuidHeap
{
	internal readonly MemoryBlock Block;

	public GuidHeap(MemoryBlock block)
	{
		Block = block;
	}

	internal Guid GetGuid(GuidHandle handle)
	{
		if (handle.IsNil)
		{
			return default(Guid);
		}
		return Block.PeekGuid((handle.Index - 1) * 16);
	}
}
