using System.Globalization;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlStringSortKey : XmlSortKey
{
	private readonly SortKey _sortKey;

	private readonly byte[] _sortKeyBytes;

	private readonly bool _descendingOrder;

	public XmlStringSortKey(SortKey sortKey, bool descendingOrder)
	{
		_sortKey = sortKey;
		_descendingOrder = descendingOrder;
	}

	public XmlStringSortKey(byte[] sortKey, bool descendingOrder)
	{
		_sortKeyBytes = sortKey;
		_descendingOrder = descendingOrder;
	}

	public override int CompareTo(object obj)
	{
		if (!(obj is XmlStringSortKey xmlStringSortKey))
		{
			return CompareToEmpty(obj);
		}
		int num;
		if (_sortKey != null)
		{
			num = SortKey.Compare(_sortKey, xmlStringSortKey._sortKey);
		}
		else
		{
			int num2 = ((_sortKeyBytes.Length < xmlStringSortKey._sortKeyBytes.Length) ? _sortKeyBytes.Length : xmlStringSortKey._sortKeyBytes.Length);
			int num3 = 0;
			while (true)
			{
				if (num3 < num2)
				{
					if (_sortKeyBytes[num3] < xmlStringSortKey._sortKeyBytes[num3])
					{
						num = -1;
						break;
					}
					if (_sortKeyBytes[num3] > xmlStringSortKey._sortKeyBytes[num3])
					{
						num = 1;
						break;
					}
					num3++;
					continue;
				}
				num = ((_sortKeyBytes.Length >= xmlStringSortKey._sortKeyBytes.Length) ? ((_sortKeyBytes.Length > xmlStringSortKey._sortKeyBytes.Length) ? 1 : 0) : (-1));
				break;
			}
		}
		if (num == 0)
		{
			return BreakSortingTie(xmlStringSortKey);
		}
		if (!_descendingOrder)
		{
			return num;
		}
		return -num;
	}
}
