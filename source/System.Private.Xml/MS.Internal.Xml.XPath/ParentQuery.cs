using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class ParentQuery : CacheAxisQuery
{
	public ParentQuery(Query qyInput, string Name, string Prefix, XPathNodeType Type)
		: base(qyInput, Name, Prefix, Type)
	{
	}

	private ParentQuery(ParentQuery other)
		: base(other)
	{
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		base.Evaluate(context);
		XPathNavigator xPathNavigator;
		while ((xPathNavigator = qyInput.Advance()) != null)
		{
			xPathNavigator = xPathNavigator.Clone();
			if (xPathNavigator.MoveToParent() && matches(xPathNavigator))
			{
				Query.Insert(outputBuffer, xPathNavigator);
			}
		}
		return this;
	}

	public override XPathNodeIterator Clone()
	{
		return new ParentQuery(this);
	}
}
