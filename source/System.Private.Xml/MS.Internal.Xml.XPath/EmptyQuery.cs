using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class EmptyQuery : Query
{
	public override int CurrentPosition => 0;

	public override int Count => 0;

	public override QueryProps Properties => (QueryProps)23;

	public override XPathResultType StaticType => XPathResultType.NodeSet;

	public override XPathNavigator Current => null;

	public override XPathNavigator Advance()
	{
		return null;
	}

	public override XPathNodeIterator Clone()
	{
		return this;
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		return this;
	}

	public override void Reset()
	{
	}
}
