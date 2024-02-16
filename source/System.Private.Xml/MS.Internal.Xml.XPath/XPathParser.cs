using System;
using System.Collections.Generic;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal struct XPathParser
{
	private sealed class ParamInfo
	{
		private readonly Function.FunctionType _ftype;

		private readonly int _minargs;

		private readonly int _maxargs;

		private readonly XPathResultType[] _argTypes;

		public Function.FunctionType FType => _ftype;

		public int Minargs => _minargs;

		public int Maxargs => _maxargs;

		public XPathResultType[] ArgTypes => _argTypes;

		internal ParamInfo(Function.FunctionType ftype, int minargs, int maxargs, XPathResultType[] argTypes)
		{
			_ftype = ftype;
			_minargs = minargs;
			_maxargs = maxargs;
			_argTypes = argTypes;
		}
	}

	private XPathScanner _scanner;

	private int _parseDepth;

	private static readonly XPathResultType[] s_temparray1 = Array.Empty<XPathResultType>();

	private static readonly XPathResultType[] s_temparray2 = new XPathResultType[1] { XPathResultType.NodeSet };

	private static readonly XPathResultType[] s_temparray3 = new XPathResultType[1] { XPathResultType.Any };

	private static readonly XPathResultType[] s_temparray4 = new XPathResultType[1] { XPathResultType.String };

	private static readonly XPathResultType[] s_temparray5 = new XPathResultType[2]
	{
		XPathResultType.String,
		XPathResultType.String
	};

	private static readonly XPathResultType[] s_temparray6 = new XPathResultType[3]
	{
		XPathResultType.String,
		XPathResultType.Number,
		XPathResultType.Number
	};

	private static readonly XPathResultType[] s_temparray7 = new XPathResultType[3]
	{
		XPathResultType.String,
		XPathResultType.String,
		XPathResultType.String
	};

	private static readonly XPathResultType[] s_temparray8 = new XPathResultType[1] { XPathResultType.Boolean };

	private static readonly XPathResultType[] s_temparray9 = new XPathResultType[1];

	private static readonly Dictionary<string, ParamInfo> s_functionTable = CreateFunctionTable();

	private static readonly Dictionary<string, Axis.AxisType> s_AxesTable = CreateAxesTable();

	private bool IsNodeType
	{
		get
		{
			if (_scanner.Prefix.Length == 0)
			{
				if (!(_scanner.Name == "node") && !(_scanner.Name == "text") && !(_scanner.Name == "processing-instruction"))
				{
					return _scanner.Name == "comment";
				}
				return true;
			}
			return false;
		}
	}

	private bool IsPrimaryExpr
	{
		get
		{
			if (_scanner.Kind != XPathScanner.LexKind.String && _scanner.Kind != XPathScanner.LexKind.Number && _scanner.Kind != XPathScanner.LexKind.Dollar && _scanner.Kind != XPathScanner.LexKind.LParens)
			{
				if (_scanner.Kind == XPathScanner.LexKind.Name && _scanner.CanBeFunction)
				{
					return !IsNodeType;
				}
				return false;
			}
			return true;
		}
	}

	private XPathParser(string xpathExpr)
	{
		_scanner = new XPathScanner(xpathExpr);
		_parseDepth = 0;
	}

	public static AstNode ParseXPathExpression(string xpathExpression)
	{
		XPathParser xPathParser = new XPathParser(xpathExpression);
		AstNode result = xPathParser.ParseExpression(null);
		if (xPathParser._scanner.Kind != XPathScanner.LexKind.Eof)
		{
			throw XPathException.Create(System.SR.Xp_InvalidToken, xPathParser._scanner.SourceText);
		}
		return result;
	}

	public static AstNode ParseXPathPattern(string xpathPattern)
	{
		XPathParser xPathParser = new XPathParser(xpathPattern);
		AstNode result = xPathParser.ParsePattern();
		if (xPathParser._scanner.Kind != XPathScanner.LexKind.Eof)
		{
			throw XPathException.Create(System.SR.Xp_InvalidToken, xPathParser._scanner.SourceText);
		}
		return result;
	}

	private AstNode ParseExpression(AstNode qyInput)
	{
		if (++_parseDepth > 200)
		{
			throw XPathException.Create(System.SR.Xp_QueryTooComplex);
		}
		AstNode result = ParseOrExpr(qyInput);
		_parseDepth--;
		return result;
	}

	private AstNode ParseOrExpr(AstNode qyInput)
	{
		AstNode astNode = ParseAndExpr(qyInput);
		while (TestOp("or"))
		{
			NextLex();
			astNode = new Operator(Operator.Op.OR, astNode, ParseAndExpr(qyInput));
		}
		return astNode;
	}

	private AstNode ParseAndExpr(AstNode qyInput)
	{
		AstNode astNode = ParseEqualityExpr(qyInput);
		while (TestOp("and"))
		{
			NextLex();
			astNode = new Operator(Operator.Op.AND, astNode, ParseEqualityExpr(qyInput));
		}
		return astNode;
	}

	private AstNode ParseEqualityExpr(AstNode qyInput)
	{
		AstNode astNode = ParseRelationalExpr(qyInput);
		while (true)
		{
			Operator.Op op = ((_scanner.Kind == XPathScanner.LexKind.Eq) ? Operator.Op.EQ : ((_scanner.Kind == XPathScanner.LexKind.Ne) ? Operator.Op.NE : Operator.Op.INVALID));
			if (op == Operator.Op.INVALID)
			{
				break;
			}
			NextLex();
			astNode = new Operator(op, astNode, ParseRelationalExpr(qyInput));
		}
		return astNode;
	}

	private AstNode ParseRelationalExpr(AstNode qyInput)
	{
		AstNode astNode = ParseAdditiveExpr(qyInput);
		while (true)
		{
			Operator.Op op = ((_scanner.Kind == XPathScanner.LexKind.Lt) ? Operator.Op.LT : ((_scanner.Kind == XPathScanner.LexKind.Le) ? Operator.Op.LE : ((_scanner.Kind == XPathScanner.LexKind.Gt) ? Operator.Op.GT : ((_scanner.Kind == XPathScanner.LexKind.Ge) ? Operator.Op.GE : Operator.Op.INVALID))));
			if (op == Operator.Op.INVALID)
			{
				break;
			}
			NextLex();
			astNode = new Operator(op, astNode, ParseAdditiveExpr(qyInput));
		}
		return astNode;
	}

	private AstNode ParseAdditiveExpr(AstNode qyInput)
	{
		AstNode astNode = ParseMultiplicativeExpr(qyInput);
		while (true)
		{
			Operator.Op op = ((_scanner.Kind == XPathScanner.LexKind.Plus) ? Operator.Op.PLUS : ((_scanner.Kind == XPathScanner.LexKind.Minus) ? Operator.Op.MINUS : Operator.Op.INVALID));
			if (op == Operator.Op.INVALID)
			{
				break;
			}
			NextLex();
			astNode = new Operator(op, astNode, ParseMultiplicativeExpr(qyInput));
		}
		return astNode;
	}

	private AstNode ParseMultiplicativeExpr(AstNode qyInput)
	{
		AstNode astNode = ParseUnaryExpr(qyInput);
		while (true)
		{
			Operator.Op op = ((_scanner.Kind == XPathScanner.LexKind.Star) ? Operator.Op.MUL : (TestOp("div") ? Operator.Op.DIV : (TestOp("mod") ? Operator.Op.MOD : Operator.Op.INVALID)));
			if (op == Operator.Op.INVALID)
			{
				break;
			}
			NextLex();
			astNode = new Operator(op, astNode, ParseUnaryExpr(qyInput));
		}
		return astNode;
	}

	private AstNode ParseUnaryExpr(AstNode qyInput)
	{
		bool flag = false;
		while (_scanner.Kind == XPathScanner.LexKind.Minus)
		{
			NextLex();
			flag = !flag;
		}
		if (flag)
		{
			return new Operator(Operator.Op.MUL, ParseUnionExpr(qyInput), new Operand(-1.0));
		}
		return ParseUnionExpr(qyInput);
	}

	private AstNode ParseUnionExpr(AstNode qyInput)
	{
		AstNode astNode = ParsePathExpr(qyInput);
		while (_scanner.Kind == XPathScanner.LexKind.Union)
		{
			NextLex();
			AstNode astNode2 = ParsePathExpr(qyInput);
			CheckNodeSet(astNode.ReturnType);
			CheckNodeSet(astNode2.ReturnType);
			astNode = new Operator(Operator.Op.UNION, astNode, astNode2);
		}
		return astNode;
	}

	private AstNode ParsePathExpr(AstNode qyInput)
	{
		AstNode astNode;
		if (IsPrimaryExpr)
		{
			astNode = ParseFilterExpr(qyInput);
			if (_scanner.Kind == XPathScanner.LexKind.Slash)
			{
				NextLex();
				astNode = ParseRelativeLocationPath(astNode);
			}
			else if (_scanner.Kind == XPathScanner.LexKind.SlashSlash)
			{
				NextLex();
				astNode = ParseRelativeLocationPath(new Axis(Axis.AxisType.DescendantOrSelf, astNode));
			}
		}
		else
		{
			astNode = ParseLocationPath(null);
		}
		return astNode;
	}

	private AstNode ParseFilterExpr(AstNode qyInput)
	{
		AstNode astNode = ParsePrimaryExpr(qyInput);
		while (_scanner.Kind == XPathScanner.LexKind.LBracket)
		{
			astNode = new Filter(astNode, ParsePredicate(astNode));
		}
		return astNode;
	}

	private AstNode ParsePredicate(AstNode qyInput)
	{
		CheckNodeSet(qyInput.ReturnType);
		PassToken(XPathScanner.LexKind.LBracket);
		AstNode result = ParseExpression(qyInput);
		PassToken(XPathScanner.LexKind.RBracket);
		return result;
	}

	private AstNode ParseLocationPath(AstNode qyInput)
	{
		if (_scanner.Kind == XPathScanner.LexKind.Slash)
		{
			NextLex();
			AstNode astNode = new Root();
			if (IsStep(_scanner.Kind))
			{
				astNode = ParseRelativeLocationPath(astNode);
			}
			return astNode;
		}
		if (_scanner.Kind == XPathScanner.LexKind.SlashSlash)
		{
			NextLex();
			return ParseRelativeLocationPath(new Axis(Axis.AxisType.DescendantOrSelf, new Root()));
		}
		return ParseRelativeLocationPath(qyInput);
	}

	private AstNode ParseRelativeLocationPath(AstNode qyInput)
	{
		AstNode astNode = qyInput;
		while (true)
		{
			astNode = ParseStep(astNode);
			if (XPathScanner.LexKind.SlashSlash == _scanner.Kind)
			{
				NextLex();
				astNode = new Axis(Axis.AxisType.DescendantOrSelf, astNode);
				continue;
			}
			if (XPathScanner.LexKind.Slash != _scanner.Kind)
			{
				break;
			}
			NextLex();
		}
		return astNode;
	}

	private static bool IsStep(XPathScanner.LexKind lexKind)
	{
		if (lexKind != XPathScanner.LexKind.Dot && lexKind != XPathScanner.LexKind.DotDot && lexKind != XPathScanner.LexKind.At && lexKind != XPathScanner.LexKind.Axe && lexKind != XPathScanner.LexKind.Star)
		{
			return lexKind == XPathScanner.LexKind.Name;
		}
		return true;
	}

	private AstNode ParseStep(AstNode qyInput)
	{
		AstNode astNode;
		if (XPathScanner.LexKind.Dot == _scanner.Kind)
		{
			NextLex();
			astNode = new Axis(Axis.AxisType.Self, qyInput);
		}
		else if (XPathScanner.LexKind.DotDot == _scanner.Kind)
		{
			NextLex();
			astNode = new Axis(Axis.AxisType.Parent, qyInput);
		}
		else
		{
			Axis.AxisType axisType = Axis.AxisType.Child;
			switch (_scanner.Kind)
			{
			case XPathScanner.LexKind.At:
				axisType = Axis.AxisType.Attribute;
				NextLex();
				break;
			case XPathScanner.LexKind.Axe:
				axisType = GetAxis();
				NextLex();
				break;
			}
			XPathNodeType nodeType = ((axisType != Axis.AxisType.Attribute) ? XPathNodeType.Element : XPathNodeType.Attribute);
			astNode = ParseNodeTest(qyInput, axisType, nodeType);
			while (XPathScanner.LexKind.LBracket == _scanner.Kind)
			{
				astNode = new Filter(astNode, ParsePredicate(astNode));
			}
		}
		return astNode;
	}

	private AstNode ParseNodeTest(AstNode qyInput, Axis.AxisType axisType, XPathNodeType nodeType)
	{
		string prefix;
		string text;
		switch (_scanner.Kind)
		{
		case XPathScanner.LexKind.Name:
			if (_scanner.CanBeFunction && IsNodeType)
			{
				prefix = string.Empty;
				text = string.Empty;
				nodeType = ((_scanner.Name == "comment") ? XPathNodeType.Comment : ((_scanner.Name == "text") ? XPathNodeType.Text : ((_scanner.Name == "node") ? XPathNodeType.All : ((_scanner.Name == "processing-instruction") ? XPathNodeType.ProcessingInstruction : XPathNodeType.Root))));
				NextLex();
				PassToken(XPathScanner.LexKind.LParens);
				if (nodeType == XPathNodeType.ProcessingInstruction && _scanner.Kind != XPathScanner.LexKind.RParens)
				{
					CheckToken(XPathScanner.LexKind.String);
					text = _scanner.StringValue;
					NextLex();
				}
				PassToken(XPathScanner.LexKind.RParens);
			}
			else
			{
				prefix = _scanner.Prefix;
				text = _scanner.Name;
				NextLex();
				if (text == "*")
				{
					text = string.Empty;
				}
			}
			break;
		case XPathScanner.LexKind.Star:
			prefix = string.Empty;
			text = string.Empty;
			NextLex();
			break;
		default:
			throw XPathException.Create(System.SR.Xp_NodeSetExpected, _scanner.SourceText);
		}
		return new Axis(axisType, qyInput, prefix, text, nodeType);
	}

	private AstNode ParsePrimaryExpr(AstNode qyInput)
	{
		AstNode astNode = null;
		switch (_scanner.Kind)
		{
		case XPathScanner.LexKind.String:
			astNode = new Operand(_scanner.StringValue);
			NextLex();
			break;
		case XPathScanner.LexKind.Number:
			astNode = new Operand(_scanner.NumberValue);
			NextLex();
			break;
		case XPathScanner.LexKind.Dollar:
			NextLex();
			CheckToken(XPathScanner.LexKind.Name);
			astNode = new Variable(_scanner.Name, _scanner.Prefix);
			NextLex();
			break;
		case XPathScanner.LexKind.LParens:
			NextLex();
			astNode = ParseExpression(qyInput);
			if (astNode.Type != AstNode.AstType.ConstantOperand)
			{
				astNode = new Group(astNode);
			}
			PassToken(XPathScanner.LexKind.RParens);
			break;
		case XPathScanner.LexKind.Name:
			if (_scanner.CanBeFunction && !IsNodeType)
			{
				astNode = ParseMethod(null);
			}
			break;
		}
		return astNode;
	}

	private AstNode ParseMethod(AstNode qyInput)
	{
		List<AstNode> list = new List<AstNode>();
		string name = _scanner.Name;
		string prefix = _scanner.Prefix;
		PassToken(XPathScanner.LexKind.Name);
		PassToken(XPathScanner.LexKind.LParens);
		if (_scanner.Kind != XPathScanner.LexKind.RParens)
		{
			while (true)
			{
				list.Add(ParseExpression(qyInput));
				if (_scanner.Kind == XPathScanner.LexKind.RParens)
				{
					break;
				}
				PassToken(XPathScanner.LexKind.Comma);
			}
		}
		PassToken(XPathScanner.LexKind.RParens);
		if (prefix.Length == 0 && s_functionTable.TryGetValue(name, out var value))
		{
			int num = list.Count;
			if (num < value.Minargs)
			{
				throw XPathException.Create(System.SR.Xp_InvalidNumArgs, name, _scanner.SourceText);
			}
			if (value.FType == Function.FunctionType.FuncConcat)
			{
				for (int i = 0; i < num; i++)
				{
					AstNode astNode = list[i];
					if (astNode.ReturnType != XPathResultType.String)
					{
						astNode = new Function(Function.FunctionType.FuncString, astNode);
					}
					list[i] = astNode;
				}
			}
			else
			{
				if (value.Maxargs < num)
				{
					throw XPathException.Create(System.SR.Xp_InvalidNumArgs, name, _scanner.SourceText);
				}
				if (value.ArgTypes.Length < num)
				{
					num = value.ArgTypes.Length;
				}
				for (int j = 0; j < num; j++)
				{
					AstNode astNode2 = list[j];
					if (value.ArgTypes[j] == XPathResultType.Any || value.ArgTypes[j] == astNode2.ReturnType)
					{
						continue;
					}
					switch (value.ArgTypes[j])
					{
					case XPathResultType.NodeSet:
						if (!(astNode2 is Variable) && (!(astNode2 is Function) || astNode2.ReturnType != XPathResultType.Any))
						{
							throw XPathException.Create(System.SR.Xp_InvalidArgumentType, name, _scanner.SourceText);
						}
						break;
					case XPathResultType.String:
						astNode2 = new Function(Function.FunctionType.FuncString, astNode2);
						break;
					case XPathResultType.Number:
						astNode2 = new Function(Function.FunctionType.FuncNumber, astNode2);
						break;
					case XPathResultType.Boolean:
						astNode2 = new Function(Function.FunctionType.FuncBoolean, astNode2);
						break;
					}
					list[j] = astNode2;
				}
			}
			return new Function(value.FType, list);
		}
		return new Function(prefix, name, list);
	}

	private AstNode ParsePattern()
	{
		AstNode astNode = ParseLocationPathPattern();
		while (_scanner.Kind == XPathScanner.LexKind.Union)
		{
			NextLex();
			astNode = new Operator(Operator.Op.UNION, astNode, ParseLocationPathPattern());
		}
		return astNode;
	}

	private AstNode ParseLocationPathPattern()
	{
		AstNode astNode = null;
		switch (_scanner.Kind)
		{
		case XPathScanner.LexKind.Slash:
			NextLex();
			astNode = new Root();
			if (_scanner.Kind == XPathScanner.LexKind.Eof || _scanner.Kind == XPathScanner.LexKind.Union)
			{
				return astNode;
			}
			break;
		case XPathScanner.LexKind.SlashSlash:
			NextLex();
			astNode = new Axis(Axis.AxisType.DescendantOrSelf, new Root());
			break;
		case XPathScanner.LexKind.Name:
			if (!_scanner.CanBeFunction)
			{
				break;
			}
			astNode = ParseIdKeyPattern();
			if (astNode != null)
			{
				switch (_scanner.Kind)
				{
				case XPathScanner.LexKind.Slash:
					NextLex();
					break;
				case XPathScanner.LexKind.SlashSlash:
					NextLex();
					astNode = new Axis(Axis.AxisType.DescendantOrSelf, astNode);
					break;
				default:
					return astNode;
				}
			}
			break;
		}
		return ParseRelativePathPattern(astNode);
	}

	private AstNode ParseIdKeyPattern()
	{
		List<AstNode> list = new List<AstNode>();
		if (_scanner.Prefix.Length == 0)
		{
			if (_scanner.Name == "id")
			{
				ParamInfo paramInfo = s_functionTable["id"];
				NextLex();
				PassToken(XPathScanner.LexKind.LParens);
				CheckToken(XPathScanner.LexKind.String);
				list.Add(new Operand(_scanner.StringValue));
				NextLex();
				PassToken(XPathScanner.LexKind.RParens);
				return new Function(paramInfo.FType, list);
			}
			if (_scanner.Name == "key")
			{
				NextLex();
				PassToken(XPathScanner.LexKind.LParens);
				CheckToken(XPathScanner.LexKind.String);
				list.Add(new Operand(_scanner.StringValue));
				NextLex();
				PassToken(XPathScanner.LexKind.Comma);
				CheckToken(XPathScanner.LexKind.String);
				list.Add(new Operand(_scanner.StringValue));
				NextLex();
				PassToken(XPathScanner.LexKind.RParens);
				return new Function("", "key", list);
			}
		}
		return null;
	}

	private AstNode ParseRelativePathPattern(AstNode qyInput)
	{
		AstNode astNode = ParseStepPattern(qyInput);
		if (XPathScanner.LexKind.SlashSlash == _scanner.Kind)
		{
			NextLex();
			astNode = ParseRelativePathPattern(new Axis(Axis.AxisType.DescendantOrSelf, astNode));
		}
		else if (XPathScanner.LexKind.Slash == _scanner.Kind)
		{
			NextLex();
			astNode = ParseRelativePathPattern(astNode);
		}
		return astNode;
	}

	private AstNode ParseStepPattern(AstNode qyInput)
	{
		Axis.AxisType axisType = Axis.AxisType.Child;
		switch (_scanner.Kind)
		{
		case XPathScanner.LexKind.At:
			axisType = Axis.AxisType.Attribute;
			NextLex();
			break;
		case XPathScanner.LexKind.Axe:
			axisType = GetAxis();
			if (axisType != Axis.AxisType.Child && axisType != Axis.AxisType.Attribute)
			{
				throw XPathException.Create(System.SR.Xp_InvalidToken, _scanner.SourceText);
			}
			NextLex();
			break;
		}
		XPathNodeType nodeType = ((axisType != Axis.AxisType.Attribute) ? XPathNodeType.Element : XPathNodeType.Attribute);
		AstNode astNode = ParseNodeTest(qyInput, axisType, nodeType);
		while (XPathScanner.LexKind.LBracket == _scanner.Kind)
		{
			astNode = new Filter(astNode, ParsePredicate(astNode));
		}
		return astNode;
	}

	private void CheckToken(XPathScanner.LexKind t)
	{
		if (_scanner.Kind != t)
		{
			throw XPathException.Create(System.SR.Xp_InvalidToken, _scanner.SourceText);
		}
	}

	private void PassToken(XPathScanner.LexKind t)
	{
		CheckToken(t);
		NextLex();
	}

	private void NextLex()
	{
		_scanner.NextLex();
	}

	private bool TestOp(string op)
	{
		if (_scanner.Kind == XPathScanner.LexKind.Name && _scanner.Prefix.Length == 0)
		{
			return _scanner.Name.Equals(op);
		}
		return false;
	}

	private void CheckNodeSet(XPathResultType t)
	{
		if (t != XPathResultType.NodeSet && t != XPathResultType.Any)
		{
			throw XPathException.Create(System.SR.Xp_NodeSetExpected, _scanner.SourceText);
		}
	}

	private static Dictionary<string, ParamInfo> CreateFunctionTable()
	{
		Dictionary<string, ParamInfo> dictionary = new Dictionary<string, ParamInfo>(36);
		dictionary.Add("last", new ParamInfo(Function.FunctionType.FuncLast, 0, 0, s_temparray1));
		dictionary.Add("position", new ParamInfo(Function.FunctionType.FuncPosition, 0, 0, s_temparray1));
		dictionary.Add("name", new ParamInfo(Function.FunctionType.FuncName, 0, 1, s_temparray2));
		dictionary.Add("namespace-uri", new ParamInfo(Function.FunctionType.FuncNameSpaceUri, 0, 1, s_temparray2));
		dictionary.Add("local-name", new ParamInfo(Function.FunctionType.FuncLocalName, 0, 1, s_temparray2));
		dictionary.Add("count", new ParamInfo(Function.FunctionType.FuncCount, 1, 1, s_temparray2));
		dictionary.Add("id", new ParamInfo(Function.FunctionType.FuncID, 1, 1, s_temparray3));
		dictionary.Add("string", new ParamInfo(Function.FunctionType.FuncString, 0, 1, s_temparray3));
		dictionary.Add("concat", new ParamInfo(Function.FunctionType.FuncConcat, 2, 100, s_temparray4));
		dictionary.Add("starts-with", new ParamInfo(Function.FunctionType.FuncStartsWith, 2, 2, s_temparray5));
		dictionary.Add("contains", new ParamInfo(Function.FunctionType.FuncContains, 2, 2, s_temparray5));
		dictionary.Add("substring-before", new ParamInfo(Function.FunctionType.FuncSubstringBefore, 2, 2, s_temparray5));
		dictionary.Add("substring-after", new ParamInfo(Function.FunctionType.FuncSubstringAfter, 2, 2, s_temparray5));
		dictionary.Add("substring", new ParamInfo(Function.FunctionType.FuncSubstring, 2, 3, s_temparray6));
		dictionary.Add("string-length", new ParamInfo(Function.FunctionType.FuncStringLength, 0, 1, s_temparray4));
		dictionary.Add("normalize-space", new ParamInfo(Function.FunctionType.FuncNormalize, 0, 1, s_temparray4));
		dictionary.Add("translate", new ParamInfo(Function.FunctionType.FuncTranslate, 3, 3, s_temparray7));
		dictionary.Add("boolean", new ParamInfo(Function.FunctionType.FuncBoolean, 1, 1, s_temparray3));
		dictionary.Add("not", new ParamInfo(Function.FunctionType.FuncNot, 1, 1, s_temparray8));
		dictionary.Add("true", new ParamInfo(Function.FunctionType.FuncTrue, 0, 0, s_temparray8));
		dictionary.Add("false", new ParamInfo(Function.FunctionType.FuncFalse, 0, 0, s_temparray8));
		dictionary.Add("lang", new ParamInfo(Function.FunctionType.FuncLang, 1, 1, s_temparray4));
		dictionary.Add("number", new ParamInfo(Function.FunctionType.FuncNumber, 0, 1, s_temparray3));
		dictionary.Add("sum", new ParamInfo(Function.FunctionType.FuncSum, 1, 1, s_temparray2));
		dictionary.Add("floor", new ParamInfo(Function.FunctionType.FuncFloor, 1, 1, s_temparray9));
		dictionary.Add("ceiling", new ParamInfo(Function.FunctionType.FuncCeiling, 1, 1, s_temparray9));
		dictionary.Add("round", new ParamInfo(Function.FunctionType.FuncRound, 1, 1, s_temparray9));
		return dictionary;
	}

	private static Dictionary<string, Axis.AxisType> CreateAxesTable()
	{
		Dictionary<string, Axis.AxisType> dictionary = new Dictionary<string, Axis.AxisType>(13);
		dictionary.Add("ancestor", Axis.AxisType.Ancestor);
		dictionary.Add("ancestor-or-self", Axis.AxisType.AncestorOrSelf);
		dictionary.Add("attribute", Axis.AxisType.Attribute);
		dictionary.Add("child", Axis.AxisType.Child);
		dictionary.Add("descendant", Axis.AxisType.Descendant);
		dictionary.Add("descendant-or-self", Axis.AxisType.DescendantOrSelf);
		dictionary.Add("following", Axis.AxisType.Following);
		dictionary.Add("following-sibling", Axis.AxisType.FollowingSibling);
		dictionary.Add("namespace", Axis.AxisType.Namespace);
		dictionary.Add("parent", Axis.AxisType.Parent);
		dictionary.Add("preceding", Axis.AxisType.Preceding);
		dictionary.Add("preceding-sibling", Axis.AxisType.PrecedingSibling);
		dictionary.Add("self", Axis.AxisType.Self);
		return dictionary;
	}

	private Axis.AxisType GetAxis()
	{
		if (!s_AxesTable.TryGetValue(_scanner.Name, out var value))
		{
			throw XPathException.Create(System.SR.Xp_InvalidToken, _scanner.SourceText);
		}
		return value;
	}
}
