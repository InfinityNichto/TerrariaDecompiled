namespace System.Xml;

internal interface IDtdAttributeInfo
{
	string Prefix { get; }

	string LocalName { get; }

	int LineNumber { get; }

	int LinePosition { get; }

	bool IsNonCDataType { get; }

	bool IsDeclaredInExternal { get; }

	bool IsXmlAttribute { get; }
}
