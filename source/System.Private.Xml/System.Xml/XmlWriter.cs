using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml;

public abstract class XmlWriter : IDisposable, IAsyncDisposable
{
	private char[] _writeNodeBuffer;

	public virtual XmlWriterSettings? Settings => null;

	public abstract WriteState WriteState { get; }

	public virtual XmlSpace XmlSpace => XmlSpace.Default;

	public virtual string? XmlLang => string.Empty;

	public abstract void WriteStartDocument();

	public abstract void WriteStartDocument(bool standalone);

	public abstract void WriteEndDocument();

	public abstract void WriteDocType(string name, string? pubid, string? sysid, string? subset);

	public void WriteStartElement(string localName, string? ns)
	{
		WriteStartElement(null, localName, ns);
	}

	public abstract void WriteStartElement(string? prefix, string localName, string? ns);

	public void WriteStartElement(string localName)
	{
		WriteStartElement(null, localName, null);
	}

	public abstract void WriteEndElement();

	public abstract void WriteFullEndElement();

	public void WriteAttributeString(string localName, string? ns, string? value)
	{
		WriteStartAttribute(null, localName, ns);
		WriteString(value);
		WriteEndAttribute();
	}

	public void WriteAttributeString(string localName, string? value)
	{
		WriteStartAttribute(null, localName, null);
		WriteString(value);
		WriteEndAttribute();
	}

	public void WriteAttributeString(string? prefix, string localName, string? ns, string? value)
	{
		WriteStartAttribute(prefix, localName, ns);
		WriteString(value);
		WriteEndAttribute();
	}

	public void WriteStartAttribute(string localName, string? ns)
	{
		WriteStartAttribute(null, localName, ns);
	}

	public abstract void WriteStartAttribute(string? prefix, string localName, string? ns);

	public void WriteStartAttribute(string localName)
	{
		WriteStartAttribute(null, localName, null);
	}

	public abstract void WriteEndAttribute();

	public abstract void WriteCData(string? text);

	public abstract void WriteComment(string? text);

	public abstract void WriteProcessingInstruction(string name, string? text);

	public abstract void WriteEntityRef(string name);

	public abstract void WriteCharEntity(char ch);

	public abstract void WriteWhitespace(string? ws);

	public abstract void WriteString(string? text);

	public abstract void WriteSurrogateCharEntity(char lowChar, char highChar);

	public abstract void WriteChars(char[] buffer, int index, int count);

	public abstract void WriteRaw(char[] buffer, int index, int count);

	public abstract void WriteRaw(string data);

	public abstract void WriteBase64(byte[] buffer, int index, int count);

	public virtual void WriteBinHex(byte[] buffer, int index, int count)
	{
		BinHexEncoder.Encode(buffer, index, count, this);
	}

	public virtual void Close()
	{
	}

	public abstract void Flush();

	public abstract string? LookupPrefix(string ns);

	public virtual void WriteNmToken(string name)
	{
		if (name == null || name.Length == 0)
		{
			throw new ArgumentException(System.SR.Xml_EmptyName);
		}
		WriteString(XmlConvert.VerifyNMTOKEN(name, ExceptionType.ArgumentException));
	}

	public virtual void WriteName(string name)
	{
		WriteString(XmlConvert.VerifyQName(name, ExceptionType.ArgumentException));
	}

