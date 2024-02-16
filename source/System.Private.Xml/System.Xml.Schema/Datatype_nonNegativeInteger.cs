namespace System.Xml.Schema;

internal class Datatype_nonNegativeInteger : Datatype_integer
{
	private static readonly FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(0m, decimal.MaxValue);

	internal override FacetsChecker FacetsChecker => s_numeric10FacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.NonNegativeInteger;

	internal override bool HasValueFacets => true;
}
