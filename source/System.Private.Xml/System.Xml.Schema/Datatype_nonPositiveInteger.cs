namespace System.Xml.Schema;

internal class Datatype_nonPositiveInteger : Datatype_integer
{
	private static readonly FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(decimal.MinValue, 0m);

	internal override FacetsChecker FacetsChecker => s_numeric10FacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.NonPositiveInteger;

	internal override bool HasValueFacets => true;
}