	public virtual void WriteQualifiedName(string localName, string? ns)
	{
		if (ns != null && ns.Length > 0)
		{
			string text = LookupPrefix(ns);
			if (text == null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Xml_UndefNamespace, ns));
			}
			WriteString(text);
			WriteString(":");
		}
		WriteString(localName);
	}

	public virtual void WriteValue(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		WriteString(XmlUntypedConverter.Untyped.ToString(value, null));
	}

	public virtual void WriteValue(string? value)
	{
		if (value != null)
		{
			WriteString(value);
		}
	}

	public virtual void WriteValue(bool value)
	{
		WriteString(XmlConvert.ToString(value));
	}

	public virtual void WriteValue(DateTime value)
	{
		WriteString(XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind));
	}

	public virtual void WriteValue(DateTimeOffset value)
	{
		if (value.Offset != TimeSpan.Zero)
		{
			WriteValue(value.LocalDateTime);
		}
		else
		{
			WriteValue(value.UtcDateTime);
		}
	}

	public virtual void WriteValue(double value)
	{
		WriteString(XmlConvert.ToString(value));
	}

	public virtual void WriteValue(float value)
	{
		WriteString(XmlConvert.ToString(value));
	}

	public virtual void WriteValue(decimal value)
	{
		WriteString(XmlConvert.ToString(value));
	}

	public virtual void WriteValue(int value)
	{
		WriteString(XmlConvert.ToString(value));
	}

	public virtual void WriteValue(long value)
	{
		WriteString(XmlConvert.ToString(value));
	}

	public virtual void WriteAttributes(XmlReader reader, bool defattr)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (reader.NodeType == XmlNodeType.Element || reader.NodeType == XmlNodeType.XmlDeclaration)
		{
			if (reader.MoveToFirstAttribute())
			{
				WriteAttributes(reader, defattr);
				reader.MoveToElement();
			}
			return;
		}
		if (reader.NodeType != XmlNodeType.Attribute)
		{
			throw new XmlException(System.SR.Xml_InvalidPosition, string.Empty);
		}
		do
		{
			if (!defattr && reader.IsDefaultInternal)
			{
				continue;
			}
			WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
			while (reader.ReadAttributeValue())
			{
				if (reader.NodeType == XmlNodeType.EntityReference)
				{
					WriteEntityRef(reader.Name);
				}
				else
				{
					WriteString(reader.Value);
				}
			}
			WriteEndAttribute();
		}
		while (reader.MoveToNextAttribute());
	}

	public virtual void WriteNode(XmlReader reader, bool defattr)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		bool canReadValueChunk = reader.CanReadValueChunk;
		int num = ((reader.NodeType == XmlNodeType.None) ? (-1) : reader.Depth);
		do
		{
			switch (reader.NodeType)
			{
			case XmlNodeType.Element:
				WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
				WriteAttributes(reader, defattr);
				if (reader.IsEmptyElement)
				{
					WriteEndElement();
				}
				break;
			case XmlNodeType.Text:
				if (canReadValueChunk)
				{
					if (_writeNodeBuffer == null)
					{
						_writeNodeBuffer = new char[1024];
					}
					int count;
					while ((count = reader.ReadValueChunk(_writeNodeBuffer, 0, 1024)) > 0)
					{
						WriteChars(_writeNodeBuffer, 0, count);
					}
				}
				else
				{
					WriteString(reader.Value);
				}
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				WriteWhitespace(reader.Value);
				break;
			case XmlNodeType.CDATA:
				WriteCData(reader.Value);
				break;
			case XmlNodeType.EntityReference:
				WriteEntityRef(reader.Name);
				break;
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.XmlDeclaration:
				WriteProcessingInstruction(reader.Name, reader.Value);
				break;
			case XmlNodeType.DocumentType:
				WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
				break;
			case XmlNodeType.Comment:
				WriteComment(reader.Value);
				break;
			case XmlNodeType.EndElement:
				WriteFullEndElement();
				break;
			}
		}
		while (reader.Read() && (num < reader.Depth || (num == reader.Depth && reader.NodeType == XmlNodeType.EndElement)));
	}

	public virtual void WriteNode(XPathNavigator navigator, bool defattr)
	{
		if (navigator == null)
		{
			throw new ArgumentNullException("navigator");
		}
		int num = 0;
		navigator = navigator.Clone();
		while (true)
		{
			bool flag = false;
			switch (navigator.NodeType)
			{
			case XPathNodeType.Element:
				WriteStartElement(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);
				if (navigator.MoveToFirstAttribute())
				{
					do
					{
						IXmlSchemaInfo schemaInfo = navigator.SchemaInfo;
						if (defattr || schemaInfo == null || !schemaInfo.IsDefault)
						{
							WriteStartAttribute(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);
							WriteString(navigator.Value);
							WriteEndAttribute();
						}
					}
					while (navigator.MoveToNextAttribute());
					navigator.MoveToParent();
				}
				if (navigator.MoveToFirstNamespace(XPathNamespaceScope.Local))
				{
					WriteLocalNamespaces(navigator);
					navigator.MoveToParent();
				}
				flag = true;
				break;
			case XPathNodeType.Text:
				WriteString(navigator.Value);
				break;
			case XPathNodeType.SignificantWhitespace:
			case XPathNodeType.Whitespace:
				WriteWhitespace(navigator.Value);
				break;
			case XPathNodeType.Root:
				flag = true;
				break;
			case XPathNodeType.Comment:
				WriteComment(navigator.Value);
				break;
			case XPathNodeType.ProcessingInstruction:
				WriteProcessingInstruction(navigator.LocalName, navigator.Value);
				break;
			}
			if (flag)
			{
				if (navigator.MoveToFirstChild())
				{
					num++;
					continue;
				}
				if (navigator.NodeType == XPathNodeType.Element)
				{
					if (navigator.IsEmptyElement)
					{
						WriteEndElement();
					}
					else
					{
						WriteFullEndElement();
					}
				}
			}
			while (true)
			{
				if (num == 0)
				{
					return;
				}
				if (navigator.MoveToNext())
				{
					break;
				}
				num--;
				navigator.MoveToParent();
				if (navigator.NodeType == XPathNodeType.Element)
				{
					WriteFullEndElement();
				}
			}
		}
	}

	public void WriteElementString(string localName, string? value)
	{
		WriteElementString(localName, null, value);
	}

	public void WriteElementString(string localName, string? ns, string? value)
	{
		WriteStartElement(localName, ns);
		if (value != null && value.Length != 0)
		{
			WriteString(value);
		}
		WriteEndElement();
	}

	public void WriteElementString(string? prefix, string localName, string? ns, string? value)
	{
		WriteStartElement(prefix, localName, ns);
		if (value != null && value.Length != 0)
		{
			WriteString(value);
		}
		WriteEndElement();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && WriteState != WriteState.Closed)
		{
			Close();
		}
	}

	private void WriteLocalNamespaces(XPathNavigator nsNav)
	{
		string localName = nsNav.LocalName;
		string value = nsNav.Value;
		if (nsNav.MoveToNextNamespace(XPathNamespaceScope.Local))
		{
			WriteLocalNamespaces(nsNav);
		}
		if (localName.Length == 0)
		{
			WriteAttributeString(string.Empty, "xmlns", "http://www.w3.org/2000/xmlns/", value);
		}
		else
		{
			WriteAttributeString("xmlns", localName, "http://www.w3.org/2000/xmlns/", value);
		}
	}

	public static XmlWriter Create(string outputFileName)
	{
		if (outputFileName == null)
		{
			throw new ArgumentNullException("outputFileName");
		}
		FileStream fileStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
		try
		{
			XmlWriterSettings settings = new XmlWriterSettings
			{
				CloseOutput = true
			};
			XmlWriter writer = new XmlEncodedRawTextWriter(fileStream, settings);
			return new XmlWellFormedWriter(writer, settings);
		}
		catch
		{
			fileStream.Dispose();
			throw;
		}
	}

	public static XmlWriter Create(string outputFileName, XmlWriterSettings? settings)
	{
		if (settings == null)
		{
			settings = XmlWriterSettings.s_defaultWriterSettings;
		}
		return settings.CreateWriter(outputFileName);
	}

	public static XmlWriter Create(Stream output)
	{
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		XmlWriterSettings s_defaultWriterSettings = XmlWriterSettings.s_defaultWriterSettings;
		XmlWriter writer = new XmlUtf8RawTextWriter(output, s_defaultWriterSettings);
		return new XmlWellFormedWriter(writer, s_defaultWriterSettings);
	}

	public static XmlWriter Create(Stream output, XmlWriterSettings? settings)
	{
		if (settings == null)
		{
			settings = XmlWriterSettings.s_defaultWriterSettings;
		}
		return settings.CreateWriter(output);
	}

	public static XmlWriter Create(TextWriter output)
	{
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		XmlWriterSettings s_defaultWriterSettings = XmlWriterSettings.s_defaultWriterSettings;
		XmlWriter writer = new XmlEncodedRawTextWriter(output, s_defaultWriterSettings);
		return new XmlWellFormedWriter(writer, s_defaultWriterSettings);
	}

	public static XmlWriter Create(TextWriter output, XmlWriterSettings? settings)
	{
		if (settings == null)
		{
			settings = XmlWriterSettings.s_defaultWriterSettings;
		}
		return settings.CreateWriter(output);
	}

	public static XmlWriter Create(StringBuilder output)
	{
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		return Create(new StringWriter(output, CultureInfo.InvariantCulture));
	}

	public static XmlWriter Create(StringBuilder output, XmlWriterSettings? settings)
	{
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		if (settings == null)
		{
			settings = XmlWriterSettings.s_defaultWriterSettings;
		}
		return settings.CreateWriter(new StringWriter(output, CultureInfo.InvariantCulture));
	}

	public static XmlWriter Create(XmlWriter output)
	{
		return Create(output, null);
	}

	public static XmlWriter Create(XmlWriter output, XmlWriterSettings? settings)
	{
		if (settings == null)
		{
			settings = XmlWriterSettings.s_defaultWriterSettings;
		}
		return settings.CreateWriter(output);
	}

	public virtual Task WriteStartDocumentAsync()
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteStartDocumentAsync(bool standalone)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteEndDocumentAsync()
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteStartElementAsync(string? prefix, string localName, string? ns)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteEndElementAsync()
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteFullEndElementAsync()
	{
		throw new NotImplementedException();
	}

	public Task WriteAttributeStringAsync(string? prefix, string localName, string? ns, string? value)
	{
		Task task = WriteStartAttributeAsync(prefix, localName, ns);
		if (task.IsSuccess())
		{
			return WriteStringAsync(value).CallTaskFuncWhenFinishAsync((XmlWriter thisRef) => thisRef.WriteEndAttributeAsync(), this);
		}
		return WriteAttributeStringAsyncHelper(task, value);
	}

	private async Task WriteAttributeStringAsyncHelper(Task task, string value)
	{
		await task.ConfigureAwait(continueOnCapturedContext: false);
		await WriteStringAsync(value).ConfigureAwait(continueOnCapturedContext: false);
		await WriteEndAttributeAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	protected internal virtual Task WriteStartAttributeAsync(string? prefix, string localName, string? ns)
	{
		throw new NotImplementedException();
	}

	protected internal virtual Task WriteEndAttributeAsync()
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteCDataAsync(string? text)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteCommentAsync(string? text)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteProcessingInstructionAsync(string name, string? text)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteEntityRefAsync(string name)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteCharEntityAsync(char ch)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteWhitespaceAsync(string? ws)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteStringAsync(string? text)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteCharsAsync(char[] buffer, int index, int count)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteRawAsync(char[] buffer, int index, int count)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteRawAsync(string data)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteBase64Async(byte[] buffer, int index, int count)
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteBinHexAsync(byte[] buffer, int index, int count)
	{
		return BinHexEncoder.EncodeAsync(buffer, index, count, this);
	}

	public virtual Task FlushAsync()
	{
		throw new NotImplementedException();
	}

	public virtual Task WriteNmTokenAsync(string name)
	{
		if (name == null || name.Length == 0)
		{
			throw new ArgumentException(System.SR.Xml_EmptyName);
		}
		return WriteStringAsync(XmlConvert.VerifyNMTOKEN(name, ExceptionType.ArgumentException));
	}

	public virtual Task WriteNameAsync(string name)
	{
		return WriteStringAsync(XmlConvert.VerifyQName(name, ExceptionType.ArgumentException));
	}

	public virtual async Task WriteQualifiedNameAsync(string localName, string? ns)
	{
		if (ns != null && ns.Length > 0)
		{
			string text = LookupPrefix(ns);
			if (text == null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Xml_UndefNamespace, ns));
			}
			await WriteStringAsync(text).ConfigureAwait(continueOnCapturedContext: false);
			await WriteStringAsync(":").ConfigureAwait(continueOnCapturedContext: false);
		}
		await WriteStringAsync(localName).ConfigureAwait(continueOnCapturedContext: false);
	}

	public virtual async Task WriteAttributesAsync(XmlReader reader, bool defattr)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (reader.NodeType == XmlNodeType.Element || reader.NodeType == XmlNodeType.XmlDeclaration)
		{
			if (reader.MoveToFirstAttribute())
			{
				await WriteAttributesAsync(reader, defattr).ConfigureAwait(continueOnCapturedContext: false);
				reader.MoveToElement();
			}
			return;
		}
		if (reader.NodeType != XmlNodeType.Attribute)
		{
			throw new XmlException(System.SR.Xml_InvalidPosition, string.Empty);
		}
		do
		{
			if (!defattr && reader.IsDefaultInternal)
			{
				continue;
			}
			await WriteStartAttributeAsync(reader.Prefix, reader.LocalName, reader.NamespaceURI).ConfigureAwait(continueOnCapturedContext: false);
			while (reader.ReadAttributeValue())
			{
				if (reader.NodeType == XmlNodeType.EntityReference)
				{
					await WriteEntityRefAsync(reader.Name).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					await WriteStringAsync(reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			await WriteEndAttributeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		while (reader.MoveToNextAttribute());
	}

	public virtual Task WriteNodeAsync(XmlReader reader, bool defattr)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (reader.Settings != null && reader.Settings.Async)
		{
			return WriteNodeAsync_CallAsyncReader(reader, defattr);
		}
		return WriteNodeAsync_CallSyncReader(reader, defattr);
	}

	internal async Task WriteNodeAsync_CallSyncReader(XmlReader reader, bool defattr)
	{
		bool canReadChunk = reader.CanReadValueChunk;
		int d = ((reader.NodeType == XmlNodeType.None) ? (-1) : reader.Depth);
		do
		{
			switch (reader.NodeType)
			{
			case XmlNodeType.Element:
				await WriteStartElementAsync(reader.Prefix, reader.LocalName, reader.NamespaceURI).ConfigureAwait(continueOnCapturedContext: false);
				await WriteAttributesAsync(reader, defattr).ConfigureAwait(continueOnCapturedContext: false);
				if (reader.IsEmptyElement)
				{
					await WriteEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				break;
			case XmlNodeType.Text:
				if (canReadChunk)
				{
					if (_writeNodeBuffer == null)
					{
						_writeNodeBuffer = new char[1024];
					}
					int count;
					while ((count = reader.ReadValueChunk(_writeNodeBuffer, 0, 1024)) > 0)
					{
						await WriteCharsAsync(_writeNodeBuffer, 0, count).ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				else
				{
					await WriteStringAsync(reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				}
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				await WriteWhitespaceAsync(reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.CDATA:
				await WriteCDataAsync(reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.EntityReference:
				await WriteEntityRefAsync(reader.Name).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.XmlDeclaration:
				await WriteProcessingInstructionAsync(reader.Name, reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.DocumentType:
				await WriteDocTypeAsync(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.Comment:
				await WriteCommentAsync(reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.EndElement:
				await WriteFullEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
				break;
			}
		}
		while (reader.Read() && (d < reader.Depth || (d == reader.Depth && reader.NodeType == XmlNodeType.EndElement)));
	}

	internal async Task WriteNodeAsync_CallAsyncReader(XmlReader reader, bool defattr)
	{
		bool canReadChunk = reader.CanReadValueChunk;
		int d = ((reader.NodeType == XmlNodeType.None) ? (-1) : reader.Depth);
		do
		{
			switch (reader.NodeType)
			{
			case XmlNodeType.Element:
				await WriteStartElementAsync(reader.Prefix, reader.LocalName, reader.NamespaceURI).ConfigureAwait(continueOnCapturedContext: false);
				await WriteAttributesAsync(reader, defattr).ConfigureAwait(continueOnCapturedContext: false);
				if (reader.IsEmptyElement)
				{
					await WriteEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				break;
			case XmlNodeType.Text:
				if (canReadChunk)
				{
					if (_writeNodeBuffer == null)
					{
						_writeNodeBuffer = new char[1024];
					}
					int count;
					while ((count = await reader.ReadValueChunkAsync(_writeNodeBuffer, 0, 1024).ConfigureAwait(continueOnCapturedContext: false)) > 0)
					{
						await WriteCharsAsync(_writeNodeBuffer, 0, count).ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				else
				{
					await WriteStringAsync(await reader.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false)).ConfigureAwait(continueOnCapturedContext: false);
				}
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				await WriteWhitespaceAsync(await reader.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false)).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.CDATA:
				await WriteCDataAsync(reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.EntityReference:
				await WriteEntityRefAsync(reader.Name).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.XmlDeclaration:
				await WriteProcessingInstructionAsync(reader.Name, reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.DocumentType:
				await WriteDocTypeAsync(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.Comment:
				await WriteCommentAsync(reader.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XmlNodeType.EndElement:
				await WriteFullEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
				break;
			}
		}
		while (await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false) && (d < reader.Depth || (d == reader.Depth && reader.NodeType == XmlNodeType.EndElement)));
	}

	public virtual async Task WriteNodeAsync(XPathNavigator navigator, bool defattr)
	{
		if (navigator == null)
		{
			throw new ArgumentNullException("navigator");
		}
		int iLevel = 0;
		navigator = navigator.Clone();
		while (true)
		{
			bool mayHaveChildren = false;
			switch (navigator.NodeType)
			{
			case XPathNodeType.Element:
				await WriteStartElementAsync(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI).ConfigureAwait(continueOnCapturedContext: false);
				if (navigator.MoveToFirstAttribute())
				{
					do
					{
						IXmlSchemaInfo schemaInfo = navigator.SchemaInfo;
						if (defattr || schemaInfo == null || !schemaInfo.IsDefault)
						{
							await WriteStartAttributeAsync(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI).ConfigureAwait(continueOnCapturedContext: false);
							await WriteStringAsync(navigator.Value).ConfigureAwait(continueOnCapturedContext: false);
							await WriteEndAttributeAsync().ConfigureAwait(continueOnCapturedContext: false);
						}
					}
					while (navigator.MoveToNextAttribute());
					navigator.MoveToParent();
				}
				if (navigator.MoveToFirstNamespace(XPathNamespaceScope.Local))
				{
					await WriteLocalNamespacesAsync(navigator).ConfigureAwait(continueOnCapturedContext: false);
					navigator.MoveToParent();
				}
				mayHaveChildren = true;
				break;
			case XPathNodeType.Text:
				await WriteStringAsync(navigator.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XPathNodeType.SignificantWhitespace:
			case XPathNodeType.Whitespace:
				await WriteWhitespaceAsync(navigator.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XPathNodeType.Root:
				mayHaveChildren = true;
				break;
			case XPathNodeType.Comment:
				await WriteCommentAsync(navigator.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case XPathNodeType.ProcessingInstruction:
				await WriteProcessingInstructionAsync(navigator.LocalName, navigator.Value).ConfigureAwait(continueOnCapturedContext: false);
				break;
			}
			if (mayHaveChildren)
			{
				if (navigator.MoveToFirstChild())
				{
					iLevel++;
					continue;
				}
				if (navigator.NodeType == XPathNodeType.Element)
				{
					if (!navigator.IsEmptyElement)
					{
						await WriteFullEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						await WriteEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
				}
			}
			while (true)
			{
				if (iLevel == 0)
				{
					return;
				}
				if (navigator.MoveToNext())
				{
					break;
				}
				iLevel--;
				navigator.MoveToParent();
				if (navigator.NodeType == XPathNodeType.Element)
				{
					await WriteFullEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
	}

	public async Task WriteElementStringAsync(string? prefix, string localName, string? ns, string value)
	{
		await WriteStartElementAsync(prefix, localName, ns).ConfigureAwait(continueOnCapturedContext: false);
		if (value != null && value.Length != 0)
		{
			await WriteStringAsync(value).ConfigureAwait(continueOnCapturedContext: false);
		}
		await WriteEndElementAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task WriteLocalNamespacesAsync(XPathNavigator nsNav)
	{
		string prefix = nsNav.LocalName;
		string ns = nsNav.Value;
		if (nsNav.MoveToNextNamespace(XPathNamespaceScope.Local))
		{
			await WriteLocalNamespacesAsync(nsNav).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (prefix.Length == 0)
		{
			await WriteAttributeStringAsync(string.Empty, "xmlns", "http://www.w3.org/2000/xmlns/", ns).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await WriteAttributeStringAsync("xmlns", prefix, "http://www.w3.org/2000/xmlns/", ns).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCore().ConfigureAwait(continueOnCapturedContext: false);
		Dispose(disposing: false);
		GC.SuppressFinalize(this);
	}

	protected virtual ValueTask DisposeAsyncCore()
	{
		if (WriteState != WriteState.Closed)
		{
			Dispose(disposing: true);
		}
		return default(ValueTask);
	}
}
