using System.Collections;

namespace System.Xml.Serialization;

internal sealed class XmlAttributeComparer : IComparer
{
	public int Compare(object o1, object o2)
	{
		XmlAttribute xmlAttribute = (XmlAttribute)o1;
		XmlAttribute xmlAttribute2 = (XmlAttribute)o2;
		int num = string.Compare(xmlAttribute.NamespaceURI, xmlAttribute2.NamespaceURI, StringComparison.Ordinal);
		if (num == 0)
		{
			return string.Compare(xmlAttribute.Name, xmlAttribute2.Name, StringComparison.Ordinal);
		}
		return num;
	}
}
