using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal sealed class XPathDocumentKindChildIterator : XPathDocumentBaseIterator
{
	private readonly XPathNodeType _typ;

	public XPathDocumentKindChildIterator(XPathDocumentNavigator parent, XPathNodeType typ)
		: base(parent)
	{
		_typ = typ;
	}

	public XPathDocumentKindChildIterator(XPathDocumentKindChildIterator iter)
		: base(iter)
	{
		_typ = iter._typ;
	}

	public override XPathNodeIterator Clone()
	{
		return new XPathDocumentKindChildIterator(this);
	}

	public override bool MoveNext()
	{
		if (pos == 0)
		{
			if (!ctxt.MoveToChild(_typ))
			{
				return false;
			}
		}
		else if (!ctxt.MoveToNext(_typ))
		{
			return false;
		}
		pos++;
		return true;
	}
}
