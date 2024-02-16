using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class XPathSingletonIterator : ResetableIterator
{
	private readonly XPathNavigator _nav;

	private int _position;

	public override XPathNavigator Current => _nav;

	public override int CurrentPosition => _position;

	public override int Count => 1;

	public XPathSingletonIterator(XPathNavigator nav)
	{
		_nav = nav;
	}

	public XPathSingletonIterator(XPathNavigator nav, bool moved)
		: this(nav)
	{
		if (moved)
		{
			_position = 1;
		}
	}

	public XPathSingletonIterator(XPathSingletonIterator it)
	{
		_nav = it._nav.Clone();
		_position = it._position;
	}

	public override XPathNodeIterator Clone()
	{
		return new XPathSingletonIterator(this);
	}

	public override bool MoveNext()
	{
		if (_position == 0)
		{
			_position = 1;
			return true;
		}
		return false;
	}

	public override void Reset()
	{
		_position = 0;
	}
}
