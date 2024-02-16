namespace System.Diagnostics.SymbolStore;

public interface ISymbolReader
{
	SymbolToken UserEntryPoint { get; }

	ISymbolDocument? GetDocument(string url, Guid language, Guid languageVendor, Guid documentType);

	ISymbolDocument[] GetDocuments();

	ISymbolMethod? GetMethod(SymbolToken method);

	ISymbolMethod? GetMethod(SymbolToken method, int version);

	ISymbolVariable[] GetVariables(SymbolToken parent);

	ISymbolVariable[] GetGlobalVariables();

	ISymbolMethod GetMethodFromDocumentPosition(ISymbolDocument document, int line, int column);

	byte[] GetSymAttribute(SymbolToken parent, string name);

	ISymbolNamespace[] GetNamespaces();
}
