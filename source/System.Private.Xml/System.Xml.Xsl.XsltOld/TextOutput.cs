using System.IO;

namespace System.Xml.Xsl.XsltOld;

internal sealed class TextOutput : SequentialOutput
{
	private TextWriter _writer;

	internal TextOutput(Processor processor, Stream stream)
		: base(processor)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		encoding = processor.Output.Encoding;
		_writer = new StreamWriter(stream, encoding);
	}

	internal TextOutput(Processor processor, TextWriter writer)
		: base(processor)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		encoding = writer.Encoding;
		_writer = writer;
	}

	internal override void Write(char outputChar)
	{
		_writer.Write(outputChar);
	}

	internal override void Write(string outputText)
	{
		_writer.Write(outputText);
	}

	internal override void Close()
	{
		_writer.Flush();
		_writer = null;
	}
}
