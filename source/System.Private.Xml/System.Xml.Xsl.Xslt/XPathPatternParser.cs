using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt;

internal sealed class XPathPatternParser
{
	public interface IPatternBuilder : IXPathBuilder<QilNode>
	{
		IXPathBuilder<QilNode> GetPredicateBuilder(QilNode context);
	}

	private XPathScanner _scanner;

	private IPatternBuilder _ptrnBuilder;

	private readonly XPathParser<QilNode> _predicateParser = new XPathParser<QilNode>();

	private int _parseRelativePath;

	public QilNode Parse(XPathScanner scanner, IPatternBuilder ptrnBuilder)
	{
		QilNode result = null;
		ptrnBuilder.StartBuild();
		try
		{
			_scanner = scanner;
			_ptrnBuilder = ptrnBuilder;
			result = ParsePattern();
			_scanner.CheckToken(LexKind.Eof);
		}
		finally
		{
			result = ptrnBuilder.EndBuild(result);
		}
		return result;
	}

	private QilNode ParsePattern()
	{
		QilNode qilNode = ParseLocationPathPattern();
		while (_scanner.Kind == LexKind.Union)
		{
			_scanner.NextLex();
			qilNode = _ptrnBuilder.Operator(XPathOperator.Union, qilNode, ParseLocationPathPattern());
		}
		return qilNode;
	}

	private QilNode ParseLocationPathPattern()
	{
		switch (_scanner.Kind)
		{
		case LexKind.Slash:
		{
			_scanner.NextLex();
			QilNode qilNode = _ptrnBuilder.Axis(XPathAxis.Root, XPathNodeType.All, null, null);
			if (XPathParser<QilNode>.IsStep(_scanner.Kind))
			{
				qilNode = _ptrnBuilder.JoinStep(qilNode, ParseRelativePathPattern());
			}
			return qilNode;
		}
		case LexKind.SlashSlash:
			_scanner.NextLex();
			return _ptrnBuilder.JoinStep(_ptrnBuilder.Axis(XPathAxis.Root, XPathNodeType.All, null, null), _ptrnBuilder.JoinStep(_ptrnBuilder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null), ParseRelativePathPattern()));
		case LexKind.Name:
			if (_scanner.CanBeFunction && _scanner.Prefix.Length == 0 && (_scanner.Name == "id" || _scanner.Name == "key"))
			{
				QilNode qilNode = ParseIdKeyPattern();
				switch (_scanner.Kind)
				{
				case LexKind.Slash:
					_scanner.NextLex();
					qilNode = _ptrnBuilder.JoinStep(qilNode, ParseRelativePathPattern());
					break;
				case LexKind.SlashSlash:
					_scanner.NextLex();
					qilNode = _ptrnBuilder.JoinStep(qilNode, _ptrnBuilder.JoinStep(_ptrnBuilder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null), ParseRelativePathPattern()));
					break;
				}
				return qilNode;
			}
			break;
		}
		return ParseRelativePathPattern();
	}

	private QilNode ParseIdKeyPattern()
	{
		List<QilNode> list = new List<QilNode>(2);
		if (_scanner.Name == "id")
		{
			_scanner.NextLex();
			_scanner.PassToken(LexKind.LParens);
			_scanner.CheckToken(LexKind.String);
			list.Add(_ptrnBuilder.String(_scanner.StringValue));
			_scanner.NextLex();
			_scanner.PassToken(LexKind.RParens);
			return _ptrnBuilder.Function("", "id", list);
		}
		_scanner.NextLex();
		_scanner.PassToken(LexKind.LParens);
		_scanner.CheckToken(LexKind.String);
		list.Add(_ptrnBuilder.String(_scanner.StringValue));
		_scanner.NextLex();
		_scanner.PassToken(LexKind.Comma);
		_scanner.CheckToken(LexKind.String);
		list.Add(_ptrnBuilder.String(_scanner.StringValue));
		_scanner.NextLex();
		_scanner.PassToken(LexKind.RParens);
		return _ptrnBuilder.Function("", "key", list);
	}

	private QilNode ParseRelativePathPattern()
	{
		if (++_parseRelativePath > 1024 && System.LocalAppContextSwitches.LimitXPathComplexity)
		{
			throw _scanner.CreateException(System.SR.Xslt_InputTooComplex);
		}
		QilNode qilNode = ParseStepPattern();
		if (_scanner.Kind == LexKind.Slash)
		{
			_scanner.NextLex();
			qilNode = _ptrnBuilder.JoinStep(qilNode, ParseRelativePathPattern());
		}
		else if (_scanner.Kind == LexKind.SlashSlash)
		{
			_scanner.NextLex();
			qilNode = _ptrnBuilder.JoinStep(qilNode, _ptrnBuilder.JoinStep(_ptrnBuilder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null), ParseRelativePathPattern()));
		}
		_parseRelativePath--;
		return qilNode;
	}

	private QilNode ParseStepPattern()
	{
		XPathAxis xPathAxis;
		switch (_scanner.Kind)
		{
		case LexKind.DotDot:
		case LexKind.Dot:
			throw _scanner.CreateException(System.SR.XPath_InvalidAxisInPattern);
		case LexKind.At:
			xPathAxis = XPathAxis.Attribute;
			_scanner.NextLex();
			break;
		case LexKind.Axis:
			xPathAxis = _scanner.Axis;
			if (xPathAxis != XPathAxis.Child && xPathAxis != XPathAxis.Attribute)
			{
				throw _scanner.CreateException(System.SR.XPath_InvalidAxisInPattern);
			}
			_scanner.NextLex();
			_scanner.NextLex();
			break;
		case LexKind.Name:
		case LexKind.Star:
			xPathAxis = XPathAxis.Child;
			break;
		default:
			throw _scanner.CreateException(System.SR.XPath_UnexpectedToken, _scanner.RawValue);
		}
		XPathParser<QilNode>.InternalParseNodeTest(_scanner, xPathAxis, out var nodeType, out var nodePrefix, out var nodeName);
		QilNode qilNode = _ptrnBuilder.Axis(xPathAxis, nodeType, nodePrefix, nodeName);
		if (_ptrnBuilder is XPathPatternBuilder xPathPatternBuilder)
		{
			List<QilNode> list = new List<QilNode>();
			while (_scanner.Kind == LexKind.LBracket)
			{
				list.Add(ParsePredicate(qilNode));
			}
			if (list.Count > 0)
			{
				qilNode = xPathPatternBuilder.BuildPredicates(qilNode, list);
			}
		}
		else
		{
			while (_scanner.Kind == LexKind.LBracket)
			{
				qilNode = _ptrnBuilder.Predicate(qilNode, ParsePredicate(qilNode), reverseStep: false);
			}
		}
		return qilNode;
	}

	private QilNode ParsePredicate(QilNode context)
	{
		_scanner.NextLex();
		QilNode result = _predicateParser.Parse(_scanner, _ptrnBuilder.GetPredicateBuilder(context), LexKind.RBracket);
		_scanner.NextLex();
		return result;
	}
}
