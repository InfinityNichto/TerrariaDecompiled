namespace System.Diagnostics.SymbolStore;

public interface ISymbolScope
{
	ISymbolMethod Method { get; }

	ISymbolScope Parent { get; }

	int StartOffset { get; }

	int EndOffset { get; }

	ISymbolScope[] GetChildren();

	ISymbolVariable[] GetLocals();

	ISymbolNamespace[] GetNamespaces();
}
