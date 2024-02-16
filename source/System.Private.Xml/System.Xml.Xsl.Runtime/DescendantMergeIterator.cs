using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct DescendantMergeIterator
{
	private enum IteratorState
	{
		NoPrevious,
		NeedCurrent,
		NeedDescendant
	}

	private XmlNavigatorFilter _filter;

	private XPathNavigator _navCurrent;

	private XPathNavigator _navRoot;

	private XPathNavigator _navEnd;

	private IteratorState _state;

	private bool _orSelf;

	public XPathNavigator Current => _navCurrent;

	public void Create(XmlNavigatorFilter filter, bool orSelf)
	{
		_filter = filter;
		_state = IteratorState.NoPrevious;
		_orSelf = orSelf;
	}

	public IteratorResult MoveNext(XPathNavigator input)
	{
		if (_state != IteratorState.NeedDescendant)
		{
			if (input == null)
			{
				return IteratorResult.NoMoreNodes;
			}
			if (_state != 0 && _navRoot.IsDescendant(input))
			{
				return IteratorResult.NeedInputNode;
			}
			_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, input);
			_navRoot = XmlQueryRuntime.SyncToNavigator(_navRoot, input);
			_navEnd = XmlQueryRuntime.SyncToNavigator(_navEnd, input);
			_navEnd.MoveToNonDescendant();
			_state = IteratorState.NeedDescendant;
			if (_orSelf && !_filter.IsFiltered(input))
			{
				return IteratorResult.HaveCurrentNode;
			}
		}
		if (_filter.MoveToFollowing(_navCurrent, _navEnd))
		{
			return IteratorResult.HaveCurrentNode;
		}
		_state = IteratorState.NeedCurrent;
		return IteratorResult.NeedInputNode;
	}
}
