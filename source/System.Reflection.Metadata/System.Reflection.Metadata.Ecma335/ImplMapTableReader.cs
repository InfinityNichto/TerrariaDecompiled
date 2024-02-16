using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ImplMapTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsModuleRefTableRowRefSizeSmall;

	private readonly bool _IsMemberForwardRowRefSizeSmall;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly int _FlagsOffset;

	private readonly int _MemberForwardedOffset;

	private readonly int _ImportNameOffset;

	private readonly int _ImportScopeOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal ImplMapTableReader(int numberOfRows, bool declaredSorted, int moduleRefTableRowRefSize, int memberForwardedRefSize, int stringHeapRefSize, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsModuleRefTableRowRefSizeSmall = moduleRefTableRowRefSize == 2;
		_IsMemberForwardRowRefSizeSmall = memberForwardedRefSize == 2;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_FlagsOffset = 0;
		_MemberForwardedOffset = _FlagsOffset + 2;
		_ImportNameOffset = _MemberForwardedOffset + memberForwardedRefSize;
		_ImportScopeOffset = _ImportNameOffset + stringHeapRefSize;
		RowSize = _ImportScopeOffset + moduleRefTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			Throw.TableNotSorted(TableIndex.ImplMap);
		}
	}

	internal MethodImport GetImport(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		MethodImportAttributes attributes = (MethodImportAttributes)Block.PeekUInt16(num + _FlagsOffset);
		StringHandle name = StringHandle.FromOffset(Block.PeekHeapReference(num + _ImportNameOffset, _IsStringHeapRefSizeSmall));
		ModuleReferenceHandle module = ModuleReferenceHandle.FromRowId(Block.PeekReference(num + _ImportScopeOffset, _IsModuleRefTableRowRefSizeSmall));
		return new MethodImport(attributes, name, module);
	}

	internal EntityHandle GetMemberForwarded(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return MemberForwardedTag.ConvertToHandle(Block.PeekTaggedReference(num + _MemberForwardedOffset, _IsMemberForwardRowRefSizeSmall));
	}

	internal int FindImplForMethod(MethodDefinitionHandle methodDef)
	{
		uint searchCodedTag = MemberForwardedTag.ConvertMethodDefToTag(methodDef);
		return BinarySearchTag(searchCodedTag);
	}

	private int BinarySearchTag(uint searchCodedTag)
	{
		int num = Block.BinarySearchReference(NumberOfRows, RowSize, _MemberForwardedOffset, searchCodedTag, _IsMemberForwardRowRefSizeSmall);
		return num + 1;
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _MemberForwardedOffset, _IsMemberForwardRowRefSizeSmall);
	}
}
