using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal sealed class XPathDocumentKindDescendantIterator : XPathDocumentBaseIterator
{
	private readonly XPathDocumentNavigator _end;

	private readonly XPathNodeType _typ;

	private bool _matchSelf;

	public XPathDocumentKindDescendantIterator(XPathDocumentNavigator root, XPathNodeType typ, bool matchSelf)
		: base(root)
	{
		_typ = typ;
		_matchSelf = matchSelf;
		if (root.NodeType != 0)
		{
			_end = new XPathDocumentNavigator(root);
			_end.MoveToNonDescendant();
		}
	}

	public XPathDocumentKindDescendantIterator(XPathDocumentKindDescendantIterator iter)
		: base(iter)
	{
		_end = iter._end;
		_typ = iter._typ;
		_matchSelf = iter._matchSelf;
	}

	public override XPathNodeIterator Clone()
	{
		return new XPathDocumentKindDescendantIterator(this);
	}

	public override bool MoveNext()
	{
		if (_matchSelf)
		{
			_matchSelf = false;
			if (ctxt.IsKindMatch(_typ))
			{
				pos++;
				return true;
			}
		}
		if (!ctxt.MoveToFollowing(_typ, _end))
		{
			return false;
		}
		pos++;
		return true;
	}
}
