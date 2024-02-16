namespace System.Xml.Xsl.Runtime;

internal class XmlIntegerSortKey : XmlSortKey
{
	private readonly long _longVal;

	public XmlIntegerSortKey(long value, XmlCollation collation)
	{
		_longVal = (collation.DescendingOrder ? (~value) : value);
	}

	public override int CompareTo(object obj)
	{
		if (!(obj is XmlIntegerSortKey xmlIntegerSortKey))
		{
			return CompareToEmpty(obj);
		}
		if (_longVal == xmlIntegerSortKey._longVal)
		{
			return BreakSortingTie(xmlIntegerSortKey);
		}
		if (_longVal >= xmlIntegerSortKey._longVal)
		{
			return 1;
		}
		return -1;
	}
}
