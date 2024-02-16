using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class DocumentOrderQuery : CacheOutputQuery
{
	public DocumentOrderQuery(Query qyParent)
		: base(qyParent)
	{
	}

	private DocumentOrderQuery(DocumentOrderQuery other)
		: base(other)
	{
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		base.Evaluate(context);
		XPathNavigator nav;
		while ((nav = input.Advance()) != null)
		{
			Query.Insert(outputBuffer, nav);
		}
		return this;
	}

	public override XPathNavigator MatchNode(XPathNavigator context)
	{
		return input.MatchNode(context);
	}

	public override XPathNodeIterator Clone()
	{
		return new DocumentOrderQuery(this);
	}
}
