using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class XPathAncestorQuery : CacheAxisQuery
{
	private readonly bool _matchSelf;

	public override int CurrentPosition => outputBuffer.Count - count + 1;

	public override QueryProps Properties => base.Properties | QueryProps.Reverse;

	public XPathAncestorQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest, bool matchSelf)
		: base(qyInput, name, prefix, typeTest)
	{
		_matchSelf = matchSelf;
	}

	private XPathAncestorQuery(XPathAncestorQuery other)
		: base(other)
	{
		_matchSelf = other._matchSelf;
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		base.Evaluate(context);
		XPathNavigator xPathNavigator = null;
		XPathNavigator xPathNavigator2;
		while ((xPathNavigator2 = qyInput.Advance()) != null)
		{
			if (!_matchSelf || !matches(xPathNavigator2) || Query.Insert(outputBuffer, xPathNavigator2))
			{
				if (xPathNavigator == null || !xPathNavigator.MoveTo(xPathNavigator2))
				{
					xPathNavigator = xPathNavigator2.Clone();
				}
				while (xPathNavigator.MoveToParent() && (!matches(xPathNavigator) || Query.Insert(outputBuffer, xPathNavigator)))
				{
				}
			}
		}
		return this;
	}

	public override XPathNodeIterator Clone()
	{
		return new XPathAncestorQuery(this);
	}
}
