using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct AssemblyProcessorTableReader
{
	internal readonly int NumberOfRows;

	private readonly int _ProcessorOffset;

	internal readonly int RowSize;

	internal readonly MemoryBlock Block;

	internal AssemblyProcessorTableReader(int numberOfRows, MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_ProcessorOffset = 0;
		RowSize = _ProcessorOffset + 4;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}
}
