using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class XPathDescendantIterator : XPathAxisIterator
{
	private int _level;

	public XPathDescendantIterator(XPathNavigator nav, XPathNodeType type, bool matchSelf)
		: base(nav, type, matchSelf)
	{
	}

	public XPathDescendantIterator(XPathNavigator nav, string name, string namespaceURI, bool matchSelf)
		: base(nav, name, namespaceURI, matchSelf)
	{
	}

	public XPathDescendantIterator(XPathDescendantIterator it)
		: base(it)
	{
		_level = it._level;
	}

	public override XPathNodeIterator Clone()
	{
		return new XPathDescendantIterator(this);
	}

	public override bool MoveNext()
	{
		if (_level == -1)
		{
			return false;
		}
		if (first)
		{
			first = false;
			if (matchSelf && Matches)
			{
				position = 1;
				return true;
			}
		}
		do
		{
			if (nav.MoveToFirstChild())
			{
				_level++;
				continue;
			}
			while (true)
			{
				if (_level == 0)
				{
					_level = -1;
					return false;
				}
				if (nav.MoveToNext())
				{
					break;
				}
				nav.MoveToParent();
				_level--;
			}
		}
		while (!Matches);
		position++;
		return true;
	}
}
