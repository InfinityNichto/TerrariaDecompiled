using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class UnionExpr : Query
{
	internal Query qy1;

	internal Query qy2;

	private bool _advance1;

	private bool _advance2;

	private XPathNavigator _currentNode;

	private XPathNavigator _nextNode;

	public override XPathResultType StaticType => XPathResultType.NodeSet;

	public override XPathNavigator Current => _currentNode;

	public override int CurrentPosition
	{
		get
		{
			throw new InvalidOperationException();
		}
	}

	public UnionExpr(Query query1, Query query2)
	{
		qy1 = query1;
		qy2 = query2;
		_advance1 = true;
		_advance2 = true;
	}

	private UnionExpr(UnionExpr other)
		: base(other)
	{
		qy1 = Query.Clone(other.qy1);
		qy2 = Query.Clone(other.qy2);
		_advance1 = other._advance1;
		_advance2 = other._advance2;
		_currentNode = Query.Clone(other._currentNode);
		_nextNode = Query.Clone(other._nextNode);
	}

	public override void Reset()
	{
		qy1.Reset();
		qy2.Reset();
		_advance1 = true;
		_advance2 = true;
		_nextNode = null;
	}

	public override void SetXsltContext(XsltContext xsltContext)
	{
		qy1.SetXsltContext(xsltContext);
		qy2.SetXsltContext(xsltContext);
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		qy1.Evaluate(context);
		qy2.Evaluate(context);
		_advance1 = true;
		_advance2 = true;
		_nextNode = null;
		ResetCount();
		return this;
	}

	private XPathNavigator ProcessSamePosition(XPathNavigator result)
	{
		_currentNode = result;
		_advance1 = (_advance2 = true);
		return result;
	}

	private XPathNavigator ProcessBeforePosition(XPathNavigator res1, XPathNavigator res2)
	{
		_nextNode = res2;
		_advance2 = false;
		_advance1 = true;
		_currentNode = res1;
		return res1;
	}

	private XPathNavigator ProcessAfterPosition(XPathNavigator res1, XPathNavigator res2)
	{
		_nextNode = res1;
		_advance1 = false;
		_advance2 = true;
		_currentNode = res2;
		return res2;
	}

	public override XPathNavigator Advance()
	{
		XmlNodeOrder xmlNodeOrder = XmlNodeOrder.Before;
		XPathNavigator xPathNavigator = ((!_advance1) ? _nextNode : qy1.Advance());
		XPathNavigator xPathNavigator2 = ((!_advance2) ? _nextNode : qy2.Advance());
		if (xPathNavigator == null || xPathNavigator2 == null)
		{
			if (xPathNavigator2 == null)
			{
				_advance1 = true;
				_advance2 = false;
				_currentNode = xPathNavigator;
				_nextNode = null;
				return xPathNavigator;
			}
			_advance1 = false;
			_advance2 = true;
			_currentNode = xPathNavigator2;
			_nextNode = null;
			return xPathNavigator2;
		}
		return Query.CompareNodes(xPathNavigator, xPathNavigator2) switch
		{
			XmlNodeOrder.Before => ProcessBeforePosition(xPathNavigator, xPathNavigator2), 
			XmlNodeOrder.After => ProcessAfterPosition(xPathNavigator, xPathNavigator2), 
			_ => ProcessSamePosition(xPathNavigator), 
		};
	}

	public override XPathNavigator MatchNode(XPathNavigator xsltContext)
	{
		if (xsltContext != null)
		{
			XPathNavigator xPathNavigator = qy1.MatchNode(xsltContext);
			if (xPathNavigator != null)
			{
				return xPathNavigator;
			}
			return qy2.MatchNode(xsltContext);
		}
		return null;
	}

	public override XPathNodeIterator Clone()
	{
		return new UnionExpr(this);
	}
}
