using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct FieldMarshalTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsHasFieldMarshalRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _ParentOffset;

	private readonly int _NativeTypeOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal FieldMarshalTableReader(int numberOfRows, bool declaredSorted, int hasFieldMarshalRefSize, int blobHeapRefSize, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsHasFieldMarshalRefSizeSmall = hasFieldMarshalRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_ParentOffset = 0;
		_NativeTypeOffset = _ParentOffset + hasFieldMarshalRefSize;
		RowSize = _NativeTypeOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			Throw.TableNotSorted(TableIndex.FieldMarshal);
		}
	}

	internal EntityHandle GetParent(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return HasFieldMarshalTag.ConvertToHandle(Block.PeekTaggedReference(num + _ParentOffset, _IsHasFieldMarshalRefSizeSmall));
	}

	internal BlobHandle GetNativeType(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return BlobHandle.FromOffset(Block.PeekHeapReference(num + _NativeTypeOffset, _IsBlobHeapRefSizeSmall));
	}

	internal int FindFieldMarshalRowId(EntityHandle handle)
	{
		int num = Block.BinarySearchReference(NumberOfRows, RowSize, _ParentOffset, HasFieldMarshalTag.ConvertToTag(handle), _IsHasFieldMarshalRefSizeSmall);
		return num + 1;
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _ParentOffset, _IsHasFieldMarshalRefSizeSmall);
	}
}
