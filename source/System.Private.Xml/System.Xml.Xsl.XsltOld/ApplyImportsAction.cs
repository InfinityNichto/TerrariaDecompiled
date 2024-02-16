namespace System.Xml.Xsl.XsltOld;

internal class ApplyImportsAction : CompiledAction
{
	private XmlQualifiedName _mode;

	private Stylesheet _stylesheet;

	internal override void Compile(Compiler compiler)
	{
		CheckEmpty(compiler);
		if (!compiler.CanHaveApplyImports)
		{
			throw XsltException.Create(System.SR.Xslt_ApplyImports);
		}
		_mode = compiler.CurrentMode;
		_stylesheet = compiler.CompiledStylesheet;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
			processor.PushTemplateLookup(frame.NodeSet, _mode, _stylesheet);
			frame.State = 2;
			break;
		case 2:
			frame.Finished();
			break;
		}
	}
}
