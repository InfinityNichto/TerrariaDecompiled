using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class GroupQuery : BaseAxisQuery
{
	public override XPathResultType StaticType => qyInput.StaticType;

	public override QueryProps Properties => QueryProps.Position;

	public GroupQuery(Query qy)
		: base(qy)
	{
	}

	private GroupQuery(GroupQuery other)
		: base(other)
	{
	}

	public override XPathNavigator Advance()
	{
		currentNode = qyInput.Advance();
		if (currentNode != null)
		{
			position++;
		}
		return currentNode;
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		return qyInput.Evaluate(nodeIterator);
	}

	public override XPathNodeIterator Clone()
	{
		return new GroupQuery(this);
	}
}
