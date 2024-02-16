using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct NodeKindContentIterator
{
	private XPathNodeType _nodeType;

	private XPathNavigator _navCurrent;

	private bool _needFirst;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context, XPathNodeType nodeType)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
		_nodeType = nodeType;
		_needFirst = true;
	}

	public bool MoveNext()
	{
		if (_needFirst)
		{
			_needFirst = !_navCurrent.MoveToChild(_nodeType);
			return !_needFirst;
		}
		return _navCurrent.MoveToNext(_nodeType);
	}
}
