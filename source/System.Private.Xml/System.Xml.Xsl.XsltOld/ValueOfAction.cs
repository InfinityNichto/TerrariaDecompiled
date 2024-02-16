namespace System.Xml.Xsl.XsltOld;

internal class ValueOfAction : CompiledAction
{
	private int _selectKey = -1;

	private bool _disableOutputEscaping;

	private static readonly Action s_BuiltInRule = new BuiltInRuleTextAction();

	internal static Action BuiltInRule()
	{
		return s_BuiltInRule;
	}

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		CheckRequiredAttribute(compiler, _selectKey != -1, "select");
		CheckEmpty(compiler);
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
			if (!Ref.Equal(localName, compiler.Atoms.DisableOutputEscaping))
			{
				return false;
			}
			_disableOutputEscaping = compiler.GetYesNo(value);
		}
		return true;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
		{
			string text = processor.ValueOf(frame, _selectKey);
			if (processor.TextEvent(text, _disableOutputEscaping))
			{
				frame.Finished();
				break;
			}
			frame.StoredOutput = text;
			frame.State = 2;
			break;
		}
		case 2:
			processor.TextEvent(frame.StoredOutput);
			frame.Finished();
			break;
		}
	}
}
