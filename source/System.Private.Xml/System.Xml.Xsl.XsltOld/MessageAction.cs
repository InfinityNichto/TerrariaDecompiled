using System.Globalization;
using System.IO;

namespace System.Xml.Xsl.XsltOld;

internal class MessageAction : ContainerAction
{
	private bool _Terminate;

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
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
		if (Ref.Equal(localName, compiler.Atoms.Terminate))
		{
			_Terminate = compiler.GetYesNo(value);
			return true;
		}
		return false;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
		{
			TextOnlyOutput output = new TextOnlyOutput(processor, new StringWriter(CultureInfo.InvariantCulture));
			processor.PushOutput(output);
			processor.PushActionFrame(frame);
			frame.State = 1;
			break;
		}
		case 1:
		{
			TextOnlyOutput textOnlyOutput = processor.PopOutput() as TextOnlyOutput;
			if (_Terminate)
			{
				throw XsltException.Create(System.SR.Xslt_Terminate, textOnlyOutput.Writer.ToString());
			}
			frame.Finished();
			break;
		}
		}
	}
}
