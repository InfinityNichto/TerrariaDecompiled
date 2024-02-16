using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class PrecedingQuery : BaseAxisQuery
{
	private XPathNodeIterator _workIterator;

	private readonly ClonableStack<XPathNavigator> _ancestorStk;

	public override QueryProps Properties => base.Properties | QueryProps.Reverse;

	public PrecedingQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest)
		: base(qyInput, name, prefix, typeTest)
	{
		_ancestorStk = new ClonableStack<XPathNavigator>();
	}

	private PrecedingQuery(PrecedingQuery other)
		: base(other)
	{
		_workIterator = Query.Clone(other._workIterator);
		_ancestorStk = other._ancestorStk.Clone();
	}

	public override void Reset()
	{
		_workIterator = null;
		_ancestorStk.Clear();
		base.Reset();
	}

	public override XPathNavigator Advance()
	{
		if (_workIterator == null)
		{
			XPathNavigator xPathNavigator = qyInput.Advance();
			if (xPathNavigator == null)
			{
				return null;
			}
			XPathNavigator xPathNavigator2 = xPathNavigator.Clone();
			do
			{
				xPathNavigator2.MoveTo(xPathNavigator);
			}
			while ((xPathNavigator = qyInput.Advance()) != null);
			if (xPathNavigator2.NodeType == XPathNodeType.Attribute || xPathNavigator2.NodeType == XPathNodeType.Namespace)
			{
				xPathNavigator2.MoveToParent();
			}
			do
			{
				_ancestorStk.Push(xPathNavigator2.Clone());
			}
			while (xPathNavigator2.MoveToParent());
			_workIterator = xPathNavigator2.SelectDescendants(XPathNodeType.All, matchSelf: true);
		}
		while (_workIterator.MoveNext())
		{
			currentNode = _workIterator.Current;
			if (currentNode.IsSamePosition(_ancestorStk.Peek()))
			{
				_ancestorStk.Pop();
				if (_ancestorStk.Count == 0)
				{
					currentNode = null;
					_workIterator = null;
					return null;
				}
			}
			else if (matches(currentNode))
			{
				position++;
				return currentNode;
			}
		}
		return null;
	}

	public override XPathNodeIterator Clone()
	{
		return new PrecedingQuery(this);
	}
}
