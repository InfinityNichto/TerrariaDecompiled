using System.Xml.Schema;

namespace System.Xml;

internal sealed class AttributePSVIInfo
{
	internal string localName;

	internal string namespaceUri;

	internal object typedAttributeValue;

	internal XmlSchemaInfo attributeSchemaInfo;

	internal AttributePSVIInfo()
	{
		attributeSchemaInfo = new XmlSchemaInfo();
	}

	internal void Reset()
	{
		typedAttributeValue = null;
		localName = string.Empty;
		namespaceUri = string.Empty;
		attributeSchemaInfo.Clear();
	}
}
