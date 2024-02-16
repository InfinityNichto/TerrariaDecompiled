using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct PrecedingSiblingDocOrderIterator
{
	private XmlNavigatorFilter _filter;

	private XPathNavigator _navCurrent;

	private XPathNavigator _navEnd;

	private bool _needFirst;

	private bool _useCompPos;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context, XmlNavigatorFilter filter)
	{
		_filter = filter;
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
		_navEnd = XmlQueryRuntime.SyncToNavigator(_navEnd, context);
		_needFirst = true;
		_useCompPos = _filter.IsFiltered(context);
	}

	public bool MoveNext()
	{
		if (_needFirst)
		{
			if (!_navCurrent.MoveToParent())
			{
				return false;
			}
			if (!_filter.MoveToContent(_navCurrent))
			{
				return false;
			}
			_needFirst = false;
		}
		else if (!_filter.MoveToFollowingSibling(_navCurrent))
		{
			return false;
		}
		if (_useCompPos)
		{
			return _navCurrent.ComparePosition(_navEnd) == XmlNodeOrder.Before;
		}
		if (_navCurrent.IsSamePosition(_navEnd))
		{
			_useCompPos = true;
			return false;
		}
		return true;
	}
}
