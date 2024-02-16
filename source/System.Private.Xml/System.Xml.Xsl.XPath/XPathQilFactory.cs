using System.Xml.Schema;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.XPath;

internal class XPathQilFactory : QilPatternFactory
{
	public XPathQilFactory(QilFactory f, bool debug)
		: base(f, debug)
	{
	}

	public QilNode Error(string res, QilNode args)
	{
		return Error(InvokeFormatMessage(String(res), args));
	}

	public QilNode Error(ISourceLineInfo lineInfo, string res, params string[] args)
	{
		return Error(String(XslLoadException.CreateMessage(lineInfo, res, args)));
	}

	public QilIterator FirstNode(QilNode n)
	{
		QilIterator qilIterator = For(DocOrderDistinct(n));
		return For(Filter(qilIterator, Eq(PositionOf(qilIterator), Int32(1))));
	}

	public bool IsAnyType(QilNode n)
	{
		XmlQueryType xmlType = n.XmlType;
		return !xmlType.IsStrict && !xmlType.IsNode;
	}

	public bool CannotBeNodeSet(QilNode n)
	{
		XmlQueryType xmlType = n.XmlType;
		if (xmlType.IsAtomicValue && !xmlType.IsEmpty)
		{
			return !(n is QilIterator);
		}
		return false;
	}

	public QilNode SafeDocOrderDistinct(QilNode n)
	{
		XmlQueryType xmlType = n.XmlType;
		if (xmlType.MaybeMany)
		{
			if (xmlType.IsNode && xmlType.IsNotRtf)
			{
				return DocOrderDistinct(n);
			}
			QilIterator qilIterator;
			if (!xmlType.IsAtomicValue)
			{
				return Loop(qilIterator = Let(n), Conditional(Gt(Length(qilIterator), Int32(1)), DocOrderDistinct(TypeAssert(qilIterator, XmlQueryTypeFactory.NodeNotRtfS)), qilIterator));
			}
		}
		return n;
	}

	public QilNode InvokeFormatMessage(QilNode res, QilNode args)
	{
		return XsltInvokeEarlyBound(QName("format-message"), XsltMethods.FormatMessage, XmlQueryTypeFactory.StringX, new QilNode[2] { res, args });
	}

	public QilNode InvokeEqualityOperator(QilNodeType op, QilNode left, QilNode right)
	{
		left = TypeAssert(left, XmlQueryTypeFactory.ItemS);
		right = TypeAssert(right, XmlQueryTypeFactory.ItemS);
		double num = ((op != QilNodeType.Eq) ? 1.0 : 0.0);
		double val = num;
		return XsltInvokeEarlyBound(QName("EqualityOperator"), XsltMethods.EqualityOperator, XmlQueryTypeFactory.BooleanX, new QilNode[3]
		{
			Double(val),
			left,
			right
		});
	}

	public QilNode InvokeRelationalOperator(QilNodeType op, QilNode left, QilNode right)
	{
		left = TypeAssert(left, XmlQueryTypeFactory.ItemS);
		right = TypeAssert(right, XmlQueryTypeFactory.ItemS);
		double val = op switch
		{
			QilNodeType.Lt => 2.0, 
			QilNodeType.Le => 3.0, 
			QilNodeType.Gt => 4.0, 
			_ => 5.0, 
		};
		return XsltInvokeEarlyBound(QName("RelationalOperator"), XsltMethods.RelationalOperator, XmlQueryTypeFactory.BooleanX, new QilNode[3]
		{
			Double(val),
			left,
			right
		});
	}

	public QilNode ConvertToType(XmlTypeCode requiredType, QilNode n)
	{
		return requiredType switch
		{
			XmlTypeCode.String => ConvertToString(n), 
			XmlTypeCode.Double => ConvertToNumber(n), 
			XmlTypeCode.Boolean => ConvertToBoolean(n), 
			XmlTypeCode.Node => EnsureNodeSet(n), 
			XmlTypeCode.Item => n, 
			_ => null, 
		};
	}

	public QilNode ConvertToString(QilNode n)
	{
		switch (n.XmlType.TypeCode)
		{
		case XmlTypeCode.Boolean:
			if (n.NodeType != QilNodeType.True)
			{
				if (n.NodeType != QilNodeType.False)
				{
					return Conditional(n, String("true"), String("false"));
				}
				return String("false");
			}
			return String("true");
		case XmlTypeCode.Double:
			if (n.NodeType != QilNodeType.LiteralDouble)
			{
				return XsltConvert(n, XmlQueryTypeFactory.StringX);
			}
			return String(XPathConvert.DoubleToString((QilLiteral)n));
		case XmlTypeCode.String:
			return n;
		default:
			if (n.XmlType.IsNode)
			{
				return XPathNodeValue(SafeDocOrderDistinct(n));
			}
			return XsltConvert(n, XmlQueryTypeFactory.StringX);
		}
	}

	public QilNode ConvertToBoolean(QilNode n)
	{
		switch (n.XmlType.TypeCode)
		{
		case XmlTypeCode.Boolean:
			return n;
		case XmlTypeCode.Double:
		{
			QilIterator qilIterator;
			if (n.NodeType != QilNodeType.LiteralDouble)
			{
				return Loop(qilIterator = Let(n), Or(Lt(qilIterator, Double(0.0)), Lt(Double(0.0), qilIterator)));
			}
			return Boolean((double)(QilLiteral)n < 0.0 || 0.0 < (double)(QilLiteral)n);
		}
		case XmlTypeCode.String:
			if (n.NodeType != QilNodeType.LiteralString)
			{
				return Ne(StrLength(n), Int32(0));
			}
			return Boolean(((string)(QilLiteral)n).Length != 0);
		default:
			if (n.XmlType.IsNode)
			{
				return Not(IsEmpty(n));
			}
			return XsltConvert(n, XmlQueryTypeFactory.BooleanX);
		}
	}

