using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal abstract class XPathDocumentBaseIterator : XPathNodeIterator
{
	protected XPathDocumentNavigator ctxt;

	protected int pos;

	public override XPathNavigator Current => ctxt;

	public override int CurrentPosition => pos;

	protected XPathDocumentBaseIterator(XPathDocumentNavigator ctxt)
	{
		this.ctxt = new XPathDocumentNavigator(ctxt);
	}

	protected XPathDocumentBaseIterator(XPathDocumentBaseIterator iter)
	{
		ctxt = new XPathDocumentNavigator(iter.ctxt);
		pos = iter.pos;
	}
}
