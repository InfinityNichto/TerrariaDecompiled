namespace System.Xml.Xsl.XsltOld;

internal class IfAction : ContainerAction
{
	internal enum ConditionType
	{
		ConditionIf,
		ConditionWhen,
		ConditionOtherwise
	}

	private readonly ConditionType _type;

	private int _testKey = -1;

	internal IfAction(ConditionType type)
	{
		_type = type;
	}

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		if (_type != ConditionType.ConditionOtherwise)
		{
			CheckRequiredAttribute(compiler, _testKey != -1, "test");
		}
		if (compiler.Recurse())
		{
			CompileTemplate(compiler);
			compiler.ToParent();
		}
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Test))
		{
			if (_type == ConditionType.ConditionOtherwise)
			{
				return false;
			}
			_testKey = compiler.AddBooleanQuery(value);
			return true;
		}
		return false;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
			if ((_type == ConditionType.ConditionIf || _type == ConditionType.ConditionWhen) && !processor.EvaluateBoolean(frame, _testKey))
			{
				frame.Finished();
				break;
			}
			processor.PushActionFrame(frame);
			frame.State = 1;
			break;
		case 1:
			if (_type == ConditionType.ConditionWhen || _type == ConditionType.ConditionOtherwise)
			{
				frame.Exit();
			}
			frame.Finished();
			break;
		}
	}
}
