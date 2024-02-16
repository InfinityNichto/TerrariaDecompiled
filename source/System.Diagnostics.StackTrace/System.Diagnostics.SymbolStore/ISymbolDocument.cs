namespace System.Diagnostics.SymbolStore;

public interface ISymbolDocument
{
	string URL { get; }

	Guid DocumentType { get; }

	Guid Language { get; }

	Guid LanguageVendor { get; }

	Guid CheckSumAlgorithmId { get; }

	bool HasEmbeddedSource { get; }

	int SourceLength { get; }

	byte[] GetCheckSum();

	int FindClosestLine(int line);

	byte[] GetSourceRange(int startLine, int startColumn, int endLine, int endColumn);
}
