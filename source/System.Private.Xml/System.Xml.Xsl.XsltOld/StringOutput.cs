using System.Text;

namespace System.Xml.Xsl.XsltOld;

internal sealed class StringOutput : SequentialOutput
{
	private readonly StringBuilder _builder;

	private string _result;

	internal string Result => _result;

	internal StringOutput(Processor processor)
		: base(processor)
	{
		_builder = new StringBuilder();
	}

	internal override void Write(char outputChar)
	{
		_builder.Append(outputChar);
	}

	internal override void Write(string outputText)
	{
		_builder.Append(outputText);
	}

	internal override void Close()
	{
		_result = _builder.ToString();
	}
}
