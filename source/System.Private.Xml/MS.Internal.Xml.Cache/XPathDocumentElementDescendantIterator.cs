using System;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal sealed class XPathDocumentElementDescendantIterator : XPathDocumentBaseIterator
{
	private readonly XPathDocumentNavigator _end;

	private readonly string _localName;

	private readonly string _namespaceUri;

	private bool _matchSelf;

	public XPathDocumentElementDescendantIterator(XPathDocumentNavigator root, string name, string namespaceURI, bool matchSelf)
		: base(root)
	{
		if (namespaceURI == null)
		{
			throw new ArgumentNullException("namespaceURI");
		}
		_localName = root.NameTable.Get(name);
		_namespaceUri = namespaceURI;
		_matchSelf = matchSelf;
		if (root.NodeType != 0)
		{
			_end = new XPathDocumentNavigator(root);
			_end.MoveToNonDescendant();
		}
	}

	public XPathDocumentElementDescendantIterator(XPathDocumentElementDescendantIterator iter)
		: base(iter)
	{
		_end = iter._end;
		_localName = iter._localName;
		_namespaceUri = iter._namespaceUri;
		_matchSelf = iter._matchSelf;
	}

	public override XPathNodeIterator Clone()
	{
		return new XPathDocumentElementDescendantIterator(this);
	}

	public override bool MoveNext()
	{
		if (_matchSelf)
		{
			_matchSelf = false;
			if (ctxt.IsElementMatch(_localName, _namespaceUri))
			{
				pos++;
				return true;
			}
		}
		if (!ctxt.MoveToFollowing(_localName, _namespaceUri, _end))
		{
			return false;
		}
		pos++;
		return true;
	}
}
