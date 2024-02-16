namespace System.Xml.Schema;

public class XmlSchemaMinLengthFacet : XmlSchemaNumericFacet
{
	public XmlSchemaMinLengthFacet()
	{
		base.FacetType = FacetType.MinLength;
	}
}
