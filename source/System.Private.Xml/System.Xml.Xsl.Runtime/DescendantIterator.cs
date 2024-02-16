using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct DescendantIterator
{
	private XmlNavigatorFilter _filter;

	private XPathNavigator _navCurrent;

	private XPathNavigator _navEnd;

	private bool _hasFirst;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator input, XmlNavigatorFilter filter, bool orSelf)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, input);
		_filter = filter;
		if (input.NodeType == XPathNodeType.Root)
		{
			_navEnd = null;
		}
		else
		{
			_navEnd = XmlQueryRuntime.SyncToNavigator(_navEnd, input);
			_navEnd.MoveToNonDescendant();
		}
		_hasFirst = orSelf && !_filter.IsFiltered(_navCurrent);
	}

	public bool MoveNext()
	{
		if (_hasFirst)
		{
			_hasFirst = false;
			return true;
		}
		return _filter.MoveToFollowing(_navCurrent, _navEnd);
	}
}
