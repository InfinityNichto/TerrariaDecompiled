using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class ChooseAction : ContainerAction
{
	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		if (compiler.Recurse())
		{
			CompileConditions(compiler);
			compiler.ToParent();
		}
	}

	private void CompileConditions(Compiler compiler)
	{
		NavigatorInput input = compiler.Input;
		bool flag = false;
		bool flag2 = false;
		do
		{
			switch (input.NodeType)
			{
			case XPathNodeType.Element:
			{
				compiler.PushNamespaceScope();
				string namespaceURI = input.NamespaceURI;
				string localName = input.LocalName;
				if (Ref.Equal(namespaceURI, input.Atoms.UriXsl))
				{
					IfAction ifAction = null;
					if (Ref.Equal(localName, input.Atoms.When))
					{
						if (flag2)
						{
							throw XsltException.Create(System.SR.Xslt_WhenAfterOtherwise);
						}
						ifAction = compiler.CreateIfAction(IfAction.ConditionType.ConditionWhen);
						flag = true;
					}
					else
					{
						if (!Ref.Equal(localName, input.Atoms.Otherwise))
						{
							throw compiler.UnexpectedKeyword();
						}
						if (flag2)
						{
							throw XsltException.Create(System.SR.Xslt_DupOtherwise);
						}
						ifAction = compiler.CreateIfAction(IfAction.ConditionType.ConditionOtherwise);
						flag2 = true;
					}
					AddAction(ifAction);
					compiler.PopScope();
					break;
				}
				throw compiler.UnexpectedKeyword();
			}
			default:
				throw XsltException.Create(System.SR.Xslt_InvalidContents, "choose");
			case XPathNodeType.SignificantWhitespace:
			case XPathNodeType.Whitespace:
			case XPathNodeType.ProcessingInstruction:
			case XPathNodeType.Comment:
				break;
			}
		}
		while (compiler.Advance());
		if (!flag)
		{
			throw XsltException.Create(System.SR.Xslt_NoWhen);
		}
	}
}
