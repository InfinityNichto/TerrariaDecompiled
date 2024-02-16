using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class ApplyTemplatesAction : ContainerAction
{
	private int _selectKey = -1;

	private XmlQualifiedName _mode;

	private static readonly ApplyTemplatesAction s_BuiltInRule = new ApplyTemplatesAction();

	internal static ApplyTemplatesAction BuiltInRule()
	{
		return s_BuiltInRule;
	}

	internal static ApplyTemplatesAction BuiltInRule(XmlQualifiedName mode)
	{
		if (!(mode == null) && !mode.IsEmpty)
		{
			return new ApplyTemplatesAction(mode);
		}
		return BuiltInRule();
	}

	internal ApplyTemplatesAction()
	{
	}

	private ApplyTemplatesAction(XmlQualifiedName mode)
	{
		_mode = mode;
	}

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		CompileContent(compiler);
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Select))
		{
			_selectKey = compiler.AddQuery(value);
		}
		else
		{
			if (!Ref.Equal(localName, compiler.Atoms.Mode))
			{
				return false;
			}
			if (compiler.AllowBuiltInMode && value == "*")
			{
				_mode = Compiler.BuiltInMode;
			}
			else
			{
				_mode = compiler.CreateXPathQName(value);
			}
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
				if (Ref.Equal(namespaceURI, input.Atoms.UriXsl))
				{
					if (Ref.Equal(localName, input.Atoms.Sort))
					{
						AddAction(compiler.CreateSortAction());
					}
					else
					{
						if (!Ref.Equal(localName, input.Atoms.WithParam))
						{
							throw compiler.UnexpectedKeyword();
						}
						WithParamAction withParamAction = compiler.CreateWithParamAction();
						CheckDuplicateParams(withParamAction.Name);
						AddAction(withParamAction);
					}
					compiler.PopScope();
					break;
				}
				throw compiler.UnexpectedKeyword();
			}
			default:
				throw XsltException.Create(System.SR.Xslt_InvalidContents, "apply-templates");
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
		default:
			return;
		case 0:
			processor.ResetParams();
			processor.InitSortArray();
			if (containedActions != null && containedActions.Count > 0)
			{
				processor.PushActionFrame(frame);
				frame.State = 2;
				return;
			}
			goto case 2;
		case 2:
			if (_selectKey == -1)
			{
				if (!frame.Node.HasChildren)
				{
					frame.Finished();
					return;
				}
				frame.InitNewNodeSet(frame.Node.SelectChildren(XPathNodeType.All));
			}
			else
			{
				frame.InitNewNodeSet(processor.StartQuery(frame.NodeSet, _selectKey));
			}
			if (processor.SortArray.Count != 0)
			{
				frame.SortNewNodeSet(processor, processor.SortArray);
			}
			frame.State = 3;
			goto case 3;
		case 3:
			if (frame.NewNextNode(processor))
			{
				frame.State = 4;
				break;
			}
			frame.Finished();
			return;
		case 4:
			break;
		case 5:
			frame.State = 3;
			goto case 3;
		case 1:
			return;
		}
		processor.PushTemplateLookup(frame.NewNodeSet, _mode, null);
		frame.State = 5;
	}
}
