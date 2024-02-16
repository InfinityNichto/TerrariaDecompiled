namespace System.Xml.Schema;

public class XmlSchemaLengthFacet : XmlSchemaNumericFacet
{
	public XmlSchemaLengthFacet()
	{
		base.FacetType = FacetType.Length;
	}
}
