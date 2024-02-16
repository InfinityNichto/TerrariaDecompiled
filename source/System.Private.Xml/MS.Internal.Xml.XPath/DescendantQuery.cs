using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class DescendantQuery : DescendantBaseQuery
{
	private XPathNodeIterator _nodeIterator;

	internal DescendantQuery(Query qyParent, string Name, string Prefix, XPathNodeType Type, bool matchSelf, bool abbrAxis)
		: base(qyParent, Name, Prefix, Type, matchSelf, abbrAxis)
	{
	}

	public DescendantQuery(DescendantQuery other)
		: base(other)
	{
		_nodeIterator = Query.Clone(other._nodeIterator);
	}

	public override void Reset()
	{
		_nodeIterator = null;
		base.Reset();
	}

	public override XPathNavigator Advance()
	{
		while (true)
		{
			if (_nodeIterator == null)
			{
				position = 0;
				XPathNavigator xPathNavigator = qyInput.Advance();
				if (xPathNavigator == null)
				{
					return null;
				}
				if (base.NameTest)
				{
					if (base.TypeTest == XPathNodeType.ProcessingInstruction)
					{
						_nodeIterator = new IteratorFilter(xPathNavigator.SelectDescendants(base.TypeTest, matchSelf), base.Name);
					}
					else
					{
						_nodeIterator = xPathNavigator.SelectDescendants(base.Name, base.Namespace, matchSelf);
					}
				}
				else
				{
					_nodeIterator = xPathNavigator.SelectDescendants(base.TypeTest, matchSelf);
				}
			}
			if (_nodeIterator.MoveNext())
			{
				break;
			}
			_nodeIterator = null;
		}
		position++;
		currentNode = _nodeIterator.Current;
		return currentNode;
	}

	public override XPathNodeIterator Clone()
	{
		return new DescendantQuery(this);
	}
}
