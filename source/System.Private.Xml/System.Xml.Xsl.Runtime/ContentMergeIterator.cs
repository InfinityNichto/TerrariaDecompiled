using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct ContentMergeIterator
{
	private enum IteratorState
	{
		NeedCurrent,
		HaveCurrentNeedNext,
		HaveCurrentNoNext,
		HaveCurrentHaveNext
	}

	private XmlNavigatorFilter _filter;

	private XPathNavigator _navCurrent;

	private XPathNavigator _navNext;

	private XmlNavigatorStack _navStack;

	private IteratorState _state;

	public XPathNavigator Current => _navCurrent;

	public void Create(XmlNavigatorFilter filter)
	{
		_filter = filter;
		_navStack.Reset();
		_state = IteratorState.NeedCurrent;
	}

	public IteratorResult MoveNext(XPathNavigator input)
	{
		return MoveNext(input, isContent: true);
	}

	internal IteratorResult MoveNext(XPathNavigator input, bool isContent)
	{
		switch (_state)
		{
		case IteratorState.NeedCurrent:
			if (input == null)
			{
				return IteratorResult.NoMoreNodes;
			}
			_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, input);
			if (isContent ? _filter.MoveToContent(_navCurrent) : _filter.MoveToFollowingSibling(_navCurrent))
			{
				_state = IteratorState.HaveCurrentNeedNext;
			}
			return IteratorResult.NeedInputNode;
		case IteratorState.HaveCurrentNeedNext:
			if (input == null)
			{
				_state = IteratorState.HaveCurrentNoNext;
				return IteratorResult.HaveCurrentNode;
			}
			_navNext = XmlQueryRuntime.SyncToNavigator(_navNext, input);
			if (isContent ? _filter.MoveToContent(_navNext) : _filter.MoveToFollowingSibling(_navNext))
			{
				_state = IteratorState.HaveCurrentHaveNext;
				return DocOrderMerge();
			}
			return IteratorResult.NeedInputNode;
		case IteratorState.HaveCurrentNoNext:
		case IteratorState.HaveCurrentHaveNext:
			if (isContent ? (!_filter.MoveToNextContent(_navCurrent)) : (!_filter.MoveToFollowingSibling(_navCurrent)))
			{
				if (_navStack.IsEmpty)
				{
					if (_state == IteratorState.HaveCurrentNoNext)
					{
						return IteratorResult.NoMoreNodes;
					}
					_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, _navNext);
					_state = IteratorState.HaveCurrentNeedNext;
					return IteratorResult.NeedInputNode;
				}
				_navCurrent = _navStack.Pop();
			}
			if (_state == IteratorState.HaveCurrentNoNext)
			{
				return IteratorResult.HaveCurrentNode;
			}
			return DocOrderMerge();
		default:
			return IteratorResult.NoMoreNodes;
		}
	}

	private IteratorResult DocOrderMerge()
	{
		switch (_navCurrent.ComparePosition(_navNext))
		{
		case XmlNodeOrder.Before:
		case XmlNodeOrder.Unknown:
			return IteratorResult.HaveCurrentNode;
		case XmlNodeOrder.After:
			_navStack.Push(_navCurrent);
			_navCurrent = _navNext;
			_navNext = null;
			break;
		}
		_state = IteratorState.HaveCurrentNeedNext;
		return IteratorResult.NeedInputNode;
	}
}
