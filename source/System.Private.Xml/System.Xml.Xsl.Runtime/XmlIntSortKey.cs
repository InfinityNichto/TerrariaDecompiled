namespace System.Xml.Xsl.Runtime;

internal sealed class XmlIntSortKey : XmlSortKey
{
	private readonly int _intVal;

	public XmlIntSortKey(int value, XmlCollation collation)
	{
		_intVal = (collation.DescendingOrder ? (~value) : value);
	}

	public override int CompareTo(object obj)
	{
		if (!(obj is XmlIntSortKey xmlIntSortKey))
		{
			return CompareToEmpty(obj);
		}
		if (_intVal == xmlIntSortKey._intVal)
		{
			return BreakSortingTie(xmlIntSortKey);
		}
		if (_intVal >= xmlIntSortKey._intVal)
		{
			return 1;
		}
		return -1;
	}
}
