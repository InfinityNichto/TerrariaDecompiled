using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class FilterQuery : BaseAxisQuery
{
	private readonly Query _cond;

	private readonly bool _noPosition;

	public Query Condition => _cond;

	public override QueryProps Properties => QueryProps.Position | (qyInput.Properties & (QueryProps)24);

	public FilterQuery(Query qyParent, Query cond, bool noPosition)
		: base(qyParent)
	{
		_cond = cond;
		_noPosition = noPosition;
	}

	private FilterQuery(FilterQuery other)
		: base(other)
	{
		_cond = Query.Clone(other._cond);
		_noPosition = other._noPosition;
	}

	public override void Reset()
	{
		_cond.Reset();
		base.Reset();
	}

	public override void SetXsltContext(XsltContext input)
	{
		base.SetXsltContext(input);
		_cond.SetXsltContext(input);
		if (_cond.StaticType != 0 && _cond.StaticType != XPathResultType.Any && _noPosition && qyInput is ReversePositionQuery reversePositionQuery)
		{
			qyInput = reversePositionQuery.input;
		}
	}

	public override XPathNavigator Advance()
	{
		while ((currentNode = qyInput.Advance()) != null)
		{
			if (EvaluatePredicate())
			{
				position++;
				return currentNode;
			}
		}
		return null;
	}

	internal bool EvaluatePredicate()
	{
		object obj = _cond.Evaluate(qyInput);
		if (obj is XPathNodeIterator)
		{
			return _cond.Advance() != null;
		}
		if (obj is string)
		{
			return ((string)obj).Length != 0;
		}
		if (obj is double)
		{
			return (double)obj == (double)qyInput.CurrentPosition;
		}
		if (obj is bool)
		{
			return (bool)obj;
		}
		return true;
	}

	public override XPathNavigator MatchNode(XPathNavigator current)
	{
		if (current == null)
		{
			return null;
		}
		XPathNavigator xPathNavigator = qyInput.MatchNode(current);
		if (xPathNavigator != null)
		{
			switch (_cond.StaticType)
			{
			case XPathResultType.Number:
			{
				if (!(_cond is OperandQuery operandQuery))
				{
					break;
				}
				double num = (double)operandQuery.val;
				if (qyInput is ChildrenQuery childrenQuery)
				{
					XPathNavigator xPathNavigator2 = current.Clone();
					xPathNavigator2.MoveToParent();
					int num2 = 0;
					xPathNavigator2.MoveToFirstChild();
					do
					{
						if (!childrenQuery.matches(xPathNavigator2))
						{
							continue;
						}
						num2++;
						if (current.IsSamePosition(xPathNavigator2))
						{
							if (num != (double)num2)
							{
								return null;
							}
							return xPathNavigator;
						}
					}
					while (xPathNavigator2.MoveToNext());
					return null;
				}
				if (!(qyInput is AttributeQuery attributeQuery))
				{
					break;
				}
				XPathNavigator xPathNavigator3 = current.Clone();
				xPathNavigator3.MoveToParent();
				int num3 = 0;
				xPathNavigator3.MoveToFirstAttribute();
				do
				{
					if (!attributeQuery.matches(xPathNavigator3))
					{
						continue;
					}
					num3++;
					if (current.IsSamePosition(xPathNavigator3))
					{
						if (num != (double)num3)
						{
							return null;
						}
						return xPathNavigator;
					}
				}
				while (xPathNavigator3.MoveToNextAttribute());
				return null;
			}
			case XPathResultType.NodeSet:
				_cond.Evaluate(new XPathSingletonIterator(current, moved: true));
				if (_cond.Advance() == null)
				{
					return null;
				}
				return xPathNavigator;
			case XPathResultType.Boolean:
				if (_noPosition)
				{
					if (!(bool)_cond.Evaluate(new XPathSingletonIterator(current, moved: true)))
					{
						return null;
					}
					return xPathNavigator;
				}
				break;
			case XPathResultType.String:
				if (_noPosition)
				{
					if (((string)_cond.Evaluate(new XPathSingletonIterator(current, moved: true))).Length == 0)
					{
						return null;
					}
					return xPathNavigator;
				}
				break;
			case (XPathResultType)4:
				return xPathNavigator;
			default:
				return null;
			}
			Evaluate(new XPathSingletonIterator(xPathNavigator, moved: true));
			XPathNavigator xPathNavigator4;
			while ((xPathNavigator4 = Advance()) != null)
			{
				if (xPathNavigator4.IsSamePosition(current))
				{
					return xPathNavigator;
				}
			}
		}
		return null;
	}

	public override XPathNodeIterator Clone()
	{
		return new FilterQuery(this);
	}
}
