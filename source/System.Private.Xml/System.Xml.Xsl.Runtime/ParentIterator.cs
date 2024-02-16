using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct ParentIterator
{
	private XPathNavigator _navCurrent;

	private bool _haveCurrent;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context, XmlNavigatorFilter filter)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
		_haveCurrent = _navCurrent.MoveToParent() && !filter.IsFiltered(_navCurrent);
	}

	public bool MoveNext()
	{
		if (_haveCurrent)
		{
			_haveCurrent = false;
			return true;
		}
		return false;
	}
}
