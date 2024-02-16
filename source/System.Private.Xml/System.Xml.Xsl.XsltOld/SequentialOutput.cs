using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Xml.Xsl.XsltOld;

internal abstract class SequentialOutput : IRecordOutput
{
	private static readonly char[] s_TextValueFind = new char[3] { '&', '>', '<' };

	private static readonly string[] s_TextValueReplace = new string[3] { "&amp;", "&gt;", "&lt;" };

	private static readonly char[] s_XmlAttributeValueFind = new char[6] { '&', '>', '<', '"', '\n', '\r' };

	private static readonly string[] s_XmlAttributeValueReplace = new string[6] { "&amp;", "&gt;", "&lt;", "&quot;", "&#xA;", "&#xD;" };

	private readonly Processor _processor;

	protected Encoding encoding;

	private ArrayList _outputCache;

	private bool _firstLine = true;

	private bool _secondRoot;

	private XsltOutput _output;

	private bool _isHtmlOutput;

	private bool _isXmlOutput;

	private Hashtable _cdataElements;

	private bool _indentOutput;

	private bool _outputDoctype;

	private bool _outputXmlDecl;

	private bool _omitXmlDeclCalled;

	private byte[] _byteBuffer;

	private Encoding _utf8Encoding;

	[MemberNotNull("_output")]
	private void CacheOuptutProps(XsltOutput output)
	{
		_output = output;
		_isXmlOutput = _output.Method == XsltOutput.OutputMethod.Xml;
		_isHtmlOutput = _output.Method == XsltOutput.OutputMethod.Html;
		_cdataElements = _output.CDataElements;
		_indentOutput = _output.Indent;
		_outputDoctype = _output.DoctypeSystem != null || (_isHtmlOutput && _output.DoctypePublic != null);
		_outputXmlDecl = _isXmlOutput && !_output.OmitXmlDeclaration && !_omitXmlDeclCalled;
	}

	internal SequentialOutput(Processor processor)
	{
		_processor = processor;
		CacheOuptutProps(processor.Output);
	}

	public void OmitXmlDecl()
	{
		_omitXmlDeclCalled = true;
		_outputXmlDecl = false;
	}

	private void WriteStartElement(RecordBuilder record)
	{
		BuilderInfo mainNode = record.MainNode;
		HtmlElementProps htmlElementProps = null;
		if (_isHtmlOutput)
		{
			if (mainNode.Prefix.Length == 0)
			{
				htmlElementProps = mainNode.htmlProps;
				if (htmlElementProps == null && mainNode.search)
				{
					htmlElementProps = HtmlElementProps.GetProps(mainNode.LocalName);
				}
				record.Manager.CurrentElementScope.HtmlElementProps = htmlElementProps;
				mainNode.IsEmptyTag = false;
			}
		}
		else if (_isXmlOutput && mainNode.Depth == 0)
		{
			if (_secondRoot && (_output.DoctypeSystem != null || _output.Standalone))
			{
				throw XsltException.Create(System.SR.Xslt_MultipleRoots);
			}
			_secondRoot = true;
		}
		if (_outputDoctype)
		{
			WriteDoctype(mainNode);
			_outputDoctype = false;
		}
		if (_cdataElements != null && _cdataElements.Contains(new XmlQualifiedName(mainNode.LocalName, mainNode.NamespaceURI)) && _isXmlOutput)
		{
			record.Manager.CurrentElementScope.ToCData = true;
		}
		Indent(record);
		Write('<');
		WriteName(mainNode.Prefix, mainNode.LocalName);
		WriteAttributes(record.AttributeList, record.AttributeCount, htmlElementProps);
		if (mainNode.IsEmptyTag)
		{
			Write(" />");
		}
		else
		{
			Write('>');
		}
		if (htmlElementProps != null && htmlElementProps.Head)
		{
			mainNode.Depth++;
			Indent(record);
			mainNode.Depth--;
			Write("<META http-equiv=\"Content-Type\" content=\"");
			Write(_output.MediaType);
			Write("; charset=");
			Write(encoding.WebName);
			Write("\">");
		}
	}

