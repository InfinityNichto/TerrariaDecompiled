using System.Collections.Generic;
using System.Xml.XPath;

namespace System.Xml.Xsl.XPath;

internal sealed class XPathParser<Node>
{
	private XPathScanner _scanner;

	private IXPathBuilder<Node> _builder;

	private readonly Stack<int> _posInfo = new Stack<int>();

	private int _parseRelativePath;

	private int _parseSubExprDepth;

	private static readonly int[] s_XPathOperatorPrecedence = new int[16]
	{
		0, 1, 2, 3, 3, 4, 4, 4, 4, 5,
		5, 6, 6, 6, 7, 8
	};

	public Node Parse(XPathScanner scanner, IXPathBuilder<Node> builder, LexKind endLex)
	{
		Node result = default(Node);
		_scanner = scanner;
		_builder = builder;
		_posInfo.Clear();
		try
		{
			builder.StartBuild();
			result = ParseExpr();
			scanner.CheckToken(endLex);
		}
		catch (XPathCompileException ex)
		{
			if (ex.queryString == null)
			{
				ex.queryString = scanner.Source;
				PopPosInfo(out ex.startChar, out ex.endChar);
			}
			throw;
		}
		finally
		{
			result = builder.EndBuild(result);
		}
		return result;
	}

	internal static bool IsStep(LexKind lexKind)
	{
		if (lexKind != LexKind.Dot && lexKind != LexKind.DotDot && lexKind != LexKind.At && lexKind != LexKind.Axis && lexKind != LexKind.Star)
		{
			return lexKind == LexKind.Name;
		}
		return true;
	}

