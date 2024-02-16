namespace System.Xml.Xsl.XsltOld;

internal class WithParamAction : VariableAction
{
	internal WithParamAction()
		: base(VariableType.WithParameter)
	{
	}

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		CheckRequiredAttribute(compiler, name, "name");
		if (compiler.Recurse())
		{
			CompileTemplate(compiler);
			compiler.ToParent();
			if (selectKey != -1 && containedActions != null)
			{
				throw XsltException.Create(System.SR.Xslt_VariableCntSel2, nameStr);
			}
		}
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
			if (selectKey != -1)
			{
				object value = processor.RunQuery(frame, selectKey);
				processor.SetParameter(name, value);
				frame.Finished();
			}
			else if (containedActions == null)
			{
				processor.SetParameter(name, string.Empty);
				frame.Finished();
			}
			else
			{
				NavigatorOutput output = new NavigatorOutput(baseUri);
				processor.PushOutput(output);
				processor.PushActionFrame(frame);
				frame.State = 1;
			}
			break;
		case 1:
		{
			IRecordOutput recordOutput = processor.PopOutput();
			processor.SetParameter(name, ((NavigatorOutput)recordOutput).Navigator);
			frame.Finished();
			break;
		}
		}
	}
}
