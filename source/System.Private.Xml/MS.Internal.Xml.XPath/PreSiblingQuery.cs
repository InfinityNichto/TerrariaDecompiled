using System.Collections.Generic;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class PreSiblingQuery : CacheAxisQuery
{
	public override QueryProps Properties => base.Properties | QueryProps.Reverse;

	public PreSiblingQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest)
		: base(qyInput, name, prefix, typeTest)
	{
	}

	private PreSiblingQuery(PreSiblingQuery other)
		: base(other)
	{
	}

	private static bool NotVisited(XPathNavigator nav, List<XPathNavigator> parentStk)
	{
		XPathNavigator xPathNavigator = nav.Clone();
		xPathNavigator.MoveToParent();
		for (int i = 0; i < parentStk.Count; i++)
		{
			if (xPathNavigator.IsSamePosition(parentStk[i]))
			{
				return false;
			}
		}
		parentStk.Add(xPathNavigator);
		return true;
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		base.Evaluate(context);
		List<XPathNavigator> parentStk = new List<XPathNavigator>();
		Stack<XPathNavigator> stack = new Stack<XPathNavigator>();
		while ((currentNode = qyInput.Advance()) != null)
		{
			stack.Push(currentNode.Clone());
		}
		while (stack.Count != 0)
		{
			XPathNavigator xPathNavigator = stack.Pop();
			if (xPathNavigator.NodeType == XPathNodeType.Attribute || xPathNavigator.NodeType == XPathNodeType.Namespace || !NotVisited(xPathNavigator, parentStk))
			{
				continue;
			}
			XPathNavigator xPathNavigator2 = xPathNavigator.Clone();
			if (!xPathNavigator2.MoveToParent())
			{
				continue;
			}
			bool flag = xPathNavigator2.MoveToFirstChild();
			while (!xPathNavigator2.IsSamePosition(xPathNavigator))
			{
				if (matches(xPathNavigator2))
				{
					Query.Insert(outputBuffer, xPathNavigator2);
				}
				if (!xPathNavigator2.MoveToNext())
				{
					break;
				}
			}
		}
		return this;
	}

	public override XPathNodeIterator Clone()
	{
		return new PreSiblingQuery(this);
	}
}
