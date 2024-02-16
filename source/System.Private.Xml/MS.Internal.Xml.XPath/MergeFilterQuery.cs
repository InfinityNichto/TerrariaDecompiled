using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class MergeFilterQuery : CacheOutputQuery
{
	private readonly Query _child;

	public MergeFilterQuery(Query input, Query child)
		: base(input)
	{
		_child = child;
	}

	private MergeFilterQuery(MergeFilterQuery other)
		: base(other)
	{
		_child = Query.Clone(other._child);
	}

	public override void SetXsltContext(XsltContext xsltContext)
	{
		base.SetXsltContext(xsltContext);
		_child.SetXsltContext(xsltContext);
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		base.Evaluate(nodeIterator);
		while (input.Advance() != null)
		{
			_child.Evaluate(input);
			XPathNavigator nav;
			while ((nav = _child.Advance()) != null)
			{
				Query.Insert(outputBuffer, nav);
			}
		}
		return this;
	}

	public override XPathNavigator MatchNode(XPathNavigator current)
	{
		XPathNavigator xPathNavigator = _child.MatchNode(current);
		if (xPathNavigator == null)
		{
			return null;
		}
		xPathNavigator = input.MatchNode(xPathNavigator);
		if (xPathNavigator == null)
		{
			return null;
		}
		Evaluate(new XPathSingletonIterator(xPathNavigator.Clone(), moved: true));
		for (XPathNavigator xPathNavigator2 = Advance(); xPathNavigator2 != null; xPathNavigator2 = Advance())
		{
			if (xPathNavigator2.IsSamePosition(current))
			{
				return xPathNavigator;
			}
		}
		return null;
	}

	public override XPathNodeIterator Clone()
	{
		return new MergeFilterQuery(this);
	}
}
