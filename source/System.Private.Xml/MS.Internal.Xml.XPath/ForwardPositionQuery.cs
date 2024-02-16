using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal class ForwardPositionQuery : CacheOutputQuery
{
	public ForwardPositionQuery(Query input)
		: base(input)
	{
	}

	protected ForwardPositionQuery(ForwardPositionQuery other)
		: base(other)
	{
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		base.Evaluate(context);
		XPathNavigator xPathNavigator;
		while ((xPathNavigator = input.Advance()) != null)
		{
			outputBuffer.Add(xPathNavigator.Clone());
		}
		return this;
	}

	public override XPathNavigator MatchNode(XPathNavigator context)
	{
		return input.MatchNode(context);
	}

	public override XPathNodeIterator Clone()
	{
		return new ForwardPositionQuery(this);
	}
}
