namespace System.Xml.Schema;

internal sealed class XmlSchemaCollectionNode
{
	private string _namespaceUri;

	private SchemaInfo _schemaInfo;

	private XmlSchema _schema;

	internal string NamespaceURI
	{
		get
		{
			return _namespaceUri;
		}
		set
		{
			_namespaceUri = value;
		}
	}

	internal SchemaInfo SchemaInfo
	{
		get
		{
			return _schemaInfo;
		}
		set
		{
			_schemaInfo = value;
		}
	}

	internal XmlSchema Schema
	{
		get
		{
			return _schema;
		}
		set
		{
			_schema = value;
		}
	}
}
