using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct LocalConstantTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _isStringHeapRefSizeSmall;

	private readonly bool _isBlobHeapRefSizeSmall;

	private readonly int _signatureOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal LocalConstantTableReader(int numberOfRows, int stringHeapRefSize, int blobHeapRefSize, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_isStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_isBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_signatureOffset = stringHeapRefSize;
		RowSize = _signatureOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal StringHandle GetName(LocalConstantHandle handle)
	{
		int offset = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(offset, _isStringHeapRefSizeSmall));
	}

	internal BlobHandle GetSignature(LocalConstantHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return BlobHandle.FromOffset(Block.PeekHeapReference(num + _signatureOffset, _isBlobHeapRefSizeSmall));
	}
}
