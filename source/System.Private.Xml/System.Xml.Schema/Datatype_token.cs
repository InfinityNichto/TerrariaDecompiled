namespace System.Xml.Schema;

internal class Datatype_token : Datatype_normalizedString
{
	public override XmlTypeCode TypeCode => XmlTypeCode.Token;

	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Collapse;
}
