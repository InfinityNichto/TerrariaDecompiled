using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct EnCMapTableReader
{
	internal readonly int NumberOfRows;

	private readonly int _TokenOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal EnCMapTableReader(int numberOfRows, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_TokenOffset = 0;
		RowSize = _TokenOffset + 4;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal uint GetToken(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekUInt32(num + _TokenOffset);
	}
}
