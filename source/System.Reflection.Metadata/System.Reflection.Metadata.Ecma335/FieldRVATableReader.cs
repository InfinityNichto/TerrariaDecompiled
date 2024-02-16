using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct FieldRVATableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsFieldTableRowRefSizeSmall;

	private readonly int _RvaOffset;

	private readonly int _FieldOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal FieldRVATableReader(int numberOfRows, bool declaredSorted, int fieldTableRowRefSize, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsFieldTableRowRefSizeSmall = fieldTableRowRefSize == 2;
		_RvaOffset = 0;
		_FieldOffset = _RvaOffset + 4;
		RowSize = _FieldOffset + fieldTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			Throw.TableNotSorted(TableIndex.FieldRva);
		}
	}

	internal int GetRva(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekInt32(num + _RvaOffset);
	}

	internal int FindFieldRvaRowId(int fieldDefRowId)
	{
		int num = Block.BinarySearchReference(NumberOfRows, RowSize, _FieldOffset, (uint)fieldDefRowId, _IsFieldTableRowRefSizeSmall);
		return num + 1;
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _FieldOffset, _IsFieldTableRowRefSizeSmall);
	}
}
