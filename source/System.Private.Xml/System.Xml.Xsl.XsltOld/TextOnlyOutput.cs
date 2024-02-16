using System.IO;

namespace System.Xml.Xsl.XsltOld;

internal sealed class TextOnlyOutput : IRecordOutput
{
	private readonly Processor _processor;

	private readonly TextWriter _writer;

	internal XsltOutput Output => _processor.Output;

	public TextWriter Writer => _writer;

	internal TextOnlyOutput(Processor processor, Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		_processor = processor;
		_writer = new StreamWriter(stream, Output.Encoding);
	}

	internal TextOnlyOutput(Processor processor, TextWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		_processor = processor;
		_writer = writer;
	}

	public Processor.OutputResult RecordDone(RecordBuilder record)
	{
		BuilderInfo mainNode = record.MainNode;
		XmlNodeType nodeType = mainNode.NodeType;
		if (nodeType == XmlNodeType.Text || (uint)(nodeType - 13) <= 1u)
		{
			_writer.Write(mainNode.Value);
		}
		record.Reset();
		return Processor.OutputResult.Continue;
	}

	public void TheEnd()
	{
		_writer.Flush();
	}
}
