using System.Collections.Generic;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class Stylesheet : StylesheetLevel
{
	private readonly Compiler _compiler;

	public List<Uri> ImportHrefs = new List<Uri>();

	public List<XslNode> GlobalVarPars = new List<XslNode>();

	public Dictionary<QilName, AttributeSet> AttributeSets = new Dictionary<QilName, AttributeSet>();

	private readonly int _importPrecedence;

	private int _orderNumber;

	public List<WhitespaceRule>[] WhitespaceRules = new List<WhitespaceRule>[3];

	public List<Template> Templates = new List<Template>();

	public Dictionary<QilName, List<TemplateMatch>> TemplateMatches = new Dictionary<QilName, List<TemplateMatch>>();

	public int ImportPrecedence => _importPrecedence;

	public void AddTemplateMatch(Template template, QilLoop filter)
	{
		if (!TemplateMatches.TryGetValue(template.Mode, out var value))
		{
			List<TemplateMatch> list2 = (TemplateMatches[template.Mode] = new List<TemplateMatch>());
			value = list2;
		}
		value.Add(new TemplateMatch(template, filter));
	}

	public void SortTemplateMatches()
	{
		foreach (QilName key in TemplateMatches.Keys)
		{
			TemplateMatches[key].Sort(TemplateMatch.Comparer);
		}
	}

	public Stylesheet(Compiler compiler, int importPrecedence)
	{
		_compiler = compiler;
		_importPrecedence = importPrecedence;
		WhitespaceRules[0] = new List<WhitespaceRule>();
		WhitespaceRules[1] = new List<WhitespaceRule>();
		WhitespaceRules[2] = new List<WhitespaceRule>();
	}

	public void AddWhitespaceRule(int index, WhitespaceRule rule)
	{
		WhitespaceRules[index].Add(rule);
	}

	public bool AddVarPar(VarPar var)
	{
		foreach (XslNode globalVarPar in GlobalVarPars)
		{
			if (globalVarPar.Name.Equals(var.Name))
			{
				return _compiler.AllGlobalVarPars.ContainsKey(var.Name);
			}
		}
		GlobalVarPars.Add(var);
		return true;
	}

	public bool AddTemplate(Template template)
	{
		template.ImportPrecedence = _importPrecedence;
		template.OrderNumber = _orderNumber++;
		_compiler.AllTemplates.Add(template);
		if (template.Name != null)
		{
			if (!_compiler.NamedTemplates.TryGetValue(template.Name, out var value))
			{
				_compiler.NamedTemplates[template.Name] = template;
			}
			else if (value.ImportPrecedence == template.ImportPrecedence)
			{
				return false;
			}
		}
		if (template.Match != null)
		{
			Templates.Add(template);
		}
		return true;
	}
}
