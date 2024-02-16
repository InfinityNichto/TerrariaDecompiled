namespace System.Xml.Schema;

public class XmlSchemaMaxLengthFacet : XmlSchemaNumericFacet
{
	public XmlSchemaMaxLengthFacet()
	{
		base.FacetType = FacetType.MaxLength;
	}
}
