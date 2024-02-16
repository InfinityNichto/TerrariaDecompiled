namespace System.Xml.Xsl.Runtime;

internal sealed class XmlEmptySortKey : XmlSortKey
{
	private readonly bool _isEmptyGreatest;

	public bool IsEmptyGreatest => _isEmptyGreatest;

	public XmlEmptySortKey(XmlCollation collation)
	{
		_isEmptyGreatest = collation.EmptyGreatest != collation.DescendingOrder;
	}

	public override int CompareTo(object obj)
	{
		if (!(obj is XmlEmptySortKey that))
		{
			return -(obj as XmlSortKey).CompareTo(this);
		}
		return BreakSortingTie(that);
	}
}
