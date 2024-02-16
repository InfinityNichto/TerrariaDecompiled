using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class FollowingQuery : BaseAxisQuery
{
	private XPathNavigator _input;

	private XPathNodeIterator _iterator;

	public FollowingQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest)
		: base(qyInput, name, prefix, typeTest)
	{
	}

	private FollowingQuery(FollowingQuery other)
		: base(other)
	{
		_input = Query.Clone(other._input);
		_iterator = Query.Clone(other._iterator);
	}

	public override void Reset()
	{
		_iterator = null;
		base.Reset();
	}

	public override XPathNavigator Advance()
	{
		if (_iterator == null)
		{
			_input = qyInput.Advance();
			if (_input == null)
			{
				return null;
			}
			XPathNavigator xPathNavigator;
			do
			{
				xPathNavigator = _input.Clone();
				_input = qyInput.Advance();
			}
			while (xPathNavigator.IsDescendant(_input));
			_input = xPathNavigator;
			_iterator = XPathEmptyIterator.Instance;
		}
		while (!_iterator.MoveNext())
		{
			bool matchSelf;
			if (_input.NodeType == XPathNodeType.Attribute || _input.NodeType == XPathNodeType.Namespace)
			{
				_input.MoveToParent();
				matchSelf = false;
			}
			else
			{
				while (!_input.MoveToNext())
				{
					if (!_input.MoveToParent())
					{
						return null;
					}
				}
				matchSelf = true;
			}
			if (base.NameTest)
			{
				_iterator = _input.SelectDescendants(base.Name, base.Namespace, matchSelf);
			}
			else
			{
				_iterator = _input.SelectDescendants(base.TypeTest, matchSelf);
			}
		}
		position++;
		currentNode = _iterator.Current;
		return currentNode;
	}

	public override XPathNodeIterator Clone()
	{
		return new FollowingQuery(this);
	}
}
