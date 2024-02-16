using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal class ChildrenQuery : BaseAxisQuery
{
	private XPathNodeIterator _iterator = XPathEmptyIterator.Instance;

	public ChildrenQuery(Query qyInput, string name, string prefix, XPathNodeType type)
		: base(qyInput, name, prefix, type)
	{
	}

	protected ChildrenQuery(ChildrenQuery other)
		: base(other)
	{
		_iterator = Query.Clone(other._iterator);
	}

	public override void Reset()
	{
		_iterator = XPathEmptyIterator.Instance;
		base.Reset();
	}

	public override XPathNavigator Advance()
	{
		while (!_iterator.MoveNext())
		{
			XPathNavigator xPathNavigator = qyInput.Advance();
			if (xPathNavigator == null)
			{
				return null;
			}
			if (base.NameTest)
			{
				if (base.TypeTest == XPathNodeType.ProcessingInstruction)
				{
					_iterator = new IteratorFilter(xPathNavigator.SelectChildren(base.TypeTest), base.Name);
				}
				else
				{
					_iterator = xPathNavigator.SelectChildren(base.Name, base.Namespace);
				}
			}
			else
			{
				_iterator = xPathNavigator.SelectChildren(base.TypeTest);
			}
			position = 0;
		}
		position++;
		currentNode = _iterator.Current;
		return currentNode;
	}

	public sealed override XPathNavigator MatchNode(XPathNavigator context)
	{
		if (context != null && matches(context))
		{
			XPathNavigator xPathNavigator = context.Clone();
			if (xPathNavigator.NodeType != XPathNodeType.Attribute && xPathNavigator.MoveToParent())
			{
				return qyInput.MatchNode(xPathNavigator);
			}
			return null;
		}
		return null;
	}

	public override XPathNodeIterator Clone()
	{
		return new ChildrenQuery(this);
	}
}
