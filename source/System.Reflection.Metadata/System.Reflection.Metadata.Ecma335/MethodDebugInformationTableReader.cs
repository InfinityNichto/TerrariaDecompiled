using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct MethodDebugInformationTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _isDocumentRefSmall;

	private readonly bool _isBlobHeapRefSizeSmall;

	private readonly int _sequencePointsOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal MethodDebugInformationTableReader(int numberOfRows, int documentRefSize, int blobHeapRefSize, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_isDocumentRefSmall = documentRefSize == 2;
		_isBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_sequencePointsOffset = documentRefSize;
		RowSize = _sequencePointsOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal DocumentHandle GetDocument(MethodDebugInformationHandle handle)
	{
		int offset = (handle.RowId - 1) * RowSize;
		return DocumentHandle.FromRowId(Block.PeekReference(offset, _isDocumentRefSmall));
	}

	internal BlobHandle GetSequencePoints(MethodDebugInformationHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return BlobHandle.FromOffset(Block.PeekHeapReference(num + _sequencePointsOffset, _isBlobHeapRefSizeSmall));
	}
}
