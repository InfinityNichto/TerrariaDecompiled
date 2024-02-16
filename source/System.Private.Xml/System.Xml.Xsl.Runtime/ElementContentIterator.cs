using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct ElementContentIterator
{
	private string _localName;

	private string _ns;

	private XPathNavigator _navCurrent;

	private bool _needFirst;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context, string localName, string ns)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
		_localName = localName;
		_ns = ns;
		_needFirst = true;
	}

	public bool MoveNext()
	{
		if (_needFirst)
		{
			_needFirst = !_navCurrent.MoveToChild(_localName, _ns);
			return !_needFirst;
		}
		return _navCurrent.MoveToNext(_localName, _ns);
	}
}
