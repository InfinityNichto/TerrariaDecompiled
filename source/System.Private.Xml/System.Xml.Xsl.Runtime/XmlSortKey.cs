namespace System.Xml.Xsl.Runtime;

internal abstract class XmlSortKey : IComparable
{
	private int _priority;

	private XmlSortKey _nextKey;

	public int Priority
	{
		set
		{
			for (XmlSortKey xmlSortKey = this; xmlSortKey != null; xmlSortKey = xmlSortKey._nextKey)
			{
				xmlSortKey._priority = value;
			}
		}
	}

	public XmlSortKey AddSortKey(XmlSortKey sortKey)
	{
		if (_nextKey != null)
		{
			_nextKey.AddSortKey(sortKey);
		}
		else
		{
			_nextKey = sortKey;
		}
		return this;
	}

	protected int BreakSortingTie(XmlSortKey that)
	{
		if (_nextKey != null)
		{
			return _nextKey.CompareTo(that._nextKey);
		}
		if (_priority >= that._priority)
		{
			return 1;
		}
		return -1;
	}

	protected int CompareToEmpty(object obj)
	{
		XmlEmptySortKey xmlEmptySortKey = obj as XmlEmptySortKey;
		if (!xmlEmptySortKey.IsEmptyGreatest)
		{
			return 1;
		}
		return -1;
	}

	public abstract int CompareTo(object that);
}