	private void WriteTextNode(RecordBuilder record)
	{
		BuilderInfo mainNode = record.MainNode;
		OutputScope currentElementScope = record.Manager.CurrentElementScope;
		currentElementScope.Mixed = true;
		if (currentElementScope.HtmlElementProps != null && currentElementScope.HtmlElementProps.NoEntities)
		{
			Write(mainNode.Value);
		}
		else if (currentElementScope.ToCData)
		{
			WriteCDataSection(mainNode.Value);
		}
		else
		{
			WriteTextNode(mainNode);
		}
	}

	private void WriteTextNode(BuilderInfo node)
	{
		for (int i = 0; i < node.TextInfoCount; i++)
		{
			string text = node.TextInfo[i];
			if (text == null)
			{
				i++;
				Write(node.TextInfo[i]);
			}
			else
			{
				WriteWithReplace(text, s_TextValueFind, s_TextValueReplace);
			}
		}
	}

	private void WriteCDataSection(string value)
	{
		Write("<![CDATA[");
		WriteCData(value);
		Write("]]>");
	}

	private void WriteDoctype(BuilderInfo mainNode)
	{
		Indent(0);
		Write("<!DOCTYPE ");
		if (_isXmlOutput)
		{
			WriteName(mainNode.Prefix, mainNode.LocalName);
		}
		else
		{
			WriteName(string.Empty, "html");
		}
		Write(' ');
		if (_output.DoctypePublic != null)
		{
			Write("PUBLIC ");
			Write('"');
			Write(_output.DoctypePublic);
			Write("\" ");
		}
		else
		{
			Write("SYSTEM ");
		}
		if (_output.DoctypeSystem != null)
		{
			Write('"');
			Write(_output.DoctypeSystem);
			Write('"');
		}
		Write('>');
	}

	private void WriteXmlDeclaration()
	{
		_outputXmlDecl = false;
		Indent(0);
		Write("<?");
		WriteName(string.Empty, "xml");
		Write(" version=\"1.0\"");
		if (encoding != null)
		{
			Write(" encoding=\"");
			Write(encoding.WebName);
			Write('"');
		}
		if (_output.HasStandalone)
		{
			Write(" standalone=\"");
			Write(_output.Standalone ? "yes" : "no");
			Write('"');
		}
		Write("?>");
	}

	private void WriteProcessingInstruction(RecordBuilder record)
	{
		Indent(record);
		WriteProcessingInstruction(record.MainNode);
	}

	private void WriteProcessingInstruction(BuilderInfo node)
	{
		Write("<?");
		WriteName(node.Prefix, node.LocalName);
		Write(' ');
		Write(node.Value);
		if (_isHtmlOutput)
		{
			Write('>');
		}
		else
		{
			Write("?>");
		}
	}

	private void WriteEndElement(RecordBuilder record)
	{
		HtmlElementProps htmlElementProps = record.Manager.CurrentElementScope.HtmlElementProps;
		if (htmlElementProps == null || !htmlElementProps.Empty)
		{
			Indent(record);
			Write("</");
			WriteName(record.MainNode.Prefix, record.MainNode.LocalName);
			Write('>');
		}
	}

	public Processor.OutputResult RecordDone(RecordBuilder record)
	{
		if (_output.Method == XsltOutput.OutputMethod.Unknown)
		{
			if (!DecideDefaultOutput(record.MainNode))
			{
				CacheRecord(record);
			}
			else
			{
				OutputCachedRecords();
				OutputRecord(record);
			}
		}
		else
		{
			OutputRecord(record);
		}
		record.Reset();
		return Processor.OutputResult.Continue;
	}

	public void TheEnd()
	{
		OutputCachedRecords();
		Close();
	}