	private Node ParseLocationPath()
	{
		if (_scanner.Kind == LexKind.Slash)
		{
			_scanner.NextLex();
			Node val = _builder.Axis(XPathAxis.Root, XPathNodeType.All, null, null);
			if (IsStep(_scanner.Kind))
			{
				val = _builder.JoinStep(val, ParseRelativeLocationPath());
			}
			return val;
		}
		if (_scanner.Kind == LexKind.SlashSlash)
		{
			_scanner.NextLex();
			return _builder.JoinStep(_builder.Axis(XPathAxis.Root, XPathNodeType.All, null, null), _builder.JoinStep(_builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null), ParseRelativeLocationPath()));
		}
		return ParseRelativeLocationPath();
	}

	private Node ParseRelativeLocationPath()
	{
		if (++_parseRelativePath > 1024 && System.LocalAppContextSwitches.LimitXPathComplexity)
		{
			throw _scanner.CreateException(System.SR.Xslt_InputTooComplex);
		}
		Node val = ParseStep();
		if (_scanner.Kind == LexKind.Slash)
		{
			_scanner.NextLex();
			val = _builder.JoinStep(val, ParseRelativeLocationPath());
		}
		else if (_scanner.Kind == LexKind.SlashSlash)
		{
			_scanner.NextLex();
			val = _builder.JoinStep(val, _builder.JoinStep(_builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null), ParseRelativeLocationPath()));
		}
		_parseRelativePath--;
		return val;
	}

	private Node ParseStep()
	{
		Node val;
		if (LexKind.Dot == _scanner.Kind)
		{
			_scanner.NextLex();
			val = _builder.Axis(XPathAxis.Self, XPathNodeType.All, null, null);
			if (LexKind.LBracket == _scanner.Kind)
			{
				throw _scanner.CreateException(System.SR.XPath_PredicateAfterDot);
			}
		}
		else if (LexKind.DotDot == _scanner.Kind)
		{
			_scanner.NextLex();
			val = _builder.Axis(XPathAxis.Parent, XPathNodeType.All, null, null);
			if (LexKind.LBracket == _scanner.Kind)
			{
				throw _scanner.CreateException(System.SR.XPath_PredicateAfterDotDot);
			}
		}
		else
		{
			XPathAxis axis;
			switch (_scanner.Kind)
			{
			case LexKind.Axis:
				axis = _scanner.Axis;
				_scanner.NextLex();
				_scanner.NextLex();
				break;
			case LexKind.At:
				axis = XPathAxis.Attribute;
				_scanner.NextLex();
				break;
			case LexKind.Name:
			case LexKind.Star:
				axis = XPathAxis.Child;
				break;
			default:
				throw _scanner.CreateException(System.SR.XPath_UnexpectedToken, _scanner.RawValue);
			}
			val = ParseNodeTest(axis);
			while (LexKind.LBracket == _scanner.Kind)
			{
				val = _builder.Predicate(val, ParsePredicate(), IsReverseAxis(axis));
			}
		}
		return val;
	}

	private static bool IsReverseAxis(XPathAxis axis)
	{
		if (axis != XPathAxis.Ancestor && axis != XPathAxis.Preceding && axis != XPathAxis.AncestorOrSelf)
		{
			return axis == XPathAxis.PrecedingSibling;
		}
		return true;
	}

	private Node ParseNodeTest(XPathAxis axis)
	{
		int lexStart = _scanner.LexStart;
		InternalParseNodeTest(_scanner, axis, out var nodeType, out var nodePrefix, out var nodeName);
		PushPosInfo(lexStart, _scanner.PrevLexEnd);
		Node result = _builder.Axis(axis, nodeType, nodePrefix, nodeName);
		PopPosInfo();
		return result;
	}

	private static bool IsNodeType(XPathScanner scanner)
	{
		if (scanner.Prefix.Length == 0)
		{
			if (!(scanner.Name == "node") && !(scanner.Name == "text") && !(scanner.Name == "processing-instruction"))
			{
				return scanner.Name == "comment";
			}
			return true;
		}
		return false;
	}

	private static XPathNodeType PrincipalNodeType(XPathAxis axis)
	{
		return axis switch
		{
			XPathAxis.Namespace => XPathNodeType.Namespace, 
			XPathAxis.Attribute => XPathNodeType.Attribute, 
			_ => XPathNodeType.Element, 
		};
	}

	internal static void InternalParseNodeTest(XPathScanner scanner, XPathAxis axis, out XPathNodeType nodeType, out string nodePrefix, out string nodeName)
	{
		switch (scanner.Kind)
		{
		case LexKind.Name:
			if (scanner.CanBeFunction && IsNodeType(scanner))
			{
				nodePrefix = null;
				nodeName = null;
				switch (scanner.Name)
				{
				case "comment":
					nodeType = XPathNodeType.Comment;
					break;
				case "text":
					nodeType = XPathNodeType.Text;
					break;
				case "node":
					nodeType = XPathNodeType.All;
					break;
				default:
					nodeType = XPathNodeType.ProcessingInstruction;
					break;
				}
				scanner.NextLex();
				scanner.PassToken(LexKind.LParens);
				if (nodeType == XPathNodeType.ProcessingInstruction && scanner.Kind != LexKind.RParens)
				{
					scanner.CheckToken(LexKind.String);
					nodePrefix = string.Empty;
					nodeName = scanner.StringValue;
					scanner.NextLex();
				}
				scanner.PassToken(LexKind.RParens);
			}
			else
			{
				nodePrefix = scanner.Prefix;
				nodeName = scanner.Name;
				nodeType = PrincipalNodeType(axis);
				scanner.NextLex();
				if (nodeName == "*")
				{
					nodeName = null;
				}
			}
			break;
		case LexKind.Star:
			nodePrefix = null;
			nodeName = null;
			nodeType = PrincipalNodeType(axis);
			scanner.NextLex();
			break;
		default:
			throw scanner.CreateException(System.SR.XPath_NodeTestExpected, scanner.RawValue);
		}
	}

	private Node ParsePredicate()
	{
		_scanner.PassToken(LexKind.LBracket);
		Node result = ParseExpr();
		_scanner.PassToken(LexKind.RBracket);
		return result;
	}

	private Node ParseExpr()
	{
		return ParseSubExpr(0);
	}

	private Node ParseSubExpr(int callerPrec)
	{
		if (++_parseSubExprDepth > 1024 && System.LocalAppContextSwitches.LimitXPathComplexity)
		{
			throw _scanner.CreateException(System.SR.Xslt_InputTooComplex);
		}
		Node val;
		if (_scanner.Kind == LexKind.Minus)
		{
			XPathOperator xPathOperator = XPathOperator.UnaryMinus;
			int callerPrec2 = s_XPathOperatorPrecedence[(int)xPathOperator];
			_scanner.NextLex();
			val = _builder.Operator(xPathOperator, ParseSubExpr(callerPrec2), default(Node));
		}
		else
		{
			val = ParseUnionExpr();
		}
		while (true)
		{
			XPathOperator xPathOperator = (XPathOperator)((_scanner.Kind <= LexKind.Union) ? _scanner.Kind : LexKind.Unknown);
			int num = s_XPathOperatorPrecedence[(int)xPathOperator];
			if (num <= callerPrec)
			{
				break;
			}
			_scanner.NextLex();
			val = _builder.Operator(xPathOperator, val, ParseSubExpr(num));
		}
		_parseSubExprDepth--;
		return val;
	}

	private Node ParseUnionExpr()
	{
		int lexStart = _scanner.LexStart;
		Node val = ParsePathExpr();
		if (_scanner.Kind == LexKind.Union)
		{
			PushPosInfo(lexStart, _scanner.PrevLexEnd);
			val = _builder.Operator(XPathOperator.Union, default(Node), val);
			PopPosInfo();
			while (_scanner.Kind == LexKind.Union)
			{
				_scanner.NextLex();
				lexStart = _scanner.LexStart;
				Node right = ParsePathExpr();
				PushPosInfo(lexStart, _scanner.PrevLexEnd);
				val = _builder.Operator(XPathOperator.Union, val, right);
				PopPosInfo();
			}
		}
		return val;
	}

	private Node ParsePathExpr()
	{
		if (IsPrimaryExpr())
		{
			int lexStart = _scanner.LexStart;
			Node val = ParseFilterExpr();
			int prevLexEnd = _scanner.PrevLexEnd;
			if (_scanner.Kind == LexKind.Slash)
			{
				_scanner.NextLex();
				PushPosInfo(lexStart, prevLexEnd);
				val = _builder.JoinStep(val, ParseRelativeLocationPath());
				PopPosInfo();
			}
			else if (_scanner.Kind == LexKind.SlashSlash)
			{
				_scanner.NextLex();
				PushPosInfo(lexStart, prevLexEnd);
				val = _builder.JoinStep(val, _builder.JoinStep(_builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null), ParseRelativeLocationPath()));
				PopPosInfo();
			}
			return val;
		}
		return ParseLocationPath();
	}

	private Node ParseFilterExpr()
	{
		int lexStart = _scanner.LexStart;
		Node val = ParsePrimaryExpr();
		int prevLexEnd = _scanner.PrevLexEnd;
		while (_scanner.Kind == LexKind.LBracket)
		{
			PushPosInfo(lexStart, prevLexEnd);
			val = _builder.Predicate(val, ParsePredicate(), reverseStep: false);
			PopPosInfo();
		}
		return val;
	}

	private bool IsPrimaryExpr()
	{
		if (_scanner.Kind != LexKind.String && _scanner.Kind != LexKind.Number && _scanner.Kind != LexKind.Dollar && _scanner.Kind != LexKind.LParens)
		{
			if (_scanner.Kind == LexKind.Name && _scanner.CanBeFunction)
			{
				return !IsNodeType(_scanner);
			}
			return false;
		}
		return true;
	}

	private Node ParsePrimaryExpr()
	{
		Node result;
		switch (_scanner.Kind)
		{
		case LexKind.String:
			result = _builder.String(_scanner.StringValue);
			_scanner.NextLex();
			break;
		case LexKind.Number:
			result = _builder.Number(XPathConvert.StringToDouble(_scanner.RawValue));
			_scanner.NextLex();
			break;
		case LexKind.Dollar:
		{
			int lexStart = _scanner.LexStart;
			_scanner.NextLex();
			_scanner.CheckToken(LexKind.Name);
			PushPosInfo(lexStart, _scanner.LexStart + _scanner.LexSize);
			result = _builder.Variable(_scanner.Prefix, _scanner.Name);
			PopPosInfo();
			_scanner.NextLex();
			break;
		}
		case LexKind.LParens:
			_scanner.NextLex();
			result = ParseExpr();
			_scanner.PassToken(LexKind.RParens);
			break;
		default:
			result = ParseFunctionCall();
			break;
		}
		return result;
	}

	private Node ParseFunctionCall()
	{
		List<Node> list = new List<Node>();
		string name = _scanner.Name;
		string prefix = _scanner.Prefix;
		int lexStart = _scanner.LexStart;
		_scanner.PassToken(LexKind.Name);
		_scanner.PassToken(LexKind.LParens);
		if (_scanner.Kind != LexKind.RParens)
		{
			while (true)
			{
				list.Add(ParseExpr());
				if (_scanner.Kind != LexKind.Comma)
				{
					break;
				}
				_scanner.NextLex();
			}
			_scanner.CheckToken(LexKind.RParens);
		}
		_scanner.NextLex();
		PushPosInfo(lexStart, _scanner.PrevLexEnd);
		Node result = _builder.Function(prefix, name, list);
		PopPosInfo();
		return result;
	}

	private void PushPosInfo(int startChar, int endChar)
	{
		_posInfo.Push(startChar);
		_posInfo.Push(endChar);
	}

	private void PopPosInfo()
	{
		_posInfo.Pop();
		_posInfo.Pop();
	}

	private void PopPosInfo(out int startChar, out int endChar)
	{
		endChar = _posInfo.Pop();
		startChar = _posInfo.Pop();
	}
}
