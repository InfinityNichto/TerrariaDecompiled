using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct DeclSecurityTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsHasDeclSecurityRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _ActionOffset;

	private readonly int _ParentOffset;

	private readonly int _PermissionSetOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal DeclSecurityTableReader(int numberOfRows, bool declaredSorted, int hasDeclSecurityRefSize, int blobHeapRefSize, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsHasDeclSecurityRefSizeSmall = hasDeclSecurityRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_ActionOffset = 0;
		_ParentOffset = _ActionOffset + 2;
		_PermissionSetOffset = _ParentOffset + hasDeclSecurityRefSize;
		RowSize = _PermissionSetOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			Throw.TableNotSorted(TableIndex.DeclSecurity);
		}
	}

	internal DeclarativeSecurityAction GetAction(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return (DeclarativeSecurityAction)Block.PeekUInt16(num + _ActionOffset);
	}

	internal EntityHandle GetParent(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return HasDeclSecurityTag.ConvertToHandle(Block.PeekTaggedReference(num + _ParentOffset, _IsHasDeclSecurityRefSizeSmall));
	}

	internal BlobHandle GetPermissionSet(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return BlobHandle.FromOffset(Block.PeekHeapReference(num + _PermissionSetOffset, _IsBlobHeapRefSizeSmall));
	}

	internal void GetAttributeRange(EntityHandle parentToken, out int firstImplRowId, out int lastImplRowId)
	{
		Block.BinarySearchReferenceRange(NumberOfRows, RowSize, _ParentOffset, HasDeclSecurityTag.ConvertToTag(parentToken), _IsHasDeclSecurityRefSizeSmall, out var startRowNumber, out var endRowNumber);
		if (startRowNumber == -1)
		{
			firstImplRowId = 1;
			lastImplRowId = 0;
		}
		else
		{
			firstImplRowId = startRowNumber + 1;
			lastImplRowId = endRowNumber + 1;
		}
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _ParentOffset, _IsHasDeclSecurityRefSizeSmall);
	}
}