	private bool DecideDefaultOutput(BuilderInfo node)
	{
		XsltOutput.OutputMethod defaultOutput = XsltOutput.OutputMethod.Xml;
		switch (node.NodeType)
		{
		case XmlNodeType.Element:
			if (node.NamespaceURI.Length == 0 && string.Equals("html", node.LocalName, StringComparison.OrdinalIgnoreCase))
			{
				defaultOutput = XsltOutput.OutputMethod.Html;
			}
			break;
		case XmlNodeType.Text:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			if (XmlCharType.IsOnlyWhitespace(node.Value))
			{
				return false;
			}
			defaultOutput = XsltOutput.OutputMethod.Xml;
			break;
		default:
			return false;
		}
		if (_processor.SetDefaultOutput(defaultOutput))
		{
			CacheOuptutProps(_processor.Output);
		}
		return true;
	}

	private void CacheRecord(RecordBuilder record)
	{
		if (_outputCache == null)
		{
			_outputCache = new ArrayList();
		}
		_outputCache.Add(record.MainNode.Clone());
	}

	private void OutputCachedRecords()
	{
		if (_outputCache != null)
		{
			for (int i = 0; i < _outputCache.Count; i++)
			{
				BuilderInfo node = (BuilderInfo)_outputCache[i];
				OutputRecord(node);
			}
			_outputCache = null;
		}
	}

	private void OutputRecord(RecordBuilder record)
	{
		BuilderInfo mainNode = record.MainNode;
		if (_outputXmlDecl)
		{
			WriteXmlDeclaration();
		}
		switch (mainNode.NodeType)
		{
		case XmlNodeType.Element:
			WriteStartElement(record);
			break;
		case XmlNodeType.Text:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			WriteTextNode(record);
			break;
		case XmlNodeType.EntityReference:
			Write('&');
			WriteName(mainNode.Prefix, mainNode.LocalName);
			Write(';');
			break;
		case XmlNodeType.ProcessingInstruction:
			WriteProcessingInstruction(record);
			break;
		case XmlNodeType.Comment:
			Indent(record);
			Write("<!--");
			Write(mainNode.Value);
			Write("-->");
			break;
		case XmlNodeType.DocumentType:
			Write(mainNode.Value);
			break;
		case XmlNodeType.EndElement:
			WriteEndElement(record);
			break;
		case XmlNodeType.Attribute:
		case XmlNodeType.CDATA:
		case XmlNodeType.Entity:
		case XmlNodeType.Document:
		case XmlNodeType.DocumentFragment:
		case XmlNodeType.Notation:
			break;
		}
	}

	private void OutputRecord(BuilderInfo node)
	{
		if (_outputXmlDecl)
		{
			WriteXmlDeclaration();
		}
		Indent(0);
		switch (node.NodeType)
		{
		case XmlNodeType.Text:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			WriteTextNode(node);
			break;
		case XmlNodeType.EntityReference:
			Write('&');
			WriteName(node.Prefix, node.LocalName);
			Write(';');
			break;
		case XmlNodeType.ProcessingInstruction:
			WriteProcessingInstruction(node);
			break;
		case XmlNodeType.Comment:
			Write("<!--");
			Write(node.Value);
			Write("-->");
			break;
		case XmlNodeType.DocumentType:
			Write(node.Value);
			break;
		case XmlNodeType.Element:
		case XmlNodeType.Attribute:
		case XmlNodeType.CDATA:
		case XmlNodeType.Entity:
		case XmlNodeType.Document:
		case XmlNodeType.DocumentFragment:
		case XmlNodeType.Notation:
		case XmlNodeType.EndElement:
			break;
		}
	}

	private void WriteName(string prefix, string name)
	{
		if (prefix != null && prefix.Length > 0)
		{
			Write(prefix);
			if (name == null || name.Length <= 0)
			{
				return;
			}
			Write(':');
		}
		Write(name);
	}

	private void WriteXmlAttributeValue(string value)
	{
		WriteWithReplace(value, s_XmlAttributeValueFind, s_XmlAttributeValueReplace);
	}

	private void WriteHtmlAttributeValue(string value)
	{
		int length = value.Length;
		int num = 0;
		while (num < length)
		{
			char c = value[num];
			num++;
			switch (c)
			{
			case '&':
				if (num != length && value[num] == '{')
				{
					Write(c);
				}
				else
				{
					Write("&amp;");
				}
				break;
			case '"':
				Write("&quot;");
				break;
			default:
				Write(c);
				break;
			}
		}
	}

