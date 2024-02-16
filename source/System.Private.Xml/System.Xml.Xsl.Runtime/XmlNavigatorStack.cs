using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal struct XmlNavigatorStack
{
	private XPathNavigator[] _stkNav;

	private int _sp;

	public bool IsEmpty => _sp == 0;

	public void Push(XPathNavigator nav)
	{
		if (_stkNav == null)
		{
			_stkNav = new XPathNavigator[8];
		}
		else if (_sp >= _stkNav.Length)
		{
			XPathNavigator[] stkNav = _stkNav;
			_stkNav = new XPathNavigator[2 * _sp];
			Array.Copy(stkNav, _stkNav, _sp);
		}
		_stkNav[_sp++] = nav;
	}

	public XPathNavigator Pop()
	{
		return _stkNav[--_sp];
	}

	public XPathNavigator Peek()
	{
		return _stkNav[_sp - 1];
	}

	public void Reset()
	{
		_sp = 0;
	}
}
