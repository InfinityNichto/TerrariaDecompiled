using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct NodeRangeIterator
{
	private enum IteratorState
	{
		HaveCurrent,
		NeedCurrent,
		HaveCurrentNoNext,
		NoNext
	}

	private XmlNavigatorFilter _filter;

	private XPathNavigator _navCurrent;

	private XPathNavigator _navEnd;

	private IteratorState _state;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator start, XmlNavigatorFilter filter, XPathNavigator end)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, start);
		_navEnd = XmlQueryRuntime.SyncToNavigator(_navEnd, end);
		_filter = filter;
		if (start.IsSamePosition(end))
		{
			_state = ((!filter.IsFiltered(start)) ? IteratorState.HaveCurrentNoNext : IteratorState.NoNext);
		}
		else
		{
			_state = (filter.IsFiltered(start) ? IteratorState.NeedCurrent : IteratorState.HaveCurrent);
		}
	}

	public bool MoveNext()
	{
		switch (_state)
		{
		case IteratorState.HaveCurrent:
			_state = IteratorState.NeedCurrent;
			return true;
		case IteratorState.NeedCurrent:
			if (!_filter.MoveToFollowing(_navCurrent, _navEnd))
			{
				if (_filter.IsFiltered(_navEnd))
				{
					_state = IteratorState.NoNext;
					return false;
				}
				_navCurrent.MoveTo(_navEnd);
				_state = IteratorState.NoNext;
			}
			return true;
		case IteratorState.HaveCurrentNoNext:
			_state = IteratorState.NoNext;
			return true;
		default:
			return false;
		}
	}
}
