using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct XPathPrecedingMergeIterator
{
	private enum IteratorState
	{
		NeedCandidateCurrent,
		HaveCandidateCurrent,
		HaveCurrentHaveNext,
		HaveCurrentNoNext
	}

	private XmlNavigatorFilter _filter;

	private IteratorState _state;

	private XPathNavigator _navCurrent;

	private XPathNavigator _navNext;

	private XmlNavigatorStack _navStack;

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
			}
			else
			{
				if (_navCurrent.ComparePosition(input) != XmlNodeOrder.Unknown)
				{
					_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, input);
					return IteratorResult.NeedInputNode;
				}
				_navNext = XmlQueryRuntime.SyncToNavigator(_navNext, input);
				_state = IteratorState.HaveCurrentHaveNext;
			}
			PushAncestors();
			break;
		}
		if (!_navStack.IsEmpty)
		{
			do
			{
				if (_filter.MoveToFollowing(_navCurrent, _navStack.Peek()))
				{
					return IteratorResult.HaveCurrentNode;
				}
				_navCurrent.MoveTo(_navStack.Pop());
			}
			while (!_navStack.IsEmpty);
		}
		if (_state == IteratorState.HaveCurrentNoNext)
		{
			_state = IteratorState.NeedCandidateCurrent;
			return IteratorResult.NoMoreNodes;
		}
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, _navNext);
		_state = IteratorState.HaveCandidateCurrent;
		return IteratorResult.HaveCurrentNode;
	}

	private void PushAncestors()
	{
		_navStack.Reset();
		do
		{
			_navStack.Push(_navCurrent.Clone());
		}
		while (_navCurrent.MoveToParent());
		_navStack.Pop();
	}
}
