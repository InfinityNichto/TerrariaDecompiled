using System.Collections;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class TemplateManager
{
	private sealed class TemplateComparer : IComparer
	{
		public int Compare(object x, object y)
		{
			TemplateAction templateAction = (TemplateAction)x;
			TemplateAction templateAction2 = (TemplateAction)y;
			if (templateAction.Priority == templateAction2.Priority)
			{
				return templateAction.TemplateId - templateAction2.TemplateId;
			}
			if (!(templateAction.Priority > templateAction2.Priority))
			{
				return -1;
			}
			return 1;
		}
	}

	private readonly XmlQualifiedName _mode;

	internal ArrayList templates;

	private readonly Stylesheet _stylesheet;

	private static readonly TemplateComparer s_TemplateComparer = new TemplateComparer();

	internal TemplateManager(Stylesheet stylesheet, XmlQualifiedName mode)
	{
		_mode = mode;
		_stylesheet = stylesheet;
	}

	internal void AddTemplate(TemplateAction template)
	{
		if (templates == null)
		{
			templates = new ArrayList();
		}
		templates.Add(template);
	}

	internal void ProcessTemplates()
	{
		if (templates != null)
		{
			templates.Sort(s_TemplateComparer);
		}
	}

	internal TemplateAction FindTemplate(Processor processor, XPathNavigator navigator)
	{
		if (templates == null)
		{
			return null;
		}
		for (int num = templates.Count - 1; num >= 0; num--)
		{
			TemplateAction templateAction = (TemplateAction)templates[num];
			int matchKey = templateAction.MatchKey;
			if (matchKey != -1 && processor.Matches(navigator, matchKey))
			{
				return templateAction;
			}
		}
		return null;
	}
}
