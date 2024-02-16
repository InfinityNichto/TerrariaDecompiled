using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct FieldLayoutTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsFieldTableRowRefSizeSmall;

	private readonly int _OffsetOffset;

	private readonly int _FieldOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal FieldLayoutTableReader(int numberOfRows, bool declaredSorted, int fieldTableRowRefSize, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsFieldTableRowRefSizeSmall = fieldTableRowRefSize == 2;
		_OffsetOffset = 0;
		_FieldOffset = _OffsetOffset + 4;
		RowSize = _FieldOffset + fieldTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			Throw.TableNotSorted(TableIndex.FieldLayout);
		}
	}

	internal int FindFieldLayoutRowId(FieldDefinitionHandle handle)
	{
		int num = Block.BinarySearchReference(NumberOfRows, RowSize, _FieldOffset, (uint)handle.RowId, _IsFieldTableRowRefSizeSmall);
		return num + 1;
	}

	internal uint GetOffset(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekUInt32(num + _OffsetOffset);
	}

	internal FieldDefinitionHandle GetField(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return FieldDefinitionHandle.FromRowId(Block.PeekReference(num + _FieldOffset, _IsFieldTableRowRefSizeSmall));
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _FieldOffset, _IsFieldTableRowRefSizeSmall);
	}
}
