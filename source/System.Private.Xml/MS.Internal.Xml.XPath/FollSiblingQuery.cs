using System.Collections.Generic;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class FollSiblingQuery : BaseAxisQuery
{
	private readonly ClonableStack<XPathNavigator> _elementStk;

	private readonly List<XPathNavigator> _parentStk;

	private XPathNavigator _nextInput;

	public FollSiblingQuery(Query qyInput, string name, string prefix, XPathNodeType type)
		: base(qyInput, name, prefix, type)
	{
		_elementStk = new ClonableStack<XPathNavigator>();
		_parentStk = new List<XPathNavigator>();
	}

	private FollSiblingQuery(FollSiblingQuery other)
		: base(other)
	{
		_elementStk = other._elementStk.Clone();
		_parentStk = new List<XPathNavigator>(other._parentStk);
		_nextInput = Query.Clone(other._nextInput);
	}

	public override void Reset()
	{
		_elementStk.Clear();
		_parentStk.Clear();
		_nextInput = null;
		base.Reset();
	}

	private bool Visited(XPathNavigator nav)
	{
		XPathNavigator xPathNavigator = nav.Clone();
		xPathNavigator.MoveToParent();
		for (int i = 0; i < _parentStk.Count; i++)
		{
			if (xPathNavigator.IsSamePosition(_parentStk[i]))
			{
				return true;
			}
		}
		_parentStk.Add(xPathNavigator);
		return false;
	}

	private XPathNavigator FetchInput()
	{
		XPathNavigator xPathNavigator;
		do
		{
			xPathNavigator = qyInput.Advance();
			if (xPathNavigator == null)
			{
				return null;
			}
		}
		while (Visited(xPathNavigator));
		return xPathNavigator.Clone();
	}

	public override XPathNavigator Advance()
	{
		while (true)
		{
			if (currentNode == null)
			{
				if (_nextInput == null)
				{
					_nextInput = FetchInput();
				}
				if (_elementStk.Count == 0)
				{
					if (_nextInput == null)
					{
						break;
					}
					currentNode = _nextInput;
					_nextInput = FetchInput();
				}
				else
				{
					currentNode = _elementStk.Pop();
				}
			}
			while (currentNode.IsDescendant(_nextInput))
			{
				_elementStk.Push(currentNode);
				currentNode = _nextInput;
				_nextInput = qyInput.Advance();
				if (_nextInput != null)
				{
					_nextInput = _nextInput.Clone();
				}
			}
			while (currentNode.MoveToNext())
			{
				if (matches(currentNode))
				{
					position++;
					return currentNode;
				}
			}
			currentNode = null;
		}
		return null;
	}

	public override XPathNodeIterator Clone()
	{
		return new FollSiblingQuery(this);
	}
}
