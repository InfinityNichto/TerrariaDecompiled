using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct XPathFollowingIterator
{
	private XmlNavigatorFilter _filter;

	private XPathNavigator _navCurrent;

	private bool _needFirst;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator input, XmlNavigatorFilter filter)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, input);
		_filter = filter;
		_needFirst = true;
	}

	public bool MoveNext()
	{
		if (_needFirst)
		{
			if (!MoveFirst(_filter, _navCurrent))
			{
				return false;
			}
			_needFirst = false;
			return true;
		}
		return _filter.MoveToFollowing(_navCurrent, null);
	}

	internal static bool MoveFirst(XmlNavigatorFilter filter, XPathNavigator nav)
	{
		if (nav.NodeType == XPathNodeType.Attribute || nav.NodeType == XPathNodeType.Namespace)
		{
			if (!nav.MoveToParent())
			{
				return false;
			}
			if (!filter.MoveToFollowing(nav, null))
			{
				return false;
			}
		}
		else
		{
			if (!nav.MoveToNonDescendant())
			{
				return false;
			}
			if (filter.IsFiltered(nav) && !filter.MoveToFollowing(nav, null))
			{
				return false;
			}
		}
		return true;
	}
}
