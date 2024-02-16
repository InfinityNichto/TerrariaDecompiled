using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml;

internal abstract class XmlRawWriter : XmlWriter
{
	protected XmlRawWriterBase64Encoder _base64Encoder;

	protected IXmlNamespaceResolver _resolver;

	public override WriteState WriteState
	{
		get
		{
			throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
		}
	}

	public override XmlSpace XmlSpace
	{
		get
		{
			throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
		}
	}

	public override string XmlLang
	{
		get
		{
			throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
		}
	}

	internal virtual IXmlNamespaceResolver NamespaceResolver
	{
		set
		{
			_resolver = value;
		}
	}

	internal virtual bool SupportsNamespaceDeclarationInChunks => false;

	public override void WriteStartDocument()
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteStartDocument(bool standalone)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteEndDocument()
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
	}

	public override void WriteEndElement()
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteFullEndElement()
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		if (_base64Encoder == null)
		{
			_base64Encoder = new XmlRawWriterBase64Encoder(this);
		}
		_base64Encoder.Encode(buffer, index, count);
	}

	public override string LookupPrefix(string ns)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteNmToken(string name)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteName(string name)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteQualifiedName(string localName, string ns)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteCData(string text)
	{
		WriteString(text);
	}

	public override void WriteCharEntity(char ch)
	{
		WriteString(char.ToString(ch));
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		Span<char> span = stackalloc char[2] { lowChar, highChar };
		ReadOnlySpan<char> value = span;
		WriteString(new string(value));
	}

	public override void WriteWhitespace(string ws)
	{
		WriteString(ws);
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		WriteString(new string(buffer, index, count));
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		WriteString(new string(buffer, index, count));
	}

	public override void WriteRaw(string data)
	{
		WriteString(data);
	}

	public override void WriteValue(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		WriteString(XmlUntypedConverter.Untyped.ToString(value, _resolver));
	}

	public override void WriteValue(string value)
	{
		WriteString(value);
	}

	public override void WriteValue(DateTimeOffset value)
	{
		WriteString(XmlConvert.ToString(value));
	}

	public override void WriteAttributes(XmlReader reader, bool defattr)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteNode(XmlReader reader, bool defattr)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteNode(XPathNavigator navigator, bool defattr)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	internal virtual void WriteXmlDeclaration(XmlStandalone standalone)
	{
	}

	internal virtual void WriteXmlDeclaration(string xmldecl)
	{
	}

	internal abstract void StartElementContent();

	internal virtual void OnRootElement(ConformanceLevel conformanceLevel)
	{
	}

	internal abstract void WriteEndElement(string prefix, string localName, string ns);

	internal virtual void WriteFullEndElement(string prefix, string localName, string ns)
	{
		WriteEndElement(prefix, localName, ns);
	}

	internal virtual void WriteQualifiedName(string prefix, string localName, string ns)
	{
		if (prefix.Length != 0)
		{
			WriteString(prefix);
			WriteString(":");
		}
		WriteString(localName);
	}

	internal abstract void WriteNamespaceDeclaration(string prefix, string ns);

	internal virtual void WriteStartNamespaceDeclaration(string prefix)
	{
		throw new NotSupportedException();
	}

	internal virtual void WriteEndNamespaceDeclaration()
	{
		throw new NotSupportedException();
	}

	internal virtual void WriteEndBase64()
	{
		_base64Encoder.Flush();
	}

	internal virtual void Close(WriteState currentState)
	{
		Close();
	}

	public override Task WriteStartDocumentAsync()
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override Task WriteStartDocumentAsync(bool standalone)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override Task WriteEndDocumentAsync()
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override Task WriteDocTypeAsync(string name, string pubid, string sysid, string subset)
	{
		return Task.CompletedTask;
	}

	public override Task WriteEndElementAsync()
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override Task WriteFullEndElementAsync()
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override Task WriteBase64Async(byte[] buffer, int index, int count)
	{
		if (_base64Encoder == null)
		{
			_base64Encoder = new XmlRawWriterBase64Encoder(this);
		}
		return _base64Encoder.EncodeAsync(buffer, index, count);
	}

	public override Task WriteNmTokenAsync(string name)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override Task WriteNameAsync(string name)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override Task WriteQualifiedNameAsync(string localName, string ns)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override Task WriteCDataAsync(string text)
	{
		return WriteStringAsync(text);
	}

	public override Task WriteCharEntityAsync(char ch)
	{
		return WriteStringAsync(char.ToString(ch));
	}

	public override Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
	{
		Span<char> span = stackalloc char[2] { lowChar, highChar };
		ReadOnlySpan<char> value = span;
		return WriteStringAsync(new string(value));
	}

	public override Task WriteWhitespaceAsync(string ws)
	{
		return WriteStringAsync(ws);
	}

	public override Task WriteCharsAsync(char[] buffer, int index, int count)
	{
		return WriteStringAsync(new string(buffer, index, count));
	}

	public override Task WriteRawAsync(char[] buffer, int index, int count)
	{
		return WriteStringAsync(new string(buffer, index, count));
	}

	public override Task WriteRawAsync(string data)
	{
		return WriteStringAsync(data);
	}

	public override Task WriteAttributesAsync(XmlReader reader, bool defattr)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override Task WriteNodeAsync(XmlReader reader, bool defattr)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override Task WriteNodeAsync(XPathNavigator navigator, bool defattr)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	internal virtual Task WriteXmlDeclarationAsync(XmlStandalone standalone)
	{
		return Task.CompletedTask;
	}

	internal virtual Task WriteXmlDeclarationAsync(string xmldecl)
	{
		return Task.CompletedTask;
	}

	internal virtual Task WriteEndElementAsync(string prefix, string localName, string ns)
	{
		throw new NotImplementedException();
	}

	internal virtual Task WriteFullEndElementAsync(string prefix, string localName, string ns)
	{
		return WriteEndElementAsync(prefix, localName, ns);
	}

	internal virtual async Task WriteQualifiedNameAsync(string prefix, string localName, string ns)
	{
		if (prefix.Length != 0)
		{
			await WriteStringAsync(prefix).ConfigureAwait(continueOnCapturedContext: false);
			await WriteStringAsync(":").ConfigureAwait(continueOnCapturedContext: false);
		}
		await WriteStringAsync(localName).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal virtual Task WriteNamespaceDeclarationAsync(string prefix, string ns)
	{
		throw new NotImplementedException();
	}

	internal virtual Task WriteStartNamespaceDeclarationAsync(string prefix)
	{
		throw new NotSupportedException();
	}

	internal virtual Task WriteEndNamespaceDeclarationAsync()
	{
		throw new NotSupportedException();
	}

	internal virtual Task WriteEndBase64Async()
	{
		return _base64Encoder.FlushAsync();
	}

	internal virtual ValueTask DisposeAsyncCore(WriteState currentState)
	{
		return DisposeAsyncCore();
	}
}