	public QilNode ConvertToNumber(QilNode n)
	{
		switch (n.XmlType.TypeCode)
		{
		case XmlTypeCode.Boolean:
			if (n.NodeType != QilNodeType.True)
			{
				if (n.NodeType != QilNodeType.False)
				{
					return Conditional(n, Double(1.0), Double(0.0));
				}
				return Double(0.0);
			}
			return Double(1.0);
		case XmlTypeCode.Double:
			return n;
		case XmlTypeCode.String:
			return XsltConvert(n, XmlQueryTypeFactory.DoubleX);
		default:
			if (n.XmlType.IsNode)
			{
				return XsltConvert(XPathNodeValue(SafeDocOrderDistinct(n)), XmlQueryTypeFactory.DoubleX);
			}
			return XsltConvert(n, XmlQueryTypeFactory.DoubleX);
		}
	}

	public QilNode ConvertToNode(QilNode n)
	{
		if (n.XmlType.IsNode && n.XmlType.IsNotRtf && n.XmlType.IsSingleton)
		{
			return n;
		}
		return XsltConvert(n, XmlQueryTypeFactory.NodeNotRtf);
	}

	public QilNode ConvertToNodeSet(QilNode n)
	{
		if (n.XmlType.IsNode && n.XmlType.IsNotRtf)
		{
			return n;
		}
		return XsltConvert(n, XmlQueryTypeFactory.NodeNotRtfS);
	}

	public QilNode TryEnsureNodeSet(QilNode n)
	{
		if (n.XmlType.IsNode && n.XmlType.IsNotRtf)
		{
			return n;
		}
		if (CannotBeNodeSet(n))
		{
			return null;
		}
		return InvokeEnsureNodeSet(n);
	}

	public QilNode EnsureNodeSet(QilNode n)
	{
		QilNode qilNode = TryEnsureNodeSet(n);
		if (qilNode == null)
		{
			throw new XPathCompileException(System.SR.XPath_NodeSetExpected);
		}
		return qilNode;
	}

	public QilNode InvokeEnsureNodeSet(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("ensure-node-set"), XsltMethods.EnsureNodeSet, XmlQueryTypeFactory.NodeSDod, new QilNode[1] { n });
	}

	public QilNode Id(QilNode context, QilNode id)
	{
		if (id.XmlType.IsSingleton)
		{
			return Deref(context, ConvertToString(id));
		}
		QilIterator n;
		return Loop(n = For(id), Deref(context, ConvertToString(n)));
	}

	public QilNode InvokeStartsWith(QilNode str1, QilNode str2)
	{
		return XsltInvokeEarlyBound(QName("starts-with"), XsltMethods.StartsWith, XmlQueryTypeFactory.BooleanX, new QilNode[2] { str1, str2 });
	}

	public QilNode InvokeContains(QilNode str1, QilNode str2)
	{
		return XsltInvokeEarlyBound(QName("contains"), XsltMethods.Contains, XmlQueryTypeFactory.BooleanX, new QilNode[2] { str1, str2 });
	}

	public QilNode InvokeSubstringBefore(QilNode str1, QilNode str2)
	{
		return XsltInvokeEarlyBound(QName("substring-before"), XsltMethods.SubstringBefore, XmlQueryTypeFactory.StringX, new QilNode[2] { str1, str2 });
	}

	public QilNode InvokeSubstringAfter(QilNode str1, QilNode str2)
	{
		return XsltInvokeEarlyBound(QName("substring-after"), XsltMethods.SubstringAfter, XmlQueryTypeFactory.StringX, new QilNode[2] { str1, str2 });
	}

	public QilNode InvokeSubstring(QilNode str, QilNode start)
	{
		return XsltInvokeEarlyBound(QName("substring"), XsltMethods.Substring2, XmlQueryTypeFactory.StringX, new QilNode[2] { str, start });
	}

	public QilNode InvokeSubstring(QilNode str, QilNode start, QilNode length)
	{
		return XsltInvokeEarlyBound(QName("substring"), XsltMethods.Substring3, XmlQueryTypeFactory.StringX, new QilNode[3] { str, start, length });
	}

	public QilNode InvokeNormalizeSpace(QilNode str)
	{
		return XsltInvokeEarlyBound(QName("normalize-space"), XsltMethods.NormalizeSpace, XmlQueryTypeFactory.StringX, new QilNode[1] { str });
	}

	public QilNode InvokeTranslate(QilNode str1, QilNode str2, QilNode str3)
	{
		return XsltInvokeEarlyBound(QName("translate"), XsltMethods.Translate, XmlQueryTypeFactory.StringX, new QilNode[3] { str1, str2, str3 });
	}

	public QilNode InvokeLang(QilNode lang, QilNode context)
	{
		return XsltInvokeEarlyBound(QName("lang"), XsltMethods.Lang, XmlQueryTypeFactory.BooleanX, new QilNode[2] { lang, context });
	}

	public QilNode InvokeFloor(QilNode value)
	{
		return XsltInvokeEarlyBound(QName("floor"), XsltMethods.Floor, XmlQueryTypeFactory.DoubleX, new QilNode[1] { value });
	}

	public QilNode InvokeCeiling(QilNode value)
	{
		return XsltInvokeEarlyBound(QName("ceiling"), XsltMethods.Ceiling, XmlQueryTypeFactory.DoubleX, new QilNode[1] { value });
	}

	public QilNode InvokeRound(QilNode value)
	{
		return XsltInvokeEarlyBound(QName("round"), XsltMethods.Round, XmlQueryTypeFactory.DoubleX, new QilNode[1] { value });
	}
}
