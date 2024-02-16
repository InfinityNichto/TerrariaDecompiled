namespace System.Xml.Schema;

internal sealed class Datatype_ENTITY : Datatype_NCName
{
	public override XmlTypeCode TypeCode => XmlTypeCode.Entity;

	public override XmlTokenizedType TokenizedType => XmlTokenizedType.ENTITY;
}
