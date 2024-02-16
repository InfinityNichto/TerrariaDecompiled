namespace System.Xml.Schema;

internal sealed class Datatype_IDREF : Datatype_NCName
{
	public override XmlTypeCode TypeCode => XmlTypeCode.Idref;

	public override XmlTokenizedType TokenizedType => XmlTokenizedType.IDREF;
}
