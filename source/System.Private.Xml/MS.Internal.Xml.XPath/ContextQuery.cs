using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal class ContextQuery : Query
{
	protected XPathNavigator contextNode;

	public override XPathNavigator Current => contextNode;

	public override XPathResultType StaticType => XPathResultType.NodeSet;

	public override int CurrentPosition => count;

	public override int Count => 1;

	public override QueryProps Properties => (QueryProps)23;

	public ContextQuery()
	{
		count = 0;
	}

	protected ContextQuery(ContextQuery other)
		: base(other)
	{
		contextNode = other.contextNode;
	}

	public override void Reset()
	{
		count = 0;
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		contextNode = context.Current;
		count = 0;
		return this;
	}

	public override XPathNavigator Advance()
	{
		if (count == 0)
		{
			count = 1;
			return contextNode;
		}
		return null;
	}

	public override XPathNavigator MatchNode(XPathNavigator current)
	{
		return current;
	}

	public override XPathNodeIterator Clone()
	{
		return new ContextQuery(this);
	}
}
