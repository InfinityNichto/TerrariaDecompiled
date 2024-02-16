using System.Collections;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class XmlFacetComparer : IComparer
{
	public int Compare(object o1, object o2)
	{
		XmlSchemaFacet xmlSchemaFacet = (XmlSchemaFacet)o1;
		XmlSchemaFacet xmlSchemaFacet2 = (XmlSchemaFacet)o2;
		return string.Compare(xmlSchemaFacet.GetType().Name + ":" + xmlSchemaFacet.Value, xmlSchemaFacet2.GetType().Name + ":" + xmlSchemaFacet2.Value, StringComparison.Ordinal);
	}
}
