namespace System.Xml.Schema;

internal sealed class SchemaNamespaceManager : XmlNamespaceManager
{
	private readonly XmlSchemaObject _node;

	public SchemaNamespaceManager(XmlSchemaObject node)
	{
		_node = node;
	}

	public override string LookupNamespace(string prefix)
	{
		if (prefix == "xml")
		{
			return "http://www.w3.org/XML/1998/namespace";
		}
		for (XmlSchemaObject xmlSchemaObject = _node; xmlSchemaObject != null; xmlSchemaObject = xmlSchemaObject.Parent)
		{
			if (xmlSchemaObject.Namespaces.TryLookupNamespace(prefix, out var ns))
			{
				return ns;
			}
		}
		if (prefix.Length != 0)
		{
			return null;
		}
		return string.Empty;
	}

	public override string LookupPrefix(string ns)
	{
		if (ns == "http://www.w3.org/XML/1998/namespace")
		{
			return "xml";
		}
		for (XmlSchemaObject xmlSchemaObject = _node; xmlSchemaObject != null; xmlSchemaObject = xmlSchemaObject.Parent)
		{
			if (xmlSchemaObject.Namespaces.TryLookupPrefix(ns, out var prefix))
			{
				return prefix;
			}
		}
		return null;
	}
}
