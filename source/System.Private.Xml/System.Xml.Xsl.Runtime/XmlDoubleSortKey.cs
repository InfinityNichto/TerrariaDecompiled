namespace System.Xml.Xsl.Runtime;

internal sealed class XmlDoubleSortKey : XmlSortKey
{
	private readonly double _dblVal;

	private readonly bool _isNaN;

	public XmlDoubleSortKey(double value, XmlCollation collation)
	{
		if (double.IsNaN(value))
		{
			_isNaN = true;
			_dblVal = ((collation.EmptyGreatest != collation.DescendingOrder) ? double.PositiveInfinity : double.NegativeInfinity);
		}
		else
		{
			_dblVal = (collation.DescendingOrder ? (0.0 - value) : value);
		}
	}

	public override int CompareTo(object obj)
	{
		if (!(obj is XmlDoubleSortKey xmlDoubleSortKey))
		{
			if (_isNaN)
			{
				return BreakSortingTie(obj as XmlSortKey);
			}
			return CompareToEmpty(obj);
		}
		if (_dblVal == xmlDoubleSortKey._dblVal)
		{
			if (_isNaN)
			{
				if (xmlDoubleSortKey._isNaN)
				{
					return BreakSortingTie(xmlDoubleSortKey);
				}
				if (_dblVal != double.NegativeInfinity)
				{
					return 1;
				}
				return -1;
			}
			if (xmlDoubleSortKey._isNaN)
			{
				if (xmlDoubleSortKey._dblVal != double.NegativeInfinity)
				{
					return -1;
				}
				return 1;
			}
			return BreakSortingTie(xmlDoubleSortKey);
		}
		if (!(_dblVal < xmlDoubleSortKey._dblVal))
		{
			return 1;
		}
		return -1;
	}
}
