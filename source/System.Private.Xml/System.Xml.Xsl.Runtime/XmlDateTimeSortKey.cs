namespace System.Xml.Xsl.Runtime;

internal sealed class XmlDateTimeSortKey : XmlIntegerSortKey
{
	public XmlDateTimeSortKey(DateTime value, XmlCollation collation)
		: base(value.Ticks, collation)
	{
	}
}
