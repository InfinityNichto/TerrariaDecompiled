using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct MethodSemanticsTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsMethodTableRowRefSizeSmall;

	private readonly bool _IsHasSemanticRefSizeSmall;

	private readonly int _SemanticsFlagOffset;

	private readonly int _MethodOffset;

	private readonly int _AssociationOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal MethodSemanticsTableReader(int numberOfRows, bool declaredSorted, int methodTableRowRefSize, int hasSemanticRefSize, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsMethodTableRowRefSizeSmall = methodTableRowRefSize == 2;
		_IsHasSemanticRefSizeSmall = hasSemanticRefSize == 2;
		_SemanticsFlagOffset = 0;
		_MethodOffset = _SemanticsFlagOffset + 2;
		_AssociationOffset = _MethodOffset + methodTableRowRefSize;
		RowSize = _AssociationOffset + hasSemanticRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			Throw.TableNotSorted(TableIndex.MethodSemantics);
		}
	}

	internal MethodDefinitionHandle GetMethod(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return MethodDefinitionHandle.FromRowId(Block.PeekReference(num + _MethodOffset, _IsMethodTableRowRefSizeSmall));
	}

	internal MethodSemanticsAttributes GetSemantics(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return (MethodSemanticsAttributes)Block.PeekUInt16(num + _SemanticsFlagOffset);
	}

	internal EntityHandle GetAssociation(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return HasSemanticsTag.ConvertToHandle(Block.PeekTaggedReference(num + _AssociationOffset, _IsHasSemanticRefSizeSmall));
	}

	internal int FindSemanticMethodsForEvent(EventDefinitionHandle eventDef, out ushort methodCount)
	{
		methodCount = 0;
		uint searchCodedTag = HasSemanticsTag.ConvertEventHandleToTag(eventDef);
		return BinarySearchTag(searchCodedTag, ref methodCount);
	}

	internal int FindSemanticMethodsForProperty(PropertyDefinitionHandle propertyDef, out ushort methodCount)
	{
		methodCount = 0;
		uint searchCodedTag = HasSemanticsTag.ConvertPropertyHandleToTag(propertyDef);
		return BinarySearchTag(searchCodedTag, ref methodCount);
	}

	private int BinarySearchTag(uint searchCodedTag, ref ushort methodCount)
	{
		Block.BinarySearchReferenceRange(NumberOfRows, RowSize, _AssociationOffset, searchCodedTag, _IsHasSemanticRefSizeSmall, out var startRowNumber, out var endRowNumber);
		if (startRowNumber == -1)
		{
			methodCount = 0;
			return 0;
		}
		methodCount = (ushort)(endRowNumber - startRowNumber + 1);
		return startRowNumber + 1;
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _AssociationOffset, _IsHasSemanticRefSizeSmall);
	}
}
