namespace System.Xml.Schema;

internal class Datatype_anyAtomicType : Datatype_anySimpleType
{
	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Preserve;

	public override XmlTypeCode TypeCode => XmlTypeCode.AnyAtomicType;

	internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
	{
		return XmlAnyConverter.AnyAtomic;
	}
}
