using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class TemplateLookupAction : Action
{
	protected XmlQualifiedName mode;

	protected Stylesheet importsOf;

	internal void Initialize(XmlQualifiedName mode, Stylesheet importsOf)
	{
		this.mode = mode;
		this.importsOf = importsOf;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		Action action = null;
		action = ((!(mode != null)) ? ((importsOf == null) ? processor.Stylesheet.FindTemplate(processor, frame.Node) : importsOf.FindTemplateImports(processor, frame.Node)) : ((importsOf == null) ? processor.Stylesheet.FindTemplate(processor, frame.Node, mode) : importsOf.FindTemplateImports(processor, frame.Node, mode)));
		if (action == null)
		{
			action = BuiltInTemplate(frame.Node);
		}
		if (action != null)
		{
			frame.SetAction(action);
		}
		else
		{
			frame.Finished();
		}
	}

	internal Action BuiltInTemplate(XPathNavigator node)
	{
		Action result = null;
		switch (node.NodeType)
		{
		case XPathNodeType.Root:
		case XPathNodeType.Element:
			result = ApplyTemplatesAction.BuiltInRule(mode);
			break;
		case XPathNodeType.Attribute:
		case XPathNodeType.Text:
		case XPathNodeType.SignificantWhitespace:
		case XPathNodeType.Whitespace:
			result = ValueOfAction.BuiltInRule();
			break;
		}
		return result;
	}
}
