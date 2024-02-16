namespace System.Diagnostics.SymbolStore;

public interface ISymbolMethod
{
	SymbolToken Token { get; }

	int SequencePointCount { get; }

	ISymbolScope RootScope { get; }

	void GetSequencePoints(int[]? offsets, ISymbolDocument[]? documents, int[]? lines, int[]? columns, int[]? endLines, int[]? endColumns);

	ISymbolScope GetScope(int offset);

	int GetOffset(ISymbolDocument document, int line, int column);

	int[] GetRanges(ISymbolDocument document, int line, int column);

	ISymbolVariable[] GetParameters();

	ISymbolNamespace GetNamespace();

	bool GetSourceStartEnd(ISymbolDocument[]? docs, int[]? lines, int[]? columns);
}
