namespace System.Xml.Schema;

public interface IXmlSchemaInfo
{
	XmlSchemaValidity Validity { get; }

	bool IsDefault { get; }

	bool IsNil { get; }

	XmlSchemaSimpleType? MemberType { get; }

	XmlSchemaType? SchemaType { get; }

	XmlSchemaElement? SchemaElement { get; }

	XmlSchemaAttribute? SchemaAttribute { get; }
}
