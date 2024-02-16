using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct PrecedingSiblingIterator
{
	private XmlNavigatorFilter _filter;

	private XPathNavigator _navCurrent;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context, XmlNavigatorFilter filter)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
		_filter = filter;
	}

	public bool MoveNext()
	{
		return _filter.MoveToPreviousSibling(_navCurrent);
	}
}
