using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct IdIterator
{
	private XPathNavigator _navCurrent;

	private string[] _idrefs;

	private int _idx;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context, string value)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
		_idrefs = XmlConvert.SplitString(value);
		_idx = -1;
	}

	public bool MoveNext()
	{
		do
		{
			_idx++;
			if (_idx >= _idrefs.Length)
			{
				return false;
			}
		}
		while (!_navCurrent.MoveToId(_idrefs[_idx]));
		return true;
	}
}
