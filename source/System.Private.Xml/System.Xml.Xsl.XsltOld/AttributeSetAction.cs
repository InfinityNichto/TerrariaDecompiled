using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class AttributeSetAction : ContainerAction
{
	internal XmlQualifiedName name;

	internal XmlQualifiedName Name => name;

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		CheckRequiredAttribute(compiler, name, "name");
		CompileContent(compiler);
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Name))
		{
			name = compiler.CreateXPathQName(value);
		}
		else
		{
			if (!Ref.Equal(localName, compiler.Atoms.UseAttributeSets))
			{
				return false;
			}
			AddAction(compiler.CreateUseAttributeSetsAction());
		}
		return true;
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
				if (Ref.Equal(namespaceURI, input.Atoms.UriXsl) && Ref.Equal(localName, input.Atoms.Attribute))
				{
					AddAction(compiler.CreateAttributeAction());
					compiler.PopScope();
					break;
				}
				throw compiler.UnexpectedKeyword();
			}
			default:
				throw XsltException.Create(System.SR.Xslt_InvalidContents, "attribute-set");
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

	internal void Merge(AttributeSetAction attributeAction)
	{
		int num = 0;
		Action action;
		while ((action = attributeAction.GetAction(num)) != null)
		{
			AddAction(action);
			num++;
		}
	}
}
