namespace System.Linq.Expressions;

internal sealed class SymbolDocumentWithGuids : SymbolDocumentInfo
{
	public override Guid Language { get; }

	public override Guid LanguageVendor { get; }

	public override Guid DocumentType { get; }

	internal SymbolDocumentWithGuids(string fileName, ref Guid language)
		: base(fileName)
	{
		Language = language;
		DocumentType = SymbolDocumentInfo.DocumentType_Text;
	}

	internal SymbolDocumentWithGuids(string fileName, ref Guid language, ref Guid vendor)
		: base(fileName)
	{
		Language = language;
		LanguageVendor = vendor;
		DocumentType = SymbolDocumentInfo.DocumentType_Text;
	}

	internal SymbolDocumentWithGuids(string fileName, ref Guid language, ref Guid vendor, ref Guid documentType)
		: base(fileName)
	{
		Language = language;
		LanguageVendor = vendor;
		DocumentType = documentType;
	}
}
