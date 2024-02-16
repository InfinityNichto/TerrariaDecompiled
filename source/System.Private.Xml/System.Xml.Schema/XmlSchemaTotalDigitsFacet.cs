namespace System.Xml.Schema;

public class XmlSchemaTotalDigitsFacet : XmlSchemaNumericFacet
{
	public XmlSchemaTotalDigitsFacet()
	{
		base.FacetType = FacetType.TotalDigits;
	}
}
