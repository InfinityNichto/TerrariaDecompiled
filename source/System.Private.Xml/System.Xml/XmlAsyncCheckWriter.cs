using System.Threading.Tasks;
using System.Xml.XPath;

namespace System.Xml;

internal sealed class XmlAsyncCheckWriter : XmlWriter
{
	private readonly XmlWriter _coreWriter;

	private Task _lastTask = Task.CompletedTask;

	internal XmlWriter CoreWriter => _coreWriter;

	public override XmlWriterSettings Settings
	{
		get
		{
			XmlWriterSettings settings = _coreWriter.Settings;
			settings = ((settings == null) ? new XmlWriterSettings() : settings.Clone());
			settings.Async = true;
			settings.ReadOnly = true;
			return settings;
		}
	}

	public override WriteState WriteState
	{
		get
		{
			CheckAsync();
			return _coreWriter.WriteState;
		}
	}

	public override XmlSpace XmlSpace
	{
		get
		{
			CheckAsync();
			return _coreWriter.XmlSpace;
		}
	}

	public override string XmlLang
	{
		get
		{
			CheckAsync();
			return _coreWriter.XmlLang;
		}
	}

	public XmlAsyncCheckWriter(XmlWriter writer)
	{
		_coreWriter = writer;
	}

	private void CheckAsync()
	{
		if (!_lastTask.IsCompleted)
		{
			throw new InvalidOperationException(System.SR.Xml_AsyncIsRunningException);
		}
	}

	public override void WriteStartDocument()
	{
		CheckAsync();
		_coreWriter.WriteStartDocument();
	}

	public override void WriteStartDocument(bool standalone)
	{
		CheckAsync();
		_coreWriter.WriteStartDocument(standalone);
	}

	public override void WriteEndDocument()
	{
		CheckAsync();
		_coreWriter.WriteEndDocument();
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		CheckAsync();
		_coreWriter.WriteDocType(name, pubid, sysid, subset);
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		CheckAsync();
		_coreWriter.WriteStartElement(prefix, localName, ns);
	}

	public override void WriteEndElement()
	{
		CheckAsync();
		_coreWriter.WriteEndElement();
	}

	public override void WriteFullEndElement()
	{
		CheckAsync();
		_coreWriter.WriteFullEndElement();
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		CheckAsync();
		_coreWriter.WriteStartAttribute(prefix, localName, ns);
	}

	public override void WriteEndAttribute()
	{
		CheckAsync();
		_coreWriter.WriteEndAttribute();
	}

	public override void WriteCData(string text)
	{
		CheckAsync();
		_coreWriter.WriteCData(text);
	}

	public override void WriteComment(string text)
	{
		CheckAsync();
		_coreWriter.WriteComment(text);
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		CheckAsync();
		_coreWriter.WriteProcessingInstruction(name, text);
	}

	public override void WriteEntityRef(string name)
	{
		CheckAsync();
		_coreWriter.WriteEntityRef(name);
	}

	public override void WriteCharEntity(char ch)
	{
		CheckAsync();
		_coreWriter.WriteCharEntity(ch);
	}

	public override void WriteWhitespace(string ws)
	{
		CheckAsync();
		_coreWriter.WriteWhitespace(ws);
	}

	public override void WriteString(string text)
	{
		CheckAsync();
		_coreWriter.WriteString(text);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		CheckAsync();
		_coreWriter.WriteSurrogateCharEntity(lowChar, highChar);
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		CheckAsync();
		_coreWriter.WriteChars(buffer, index, count);
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		CheckAsync();
		_coreWriter.WriteRaw(buffer, index, count);
	}

	public override void WriteRaw(string data)
	{
		CheckAsync();
		_coreWriter.WriteRaw(data);
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		CheckAsync();
		_coreWriter.WriteBase64(buffer, index, count);
	}

	public override void WriteBinHex(byte[] buffer, int index, int count)
	{
		CheckAsync();
		_coreWriter.WriteBinHex(buffer, index, count);
	}

	public override void Close()
	{
		CheckAsync();
		_coreWriter.Close();
	}

	public override void Flush()
	{
		CheckAsync();
		_coreWriter.Flush();
	}

	public override string LookupPrefix(string ns)
	{
		CheckAsync();
		return _coreWriter.LookupPrefix(ns);
	}

	public override void WriteNmToken(string name)
	{
		CheckAsync();
		_coreWriter.WriteNmToken(name);
	}

	public override void WriteName(string name)
	{
		CheckAsync();
		_coreWriter.WriteName(name);
	}

	public override void WriteQualifiedName(string localName, string ns)
	{
		CheckAsync();
		_coreWriter.WriteQualifiedName(localName, ns);
	}

	public override void WriteValue(object value)
	{
		CheckAsync();
		_coreWriter.WriteValue(value);
	}

	public override void WriteValue(string value)
	{
		CheckAsync();
		_coreWriter.WriteValue(value);
	}

