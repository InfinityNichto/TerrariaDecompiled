namespace System.Xml.Schema;

internal sealed class Datatype_tokenV1Compat : Datatype_normalizedStringV1Compat
{
	public override XmlTypeCode TypeCode => XmlTypeCode.Token;
}
