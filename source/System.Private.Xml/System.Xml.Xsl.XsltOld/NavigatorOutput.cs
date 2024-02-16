using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class NavigatorOutput : IRecordOutput
{
	private readonly XPathDocument _doc;

	private int _documentIndex;

	private readonly XmlRawWriter _wr;

	internal XPathNavigator Navigator => ((IXPathNavigable)_doc).CreateNavigator();

	internal NavigatorOutput(string baseUri)
	{
		_doc = new XPathDocument();
		_wr = _doc.LoadFromWriter(XPathDocument.LoadFlags.AtomizeNames, baseUri);
	}

	public Processor.OutputResult RecordDone(RecordBuilder record)
	{
		BuilderInfo mainNode = record.MainNode;
		_documentIndex++;
		switch (mainNode.NodeType)
		{
		case XmlNodeType.Element:
		{
			_wr.WriteStartElement(mainNode.Prefix, mainNode.LocalName, mainNode.NamespaceURI);
			for (int i = 0; i < record.AttributeCount; i++)
			{
				_documentIndex++;
				BuilderInfo builderInfo = (BuilderInfo)record.AttributeList[i];
				if (builderInfo.NamespaceURI == "http://www.w3.org/2000/xmlns/")
				{
					if (builderInfo.Prefix.Length == 0)
					{
						_wr.WriteNamespaceDeclaration(string.Empty, builderInfo.Value);
					}
					else
					{
						_wr.WriteNamespaceDeclaration(builderInfo.LocalName, builderInfo.Value);
					}
				}
				else
				{
					_wr.WriteAttributeString(builderInfo.Prefix, builderInfo.LocalName, builderInfo.NamespaceURI, builderInfo.Value);
				}
			}
			_wr.StartElementContent();
			if (mainNode.IsEmptyTag)
			{
				_wr.WriteEndElement(mainNode.Prefix, mainNode.LocalName, mainNode.NamespaceURI);
			}
			break;
		}
		case XmlNodeType.Text:
			_wr.WriteString(mainNode.Value);
			break;
		case XmlNodeType.SignificantWhitespace:
			_wr.WriteString(mainNode.Value);
			break;
		case XmlNodeType.ProcessingInstruction:
			_wr.WriteProcessingInstruction(mainNode.LocalName, mainNode.Value);
			break;
		case XmlNodeType.Comment:
			_wr.WriteComment(mainNode.Value);
			break;
		case XmlNodeType.EndElement:
			_wr.WriteEndElement(mainNode.Prefix, mainNode.LocalName, mainNode.NamespaceURI);
			break;
		}
		record.Reset();
		return Processor.OutputResult.Continue;
	}

	public void TheEnd()
	{
		_wr.Close();
	}
}
