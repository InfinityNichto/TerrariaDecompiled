using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class CallTemplateAction : ContainerAction
{
	private XmlQualifiedName _name;

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		CheckRequiredAttribute(compiler, _name, "name");
		CompileContent(compiler);
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Name))
		{
			_name = compiler.CreateXPathQName(value);
			return true;
		}
		return false;
	}

	private void CompileContent(Compiler compiler)
	{
		NavigatorInput input = compiler.Input;
		if (!compiler.Recurse())
		{
			return;
		}
		do
		{
			switch (input.NodeType)
			{
			case XPathNodeType.Element:
			{
				compiler.PushNamespaceScope();
				string namespaceURI = input.NamespaceURI;
				string localName = input.LocalName;
				if (Ref.Equal(namespaceURI, input.Atoms.UriXsl) && Ref.Equal(localName, input.Atoms.WithParam))
				{
					WithParamAction withParamAction = compiler.CreateWithParamAction();
					CheckDuplicateParams(withParamAction.Name);
					AddAction(withParamAction);
					compiler.PopScope();
					break;
				}
				throw compiler.UnexpectedKeyword();
			}
			default:
				throw XsltException.Create(System.SR.Xslt_InvalidContents, "call-template");
			case XPathNodeType.SignificantWhitespace:
			case XPathNodeType.Whitespace:
			case XPathNodeType.ProcessingInstruction:
			case XPathNodeType.Comment:
				break;
			}
		}
		while (compiler.Advance());
		compiler.ToParent();
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
			processor.ResetParams();
			if (containedActions != null && containedActions.Count > 0)
			{
				processor.PushActionFrame(frame);
				frame.State = 2;
				break;
			}
			goto case 2;
		case 2:
		{
			TemplateAction templateAction = processor.Stylesheet.FindTemplate(_name);
			if (templateAction != null)
			{
				frame.State = 3;
				processor.PushActionFrame(templateAction, frame.NodeSet);
				break;
			}
			throw XsltException.Create(System.SR.Xslt_InvalidCallTemplate, _name.ToString());
		}
		case 3:
			frame.Finished();
			break;
		case 1:
			break;
		}
	}
}
