namespace System.Xml;

internal interface IDtdEntityInfo
{
	string Name { get; }

	bool IsExternal { get; }

	bool IsDeclaredInExternal { get; }

	bool IsUnparsedEntity { get; }

	bool IsParameterEntity { get; }

	string BaseUriString { get; }

	string DeclaredUriString { get; }

	string SystemId { get; }

	string PublicId { get; }

	string Text { get; }

	int LineNumber { get; }

	int LinePosition { get; }
}
