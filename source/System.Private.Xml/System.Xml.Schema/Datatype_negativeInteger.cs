namespace System.Xml.Schema;

internal sealed class Datatype_negativeInteger : Datatype_nonPositiveInteger
{
	private static readonly FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(decimal.MinValue, -1m);

	internal override FacetsChecker FacetsChecker => s_numeric10FacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.NegativeInteger;
}
