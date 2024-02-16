using System.Collections;

namespace System.Xml.Serialization;

internal sealed class QNameComparer : IComparer
{
	public int Compare(object o1, object o2)
	{
		XmlQualifiedName xmlQualifiedName = (XmlQualifiedName)o1;
		XmlQualifiedName xmlQualifiedName2 = (XmlQualifiedName)o2;
		int num = string.Compare(xmlQualifiedName.Namespace, xmlQualifiedName2.Namespace, StringComparison.Ordinal);
		if (num == 0)
		{
			return string.Compare(xmlQualifiedName.Name, xmlQualifiedName2.Name, StringComparison.Ordinal);
		}
		return num;
	}
}
