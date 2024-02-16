using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct XPathFollowingMergeIterator
{
	private enum IteratorState
	{
		NeedCandidateCurrent,
		HaveCandidateCurrent,
		HaveCurrentNeedNext,
		HaveCurrentHaveNext,
		HaveCurrentNoNext
	}

	private XmlNavigatorFilter _filter;

	private IteratorState _state;

	private XPathNavigator _navCurrent;

	private XPathNavigator _navNext;

	public XPathNavigator Current => _navCurrent;

	public void Create(XmlNavigatorFilter filter)
	{
		_filter = filter;
		_state = IteratorState.NeedCandidateCurrent;
	}

	public IteratorResult MoveNext(XPathNavigator input)
	{
		switch (_state)
		{
		case IteratorState.NeedCandidateCurrent:
			if (input == null)
			{
				return IteratorResult.NoMoreNodes;
			}
			_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, input);
			_state = IteratorState.HaveCandidateCurrent;
			return IteratorResult.NeedInputNode;
		case IteratorState.HaveCandidateCurrent:
			if (input == null)
			{
				_state = IteratorState.HaveCurrentNoNext;
				return MoveFirst();
			}
			if (_navCurrent.IsDescendant(input))
			{
				goto case IteratorState.NeedCandidateCurrent;
			}
			_state = IteratorState.HaveCurrentNeedNext;
			goto case IteratorState.HaveCurrentNeedNext;
		case IteratorState.HaveCurrentNeedNext:
			if (input == null)
			{
				_state = IteratorState.HaveCurrentNoNext;
				return MoveFirst();
			}
			if (_navCurrent.ComparePosition(input) != XmlNodeOrder.Unknown)
			{
				return IteratorResult.NeedInputNode;
			}
			_navNext = XmlQueryRuntime.SyncToNavigator(_navNext, input);
			_state = IteratorState.HaveCurrentHaveNext;
			return MoveFirst();
		default:
			if (!_filter.MoveToFollowing(_navCurrent, null))
			{
				return MoveFailed();
			}
			return IteratorResult.HaveCurrentNode;
		}
	}

	private IteratorResult MoveFailed()
	{
		if (_state == IteratorState.HaveCurrentNoNext)
		{
			_state = IteratorState.NeedCandidateCurrent;
			return IteratorResult.NoMoreNodes;
		}
		_state = IteratorState.HaveCandidateCurrent;
		XPathNavigator navCurrent = _navCurrent;
		_navCurrent = _navNext;
		_navNext = navCurrent;
		return IteratorResult.NeedInputNode;
	}

	private IteratorResult MoveFirst()
	{
		if (!XPathFollowingIterator.MoveFirst(_filter, _navCurrent))
		{
			return MoveFailed();
		}
		return IteratorResult.HaveCurrentNode;
	}
}
