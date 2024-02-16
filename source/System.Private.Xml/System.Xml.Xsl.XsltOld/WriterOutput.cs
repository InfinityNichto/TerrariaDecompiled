using System.Collections;

namespace System.Xml.Xsl.XsltOld;

internal sealed class WriterOutput : IRecordOutput
{
	private XmlWriter _writer;

	private readonly Processor _processor;

	internal WriterOutput(Processor processor, XmlWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		_writer = writer;
		_processor = processor;
	}

	public Processor.OutputResult RecordDone(RecordBuilder record)
	{
		BuilderInfo mainNode = record.MainNode;
		switch (mainNode.NodeType)
		{
		case XmlNodeType.Element:
			_writer.WriteStartElement(mainNode.Prefix, mainNode.LocalName, mainNode.NamespaceURI);
			WriteAttributes(record.AttributeList, record.AttributeCount);
			if (mainNode.IsEmptyTag)
			{
				_writer.WriteEndElement();
			}
			break;
		case XmlNodeType.Text:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			_writer.WriteString(mainNode.Value);
			break;
		case XmlNodeType.CDATA:
			_writer.WriteCData(mainNode.Value);
			break;
		case XmlNodeType.EntityReference:
			_writer.WriteEntityRef(mainNode.LocalName);
			break;
		case XmlNodeType.ProcessingInstruction:
			_writer.WriteProcessingInstruction(mainNode.LocalName, mainNode.Value);
			break;
		case XmlNodeType.Comment:
			_writer.WriteComment(mainNode.Value);
			break;
		case XmlNodeType.DocumentType:
			_writer.WriteRaw(mainNode.Value);
			break;
		case XmlNodeType.EndElement:
			_writer.WriteFullEndElement();
			break;
		}
		record.Reset();
		return Processor.OutputResult.Continue;
	}

	public void TheEnd()
	{
		_writer.Flush();
		_writer = null;
	}

	private void WriteAttributes(ArrayList list, int count)
	{
		for (int i = 0; i < count; i++)
		{
			BuilderInfo builderInfo = (BuilderInfo)list[i];
			_writer.WriteAttributeString(builderInfo.Prefix, builderInfo.LocalName, builderInfo.NamespaceURI, builderInfo.Value);
		}
	}
}
