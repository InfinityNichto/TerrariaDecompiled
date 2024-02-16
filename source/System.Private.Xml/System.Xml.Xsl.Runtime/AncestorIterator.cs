using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct AncestorIterator
{
	private XmlNavigatorFilter _filter;

	private XPathNavigator _navCurrent;

	private bool _haveCurrent;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context, XmlNavigatorFilter filter, bool orSelf)
	{
		_filter = filter;
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
		_haveCurrent = orSelf && !_filter.IsFiltered(_navCurrent);
	}

	public bool MoveNext()
	{
		if (_haveCurrent)
		{
			_haveCurrent = false;
			return true;
		}
		while (_navCurrent.MoveToParent())
		{
			if (!_filter.IsFiltered(_navCurrent))
			{
				return true;
			}
		}
		return false;
	}
}
