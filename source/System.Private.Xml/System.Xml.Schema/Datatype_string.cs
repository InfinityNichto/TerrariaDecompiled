namespace System.Xml.Schema;

internal class Datatype_string : Datatype_anySimpleType
{
	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Preserve;

	internal override FacetsChecker FacetsChecker => DatatypeImplementation.stringFacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.String;

	public override XmlTokenizedType TokenizedType => XmlTokenizedType.CDATA;

	internal override RestrictionFlags ValidRestrictionFlags => RestrictionFlags.Length | RestrictionFlags.MinLength | RestrictionFlags.MaxLength | RestrictionFlags.Pattern | RestrictionFlags.Enumeration | RestrictionFlags.WhiteSpace;

	internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
	{
		return XmlStringConverter.Create(schemaType);
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		Exception ex = DatatypeImplementation.stringFacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			ex = DatatypeImplementation.stringFacetsChecker.CheckValueFacets(s, this);
			if (ex == null)
			{
				typedValue = s;
				return null;
			}
		}
		return ex;
	}
}
