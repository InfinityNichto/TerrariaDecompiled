using System.Collections.Generic;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class Function : AstNode
{
	public enum FunctionType
	{
		FuncLast,
		FuncPosition,
		FuncCount,
		FuncID,
		FuncLocalName,
		FuncNameSpaceUri,
		FuncName,
		FuncString,
		FuncBoolean,
		FuncNumber,
		FuncTrue,
		FuncFalse,
		FuncNot,
		FuncConcat,
		FuncStartsWith,
		FuncContains,
		FuncSubstringBefore,
		FuncSubstringAfter,
		FuncSubstring,
		FuncStringLength,
		FuncNormalize,
		FuncTranslate,
		FuncLang,
		FuncSum,
		FuncFloor,
		FuncCeiling,
		FuncRound,
		FuncUserDefined
	}

	private readonly FunctionType _functionType;

	private readonly List<AstNode> _argumentList;

	private readonly string _name;

	private readonly string _prefix;

	internal static XPathResultType[] ReturnTypes = new XPathResultType[28]
	{
		XPathResultType.Number,
		XPathResultType.Number,
		XPathResultType.Number,
		XPathResultType.NodeSet,
		XPathResultType.String,
		XPathResultType.String,
		XPathResultType.String,
		XPathResultType.String,
		XPathResultType.Boolean,
		XPathResultType.Number,
		XPathResultType.Boolean,
		XPathResultType.Boolean,
		XPathResultType.Boolean,
		XPathResultType.String,
		XPathResultType.Boolean,
		XPathResultType.Boolean,
		XPathResultType.String,
		XPathResultType.String,
		XPathResultType.String,
		XPathResultType.Number,
		XPathResultType.String,
		XPathResultType.String,
		XPathResultType.Boolean,
		XPathResultType.Number,
		XPathResultType.Number,
		XPathResultType.Number,
		XPathResultType.Number,
		XPathResultType.Any
	};

	public override AstType Type => AstType.Function;

	public override XPathResultType ReturnType => ReturnTypes[(int)_functionType];

	public FunctionType TypeOfFunction => _functionType;

	public List<AstNode> ArgumentList => _argumentList;

	public string Prefix => _prefix;

	public string Name => _name;

	public Function(FunctionType ftype, List<AstNode> argumentList)
	{
		_functionType = ftype;
		_argumentList = new List<AstNode>(argumentList);
	}

	public Function(string prefix, string name, List<AstNode> argumentList)
	{
		_functionType = FunctionType.FuncUserDefined;
		_prefix = prefix;
		_name = name;
		_argumentList = new List<AstNode>(argumentList);
	}

	public Function(FunctionType ftype, AstNode arg)
	{
		_functionType = ftype;
		_argumentList = new List<AstNode>();
		_argumentList.Add(arg);
	}
}
