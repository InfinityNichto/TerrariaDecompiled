namespace System.Xml.Schema;

internal sealed class Datatype_ID : Datatype_NCName
{
	public override XmlTypeCode TypeCode => XmlTypeCode.Id;

	public override XmlTokenizedType TokenizedType => XmlTokenizedType.ID;
}
