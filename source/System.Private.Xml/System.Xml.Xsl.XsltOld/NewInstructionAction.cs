using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class NewInstructionAction : ContainerAction
{
	private string _name;

	private string _parent;

	private bool _fallback;

	internal override void Compile(Compiler compiler)
	{
		XPathNavigator xPathNavigator = compiler.Input.Navigator.Clone();
		_name = xPathNavigator.Name;
		xPathNavigator.MoveToParent();
		_parent = xPathNavigator.Name;
		if (compiler.Recurse())
		{
			CompileSelectiveTemplate(compiler);
			compiler.ToParent();
		}
	}

	internal void CompileSelectiveTemplate(Compiler compiler)
	{
		NavigatorInput input = compiler.Input;
		do
		{
			if (Ref.Equal(input.NamespaceURI, input.Atoms.UriXsl) && Ref.Equal(input.LocalName, input.Atoms.Fallback))
			{
				_fallback = true;
				if (compiler.Recurse())
				{
					CompileTemplate(compiler);
					compiler.ToParent();
				}
			}
		}
		while (compiler.Advance());
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		default:
			return;
		case 0:
			if (!_fallback)
			{
				throw XsltException.Create(System.SR.Xslt_UnknownExtensionElement, _name);
			}
			if (containedActions != null && containedActions.Count > 0)
			{
				processor.PushActionFrame(frame);
				frame.State = 1;
				return;
			}
			break;
		case 1:
			break;
		}
		frame.Finished();
	}
}
