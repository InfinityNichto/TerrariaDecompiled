namespace System.Xml.Schema;

internal class Datatype_normalizedStringV1Compat : Datatype_string
{
	public override XmlTypeCode TypeCode => XmlTypeCode.NormalizedString;

	internal override bool HasValueFacets => true;
}
