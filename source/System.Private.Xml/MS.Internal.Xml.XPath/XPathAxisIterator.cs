using System;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal abstract class XPathAxisIterator : XPathNodeIterator
{
	internal XPathNavigator nav;

	internal XPathNodeType type;

	internal string name;

	internal string uri;

	internal int position;

	internal bool matchSelf;

	internal bool first = true;

	public override XPathNavigator Current => nav;

	public override int CurrentPosition => position;

	protected virtual bool Matches
	{
		get
		{
			if (name == null)
			{
				if (type != nav.NodeType && type != XPathNodeType.All)
				{
					if (type == XPathNodeType.Text)
					{
						if (nav.NodeType != XPathNodeType.Whitespace)
						{
							return nav.NodeType == XPathNodeType.SignificantWhitespace;
						}
						return true;
					}
					return false;
				}
				return true;
			}
			if (nav.NodeType == XPathNodeType.Element && (name.Length == 0 || name == nav.LocalName))
			{
				return uri == nav.NamespaceURI;
			}
			return false;
		}
	}

	public XPathAxisIterator(XPathNavigator nav, bool matchSelf)
	{
		this.nav = nav;
		this.matchSelf = matchSelf;
	}

	public XPathAxisIterator(XPathNavigator nav, XPathNodeType type, bool matchSelf)
		: this(nav, matchSelf)
	{
		this.type = type;
	}

	public XPathAxisIterator(XPathNavigator nav, string name, string namespaceURI, bool matchSelf)
		: this(nav, matchSelf)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (namespaceURI == null)
		{
			throw new ArgumentNullException("namespaceURI");
		}
		this.name = name;
		uri = namespaceURI;
	}

	public XPathAxisIterator(XPathAxisIterator it)
	{
		nav = it.nav.Clone();
		type = it.type;
		name = it.name;
		uri = it.uri;
		position = it.position;
		matchSelf = it.matchSelf;
		first = it.first;
	}
}
