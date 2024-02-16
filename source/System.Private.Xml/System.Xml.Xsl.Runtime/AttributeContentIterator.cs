using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct AttributeContentIterator
{
	private XPathNavigator _navCurrent;

	private bool _needFirst;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
		_needFirst = true;
	}

	public bool MoveNext()
	{
		if (_needFirst)
		{
			_needFirst = !XmlNavNeverFilter.MoveToFirstAttributeContent(_navCurrent);
			return !_needFirst;
		}
		return XmlNavNeverFilter.MoveToNextAttributeContent(_navCurrent);
	}
}