	public override void WriteValue(bool value)
	{
		CheckAsync();
		_coreWriter.WriteValue(value);
	}

	public override void WriteValue(DateTime value)
	{
		CheckAsync();
		_coreWriter.WriteValue(value);
	}

	public override void WriteValue(DateTimeOffset value)
	{
		CheckAsync();
		_coreWriter.WriteValue(value);
	}

	public override void WriteValue(double value)
	{
		CheckAsync();
		_coreWriter.WriteValue(value);
	}

	public override void WriteValue(float value)
	{
		CheckAsync();
		_coreWriter.WriteValue(value);
	}

	public override void WriteValue(decimal value)
	{
		CheckAsync();
		_coreWriter.WriteValue(value);
	}

	public override void WriteValue(int value)
	{
		CheckAsync();
		_coreWriter.WriteValue(value);
	}

	public override void WriteValue(long value)
	{
		CheckAsync();
		_coreWriter.WriteValue(value);
	}

	public override void WriteAttributes(XmlReader reader, bool defattr)
	{
		CheckAsync();
		_coreWriter.WriteAttributes(reader, defattr);
	}

	public override void WriteNode(XmlReader reader, bool defattr)
	{
		CheckAsync();
		_coreWriter.WriteNode(reader, defattr);
	}

	public override void WriteNode(XPathNavigator navigator, bool defattr)
	{
		CheckAsync();
		_coreWriter.WriteNode(navigator, defattr);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			CheckAsync();
			_coreWriter.Dispose();
		}
	}

	public override Task WriteStartDocumentAsync()
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteStartDocumentAsync();
	}

	public override Task WriteStartDocumentAsync(bool standalone)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteStartDocumentAsync(standalone);
	}

	public override Task WriteEndDocumentAsync()
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteEndDocumentAsync();
	}

	public override Task WriteDocTypeAsync(string name, string pubid, string sysid, string subset)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteDocTypeAsync(name, pubid, sysid, subset);
	}

	public override Task WriteStartElementAsync(string prefix, string localName, string ns)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteStartElementAsync(prefix, localName, ns);
	}

	public override Task WriteEndElementAsync()
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteEndElementAsync();
	}

	public override Task WriteFullEndElementAsync()
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteFullEndElementAsync();
	}

	protected internal override Task WriteStartAttributeAsync(string prefix, string localName, string ns)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteStartAttributeAsync(prefix, localName, ns);
	}

	protected internal override Task WriteEndAttributeAsync()
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteEndAttributeAsync();
	}

	public override Task WriteCDataAsync(string text)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteCDataAsync(text);
	}

	public override Task WriteCommentAsync(string text)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteCommentAsync(text);
	}

	public override Task WriteProcessingInstructionAsync(string name, string text)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteProcessingInstructionAsync(name, text);
	}

	public override Task WriteEntityRefAsync(string name)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteEntityRefAsync(name);
	}

	public override Task WriteCharEntityAsync(char ch)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteCharEntityAsync(ch);
	}

	public override Task WriteWhitespaceAsync(string ws)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteWhitespaceAsync(ws);
	}

	public override Task WriteStringAsync(string text)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteStringAsync(text);
	}

	public override Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteSurrogateCharEntityAsync(lowChar, highChar);
	}

	public override Task WriteCharsAsync(char[] buffer, int index, int count)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteCharsAsync(buffer, index, count);
	}

	public override Task WriteRawAsync(char[] buffer, int index, int count)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteRawAsync(buffer, index, count);
	}

	public override Task WriteRawAsync(string data)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteRawAsync(data);
	}

	public override Task WriteBase64Async(byte[] buffer, int index, int count)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteBase64Async(buffer, index, count);
	}

	public override Task WriteBinHexAsync(byte[] buffer, int index, int count)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteBinHexAsync(buffer, index, count);
	}

	public override Task FlushAsync()
	{
		CheckAsync();
		return _lastTask = _coreWriter.FlushAsync();
	}

	public override Task WriteNmTokenAsync(string name)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteNmTokenAsync(name);
	}

	public override Task WriteNameAsync(string name)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteNameAsync(name);
	}

	public override Task WriteQualifiedNameAsync(string localName, string ns)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteQualifiedNameAsync(localName, ns);
	}

	public override Task WriteAttributesAsync(XmlReader reader, bool defattr)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteAttributesAsync(reader, defattr);
	}

	public override Task WriteNodeAsync(XmlReader reader, bool defattr)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteNodeAsync(reader, defattr);
	}

	public override Task WriteNodeAsync(XPathNavigator navigator, bool defattr)
	{
		CheckAsync();
		return _lastTask = _coreWriter.WriteNodeAsync(navigator, defattr);
	}

	protected override ValueTask DisposeAsyncCore()
	{
		CheckAsync();
		return _coreWriter.DisposeAsync();
	}
}
