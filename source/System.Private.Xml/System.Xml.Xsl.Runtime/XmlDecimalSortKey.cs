namespace System.Xml.Xsl.Runtime;

internal sealed class XmlDecimalSortKey : XmlSortKey
{
	private readonly decimal _decVal;

	public XmlDecimalSortKey(decimal value, XmlCollation collation)
	{
		_decVal = (collation.DescendingOrder ? (-value) : value);
	}

	public override int CompareTo(object obj)
	{
		if (!(obj is XmlDecimalSortKey xmlDecimalSortKey))
		{
			return CompareToEmpty(obj);
		}
		int num = decimal.Compare(_decVal, xmlDecimalSortKey._decVal);
		if (num == 0)
		{
			return BreakSortingTie(xmlDecimalSortKey);
		}
		return num;
	}
}
