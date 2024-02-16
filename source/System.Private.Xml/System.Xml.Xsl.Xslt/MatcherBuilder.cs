using System.Collections.Generic;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt;

internal sealed class MatcherBuilder
{
	private readonly XPathQilFactory _f;

	private readonly ReferenceReplacer _refReplacer;

	private readonly InvokeGenerator _invkGen;

	private int _priority = -1;

	private readonly PatternBag _elementPatterns = new PatternBag();

	private readonly PatternBag _attributePatterns = new PatternBag();

	private readonly List<Pattern> _textPatterns = new List<Pattern>();

	private readonly List<Pattern> _documentPatterns = new List<Pattern>();

	private readonly List<Pattern> _commentPatterns = new List<Pattern>();

	private readonly PatternBag _piPatterns = new PatternBag();

	private readonly List<Pattern> _heterogenousPatterns = new List<Pattern>();

	private readonly List<List<TemplateMatch>> _allMatches = new List<List<TemplateMatch>>();

	public MatcherBuilder(XPathQilFactory f, ReferenceReplacer refReplacer, InvokeGenerator invkGen)
	{
		_f = f;
		_refReplacer = refReplacer;
		_invkGen = invkGen;
	}

	private void Clear()
	{
		_priority = -1;
		_elementPatterns.Clear();
		_attributePatterns.Clear();
		_textPatterns.Clear();
		_documentPatterns.Clear();
		_commentPatterns.Clear();
		_piPatterns.Clear();
		_heterogenousPatterns.Clear();
		_allMatches.Clear();
	}

	private void AddPatterns(List<TemplateMatch> matches)
	{
		foreach (TemplateMatch match in matches)
		{
			Pattern pattern = new Pattern(match, ++_priority);
			switch (match.NodeKind)
			{
			case XmlNodeKindFlags.Element:
				_elementPatterns.Add(pattern);
				break;
			case XmlNodeKindFlags.Attribute:
				_attributePatterns.Add(pattern);
				break;
			case XmlNodeKindFlags.Text:
				_textPatterns.Add(pattern);
				break;
			case XmlNodeKindFlags.Document:
				_documentPatterns.Add(pattern);
				break;
			case XmlNodeKindFlags.Comment:
				_commentPatterns.Add(pattern);
				break;
			case XmlNodeKindFlags.PI:
				_piPatterns.Add(pattern);
				break;
			default:
				_heterogenousPatterns.Add(pattern);
				break;
			}
		}
	}

	private void CollectPatternsInternal(Stylesheet sheet, QilName mode)
	{
		Stylesheet[] imports = sheet.Imports;
		foreach (Stylesheet sheet2 in imports)
		{
			CollectPatternsInternal(sheet2, mode);
		}
		if (sheet.TemplateMatches.TryGetValue(mode, out var value))
		{
			AddPatterns(value);
			_allMatches.Add(value);
		}
	}

	public void CollectPatterns(StylesheetLevel sheet, QilName mode)
	{
		Clear();
		Stylesheet[] imports = sheet.Imports;
		foreach (Stylesheet sheet2 in imports)
		{
			CollectPatternsInternal(sheet2, mode);
		}
	}

	private QilNode MatchPattern(QilIterator it, TemplateMatch match)
	{
		QilNode condition = match.Condition;
		if (condition == null)
		{
			return _f.True();
		}
		condition = condition.DeepClone(_f.BaseFactory);
		return _refReplacer.Replace(condition, match.Iterator, it);
	}

	private QilNode MatchPatterns(QilIterator it, List<Pattern> patternList)
	{
		QilNode qilNode = _f.Int32(-1);
		foreach (Pattern pattern in patternList)
		{
			qilNode = _f.Conditional(MatchPattern(it, pattern.Match), _f.Int32(pattern.Priority), qilNode);
		}
		return qilNode;
	}

