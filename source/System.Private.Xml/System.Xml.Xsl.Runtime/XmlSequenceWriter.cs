using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal abstract class XmlSequenceWriter
{
	public abstract XmlRawWriter StartTree(XPathNodeType rootType, IXmlNamespaceResolver nsResolver, XmlNameTable nameTable);

	public abstract void EndTree();

	public abstract void WriteItem(XPathItem item);
}
