using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct MethodPtrTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsMethodTableRowRefSizeSmall;

	private readonly int _MethodOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal MethodPtrTableReader(int numberOfRows, int methodTableRowRefSize, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsMethodTableRowRefSizeSmall = methodTableRowRefSize == 2;
		_MethodOffset = 0;
		RowSize = _MethodOffset + methodTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal MethodDefinitionHandle GetMethodFor(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return MethodDefinitionHandle.FromRowId(Block.PeekReference(num + _MethodOffset, _IsMethodTableRowRefSizeSmall));
	}

	internal int GetRowIdForMethodDefRow(int methodDefRowId)
	{
		return Block.LinearSearchReference(RowSize, _MethodOffset, (uint)methodDefRowId, _IsMethodTableRowRefSizeSmall) + 1;
	}
}
