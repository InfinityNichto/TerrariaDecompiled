using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class IDQuery : CacheOutputQuery
{
	public IDQuery(Query arg)
		: base(arg)
	{
	}

	private IDQuery(IDQuery other)
		: base(other)
	{
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		object obj = base.Evaluate(context);
		XPathNavigator contextNode = context.Current.Clone();
		switch (GetXPathType(obj))
		{
		case XPathResultType.NodeSet:
		{
			XPathNavigator xPathNavigator;
			while ((xPathNavigator = input.Advance()) != null)
			{
				ProcessIds(contextNode, xPathNavigator.Value);
			}
			break;
		}
		case XPathResultType.String:
			ProcessIds(contextNode, (string)obj);
			break;
		case XPathResultType.Number:
			ProcessIds(contextNode, StringFunctions.toString((double)obj));
			break;
		case XPathResultType.Boolean:
			ProcessIds(contextNode, StringFunctions.toString((bool)obj));
			break;
		case (XPathResultType)4:
			ProcessIds(contextNode, ((XPathNavigator)obj).Value);
			break;
		}
		return this;
	}

	private void ProcessIds(XPathNavigator contextNode, string val)
	{
		string[] array = XmlConvert.SplitString(val);
		for (int i = 0; i < array.Length; i++)
		{
			if (contextNode.MoveToId(array[i]))
			{
				Query.Insert(outputBuffer, contextNode);
			}
		}
	}

	public override XPathNavigator MatchNode(XPathNavigator context)
	{
		Evaluate(new XPathSingletonIterator(context, moved: true));
		XPathNavigator xPathNavigator;
		while ((xPathNavigator = Advance()) != null)
		{
			if (xPathNavigator.IsSamePosition(context))
			{
				return context;
			}
		}
		return null;
	}

	public override XPathNodeIterator Clone()
	{
		return new IDQuery(this);
	}
}
