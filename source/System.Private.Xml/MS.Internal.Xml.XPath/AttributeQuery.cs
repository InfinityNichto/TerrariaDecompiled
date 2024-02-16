using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class AttributeQuery : BaseAxisQuery
{
	private bool _onAttribute;

	public AttributeQuery(Query qyParent, string Name, string Prefix, XPathNodeType Type)
		: base(qyParent, Name, Prefix, Type)
	{
	}

	private AttributeQuery(AttributeQuery other)
		: base(other)
	{
		_onAttribute = other._onAttribute;
	}

	public override void Reset()
	{
		_onAttribute = false;
		base.Reset();
	}

	public override XPathNavigator Advance()
	{
		do
		{
			if (!_onAttribute)
			{
				currentNode = qyInput.Advance();
				if (currentNode == null)
				{
					return null;
				}
				position = 0;
				currentNode = currentNode.Clone();
				_onAttribute = currentNode.MoveToFirstAttribute();
			}
			else
			{
				_onAttribute = currentNode.MoveToNextAttribute();
			}
		}
		while (!_onAttribute || !matches(currentNode));
		position++;
		return currentNode;
	}

	public override XPathNavigator MatchNode(XPathNavigator context)
	{
		if (context != null && context.NodeType == XPathNodeType.Attribute && matches(context))
		{
			XPathNavigator xPathNavigator = context.Clone();
			if (xPathNavigator.MoveToParent())
			{
				return qyInput.MatchNode(xPathNavigator);
			}
		}
		return null;
	}

	public override XPathNodeIterator Clone()
	{
		return new AttributeQuery(this);
	}
}
