namespace System.Xml.Schema;

internal class Datatype_normalizedString : Datatype_string
{
	public override XmlTypeCode TypeCode => XmlTypeCode.NormalizedString;

	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Replace;

	internal override bool HasValueFacets => true;
}
