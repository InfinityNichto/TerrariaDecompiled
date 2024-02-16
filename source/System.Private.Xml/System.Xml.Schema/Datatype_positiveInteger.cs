namespace System.Xml.Schema;

internal sealed class Datatype_positiveInteger : Datatype_nonNegativeInteger
{
	private static readonly FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(1m, decimal.MaxValue);

	internal override FacetsChecker FacetsChecker => s_numeric10FacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.PositiveInteger;
}
