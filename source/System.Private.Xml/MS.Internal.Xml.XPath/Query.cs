using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

[DebuggerDisplay("{ToString()}")]
internal abstract class Query : ResetableIterator
{
	public override int Count
	{
		get
		{
			if (count == -1)
			{
				Query query = (Query)Clone();
				query.Reset();
				count = 0;
				while (query.MoveNext())
				{
					count++;
				}
			}
			return count;
		}
	}

	public virtual double XsltDefaultPriority => 0.5;

	public abstract XPathResultType StaticType { get; }

	public virtual QueryProps Properties => QueryProps.Merge;

	public Query()
	{
	}

	protected Query(Query other)
		: base(other)
	{
	}

	public override bool MoveNext()
	{
		return Advance() != null;
	}

	public virtual void SetXsltContext(XsltContext context)
	{
	}

	public abstract object Evaluate(XPathNodeIterator nodeIterator);

	public abstract XPathNavigator Advance();

	public virtual XPathNavigator MatchNode(XPathNavigator current)
	{
		throw XPathException.Create(System.SR.Xp_InvalidPattern);
	}

	[return: NotNullIfNotNull("input")]
	public static Query Clone(Query input)
	{
		if (input != null)
		{
			return (Query)input.Clone();
		}
		return null;
	}

	[return: NotNullIfNotNull("input")]
	protected static XPathNodeIterator Clone(XPathNodeIterator input)
	{
		return input?.Clone();
	}

	[return: NotNullIfNotNull("input")]
	protected static XPathNavigator Clone(XPathNavigator input)
	{
		return input?.Clone();
	}

	public static bool Insert(List<XPathNavigator> buffer, XPathNavigator nav)
	{
		int num = 0;
		int num2 = buffer.Count;
		if (num2 != 0)
		{
			switch (CompareNodes(buffer[num2 - 1], nav))
			{
			case XmlNodeOrder.Same:
				return false;
			case XmlNodeOrder.Before:
				buffer.Add(nav.Clone());
				return true;
			}
			num2--;
		}
		while (num < num2)
		{
			int median = GetMedian(num, num2);
			switch (CompareNodes(buffer[median], nav))
			{
			case XmlNodeOrder.Same:
				return false;
			case XmlNodeOrder.Before:
				num = median + 1;
				break;
			default:
				num2 = median;
				break;
			}
		}
		buffer.Insert(num, nav.Clone());
		return true;
	}

	private static int GetMedian(int l, int r)
	{
		return l + r >>> 1;
	}

	public static XmlNodeOrder CompareNodes(XPathNavigator l, XPathNavigator r)
	{
		XmlNodeOrder xmlNodeOrder = l.ComparePosition(r);
		if (xmlNodeOrder == XmlNodeOrder.Unknown)
		{
			XPathNavigator xPathNavigator = l.Clone();
			xPathNavigator.MoveToRoot();
			string baseURI = xPathNavigator.BaseURI;
			if (!xPathNavigator.MoveTo(r))
			{
				xPathNavigator = r.Clone();
			}
			xPathNavigator.MoveToRoot();
			string baseURI2 = xPathNavigator.BaseURI;
			int num = string.CompareOrdinal(baseURI, baseURI2);
			xmlNodeOrder = ((num >= 0) ? ((num > 0) ? XmlNodeOrder.After : XmlNodeOrder.Unknown) : XmlNodeOrder.Before);
		}
		return xmlNodeOrder;
	}

	protected XPathResultType GetXPathType(object value)
	{
		if (value is XPathNodeIterator)
		{
			return XPathResultType.NodeSet;
		}
		if (value is string)
		{
			return XPathResultType.String;
		}
		if (value is double)
		{
			return XPathResultType.Number;
		}
		if (value is bool)
		{
			return XPathResultType.Boolean;
		}
		return (XPathResultType)4;
	}
}
