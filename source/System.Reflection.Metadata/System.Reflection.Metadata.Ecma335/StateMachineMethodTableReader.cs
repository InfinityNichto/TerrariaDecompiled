using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct StateMachineMethodTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _isMethodRefSizeSmall;

	private readonly int _kickoffMethodOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal StateMachineMethodTableReader(int numberOfRows, bool declaredSorted, int methodRefSize, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_isMethodRefSizeSmall = methodRefSize == 2;
		_kickoffMethodOffset = methodRefSize;
		RowSize = _kickoffMethodOffset + methodRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (numberOfRows > 0 && !declaredSorted)
		{
			Throw.TableNotSorted(TableIndex.StateMachineMethod);
		}
	}

	internal MethodDefinitionHandle FindKickoffMethod(int moveNextMethodRowId)
	{
		int num = Block.BinarySearchReference(NumberOfRows, RowSize, 0, (uint)moveNextMethodRowId, _isMethodRefSizeSmall);
		if (num < 0)
		{
			return default(MethodDefinitionHandle);
		}
		return GetKickoffMethod(num + 1);
	}

	private MethodDefinitionHandle GetKickoffMethod(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return MethodDefinitionHandle.FromRowId(Block.PeekReference(num + _kickoffMethodOffset, _isMethodRefSizeSmall));
	}
}