	private QilNode MatchPatterns(QilIterator it, XmlQueryType xt, List<Pattern> patternList, QilNode otherwise)
	{
		if (patternList.Count == 0)
		{
			return otherwise;
		}
		return _f.Conditional(_f.IsType(it, xt), MatchPatterns(it, patternList), otherwise);
	}

	private bool IsNoMatch(QilNode matcher)
	{
		if (matcher.NodeType == QilNodeType.LiteralInt32)
		{
			return true;
		}
		return false;
	}

	private QilNode MatchPatternsWhosePriorityGreater(QilIterator it, List<Pattern> patternList, QilNode matcher)
	{
		if (patternList.Count == 0)
		{
			return matcher;
		}
		if (IsNoMatch(matcher))
		{
			return MatchPatterns(it, patternList);
		}
		QilIterator qilIterator = _f.Let(matcher);
		QilNode qilNode = _f.Int32(-1);
		int num = -1;
		foreach (Pattern pattern in patternList)
		{
			if (pattern.Priority > num + 1)
			{
				qilNode = _f.Conditional(_f.Gt(qilIterator, _f.Int32(num)), qilIterator, qilNode);
			}
			qilNode = _f.Conditional(MatchPattern(it, pattern.Match), _f.Int32(pattern.Priority), qilNode);
			num = pattern.Priority;
		}
		if (num != _priority)
		{
			qilNode = _f.Conditional(_f.Gt(qilIterator, _f.Int32(num)), qilIterator, qilNode);
		}
		return _f.Loop(qilIterator, qilNode);
	}

	private QilNode MatchPatterns(QilIterator it, XmlQueryType xt, PatternBag patternBag, QilNode otherwise)
	{
		if (patternBag.FixedNamePatternsNames.Count == 0)
		{
			return MatchPatterns(it, xt, patternBag.NonFixedNamePatterns, otherwise);
		}
		QilNode qilNode = _f.Int32(-1);
		foreach (QilName fixedNamePatternsName in patternBag.FixedNamePatternsNames)
		{
			qilNode = _f.Conditional(_f.Eq(_f.NameOf(it), fixedNamePatternsName.ShallowClone(_f.BaseFactory)), MatchPatterns(it, patternBag.FixedNamePatterns[fixedNamePatternsName]), qilNode);
		}
		qilNode = MatchPatternsWhosePriorityGreater(it, patternBag.NonFixedNamePatterns, qilNode);
		return _f.Conditional(_f.IsType(it, xt), qilNode, otherwise);
	}

	public QilNode BuildMatcher(QilIterator it, IList<XslNode> actualArgs, QilNode otherwise)
	{
		QilNode otherwise2 = _f.Int32(-1);
		otherwise2 = MatchPatterns(it, XmlQueryTypeFactory.PI, _piPatterns, otherwise2);
		otherwise2 = MatchPatterns(it, XmlQueryTypeFactory.Comment, _commentPatterns, otherwise2);
		otherwise2 = MatchPatterns(it, XmlQueryTypeFactory.Document, _documentPatterns, otherwise2);
		otherwise2 = MatchPatterns(it, XmlQueryTypeFactory.Text, _textPatterns, otherwise2);
		otherwise2 = MatchPatterns(it, XmlQueryTypeFactory.Attribute, _attributePatterns, otherwise2);
		otherwise2 = MatchPatterns(it, XmlQueryTypeFactory.Element, _elementPatterns, otherwise2);
		otherwise2 = MatchPatternsWhosePriorityGreater(it, _heterogenousPatterns, otherwise2);
		if (IsNoMatch(otherwise2))
		{
			return otherwise;
		}
		QilNode[] array = new QilNode[_priority + 2];
		int num = -1;
		foreach (List<TemplateMatch> allMatch in _allMatches)
		{
			foreach (TemplateMatch item in allMatch)
			{
				array[++num] = _invkGen.GenerateInvoke(item.TemplateFunction, actualArgs);
			}
		}
		array[++num] = otherwise;
		return _f.Choice(otherwise2, _f.BranchList(array));
	}
}