	private void WriteHtmlUri(string value)
	{
		int length = value.Length;
		int num = 0;
		while (num < length)
		{
			char c = value[num];
			num++;
			switch (c)
			{
			case '&':
				if (num != length && value[num] == '{')
				{
					Write(c);
				}
				else
				{
					Write("&amp;");
				}
				continue;
			case '"':
				Write("&quot;");
				continue;
			case '\n':
				Write("&#xA;");
				continue;
			case '\r':
				Write("&#xD;");
				continue;
			}
			if ('\u007f' < c)
			{
				if (_utf8Encoding == null)
				{
					_utf8Encoding = Encoding.UTF8;
					_byteBuffer = new byte[_utf8Encoding.GetMaxByteCount(1)];
				}
				int bytes = _utf8Encoding.GetBytes(value, num - 1, 1, _byteBuffer, 0);
				for (int i = 0; i < bytes; i++)
				{
					Write("%");
					uint num2 = _byteBuffer[i];
					Write(num2.ToString("X2", CultureInfo.InvariantCulture));
				}
			}
			else
			{
				Write(c);
			}
		}
	}

	private void WriteWithReplace(string value, char[] find, string[] replace)
	{
		int length = value.Length;
		int i;
		for (i = 0; i < length; i++)
		{
			int num = value.IndexOfAny(find, i);
			if (num == -1)
			{
				break;
			}
			for (; i < num; i++)
			{
				Write(value[i]);
			}
			char c = value[i];
			int num2 = find.Length - 1;
			while (0 <= num2)
			{
				if (find[num2] == c)
				{
					Write(replace[num2]);
					break;
				}
				num2--;
			}
		}
		if (i == 0)
		{
			Write(value);
			return;
		}
		for (; i < length; i++)
		{
			Write(value[i]);
		}
	}

	private void WriteCData(string value)
	{
		Write(value.Replace("]]>", "]]]]><![CDATA[>"));
	}

	private void WriteAttributes(ArrayList list, int count, HtmlElementProps htmlElementsProps)
	{
		for (int i = 0; i < count; i++)
		{
			BuilderInfo builderInfo = (BuilderInfo)list[i];
			string value = builderInfo.Value;
			bool flag = false;
			bool flag2 = false;
			if (htmlElementsProps != null && builderInfo.Prefix.Length == 0)
			{
				HtmlAttributeProps htmlAttributeProps = builderInfo.htmlAttrProps;
				if (htmlAttributeProps == null && builderInfo.search)
				{
					htmlAttributeProps = HtmlAttributeProps.GetProps(builderInfo.LocalName);
				}
				if (htmlAttributeProps != null)
				{
					flag = htmlElementsProps.AbrParent && htmlAttributeProps.Abr;
					flag2 = htmlElementsProps.UriParent && (htmlAttributeProps.Uri || (htmlElementsProps.NameParent && htmlAttributeProps.Name));
				}
			}
			Write(' ');
			WriteName(builderInfo.Prefix, builderInfo.LocalName);
			if (!flag || !string.Equals(builderInfo.LocalName, value, StringComparison.OrdinalIgnoreCase))
			{
				Write("=\"");
				if (flag2)
				{
					WriteHtmlUri(value);
				}
				else if (_isHtmlOutput)
				{
					WriteHtmlAttributeValue(value);
				}
				else
				{
					WriteXmlAttributeValue(value);
				}
				Write('"');
			}
		}
	}

	private void Indent(RecordBuilder record)
	{
		if (!record.Manager.CurrentElementScope.Mixed)
		{
			Indent(record.MainNode.Depth);
		}
	}

	private void Indent(int depth)
	{
		if (_firstLine)
		{
			if (_indentOutput)
			{
				_firstLine = false;
			}
			return;
		}
		Write("\r\n");
		int num = 2 * depth;
		while (0 < num)
		{
			Write(" ");
			num--;
		}
	}

	internal abstract void Write(char outputChar);

	internal abstract void Write(string outputText);

	internal abstract void Close();
}
