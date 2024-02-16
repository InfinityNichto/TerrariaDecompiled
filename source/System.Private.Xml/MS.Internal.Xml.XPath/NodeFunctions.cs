using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class NodeFunctions : ValueQuery
{
	private readonly Query _arg;

	private readonly Function.FunctionType _funcType;

	private XsltContext _xsltContext;

	public override XPathResultType StaticType => Function.ReturnTypes[(int)_funcType];

	public NodeFunctions(Function.FunctionType funcType, Query arg)
	{
		_funcType = funcType;
		_arg = arg;
	}

	public override void SetXsltContext(XsltContext context)
	{
		_xsltContext = (context.Whitespace ? context : null);
		if (_arg != null)
		{
			_arg.SetXsltContext(context);
		}
	}

	private XPathNavigator EvaluateArg(XPathNodeIterator context)
	{
		if (_arg == null)
		{
			return context.Current;
		}
		_arg.Evaluate(context);
		return _arg.Advance();
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		switch (_funcType)
		{
		case Function.FunctionType.FuncPosition:
			return (double)context.CurrentPosition;
		case Function.FunctionType.FuncLast:
			return (double)context.Count;
		case Function.FunctionType.FuncNameSpaceUri:
		{
			XPathNavigator xPathNavigator2 = EvaluateArg(context);
			if (xPathNavigator2 != null)
			{
				return xPathNavigator2.NamespaceURI;
			}
			break;
		}
		case Function.FunctionType.FuncLocalName:
		{
			XPathNavigator xPathNavigator2 = EvaluateArg(context);
			if (xPathNavigator2 != null)
			{
				return xPathNavigator2.LocalName;
			}
			break;
		}
		case Function.FunctionType.FuncName:
		{
			XPathNavigator xPathNavigator2 = EvaluateArg(context);
			if (xPathNavigator2 != null)
			{
				return xPathNavigator2.Name;
			}
			break;
		}
		case Function.FunctionType.FuncCount:
		{
			_arg.Evaluate(context);
			int num = 0;
			if (_xsltContext != null)
			{
				XPathNavigator xPathNavigator;
				while ((xPathNavigator = _arg.Advance()) != null)
				{
					if (xPathNavigator.NodeType != XPathNodeType.Whitespace || _xsltContext.PreserveWhitespace(xPathNavigator))
					{
						num++;
					}
				}
			}
			else
			{
				while (_arg.Advance() != null)
				{
					num++;
				}
			}
			return (double)num;
		}
		}
		return string.Empty;
	}

	public override XPathNodeIterator Clone()
	{
		NodeFunctions nodeFunctions = new NodeFunctions(_funcType, Query.Clone(_arg));
		nodeFunctions._xsltContext = _xsltContext;
		return nodeFunctions;
	}
}
