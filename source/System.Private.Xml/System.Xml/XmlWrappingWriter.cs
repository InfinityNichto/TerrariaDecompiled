using System.Threading.Tasks;

namespace System.Xml;

internal class XmlWrappingWriter : XmlWriter
{
	protected XmlWriter writer;

	public override XmlWriterSettings Settings => writer.Settings;

	public override WriteState WriteState => writer.WriteState;

	public override XmlSpace XmlSpace => writer.XmlSpace;

	public override string XmlLang => writer.XmlLang;

	internal XmlWrappingWriter(XmlWriter baseWriter)
	{
		writer = baseWriter;
	}

	public override void WriteStartDocument()
	{
		writer.WriteStartDocument();
	}

	public override void WriteStartDocument(bool standalone)
	{
		writer.WriteStartDocument(standalone);
	}

	public override void WriteEndDocument()
	{
		writer.WriteEndDocument();
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		writer.WriteDocType(name, pubid, sysid, subset);
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		writer.WriteStartElement(prefix, localName, ns);
	}

	public override void WriteEndElement()
	{
		writer.WriteEndElement();
	}

	public override void WriteFullEndElement()
	{
		writer.WriteFullEndElement();
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		writer.WriteStartAttribute(prefix, localName, ns);
	}

	public override void WriteEndAttribute()
	{
		writer.WriteEndAttribute();
	}

	public override void WriteCData(string text)
	{
		writer.WriteCData(text);
	}

	public override void WriteComment(string text)
	{
		writer.WriteComment(text);
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		writer.WriteProcessingInstruction(name, text);
	}

	public override void WriteEntityRef(string name)
	{
		writer.WriteEntityRef(name);
	}

	public override void WriteCharEntity(char ch)
	{
		writer.WriteCharEntity(ch);
	}

	public override void WriteWhitespace(string ws)
	{
		writer.WriteWhitespace(ws);
	}

	public override void WriteString(string text)
	{
		writer.WriteString(text);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		writer.WriteSurrogateCharEntity(lowChar, highChar);
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		writer.WriteChars(buffer, index, count);
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		writer.WriteRaw(buffer, index, count);
	}

	public override void WriteRaw(string data)
	{
		writer.WriteRaw(data);
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		writer.WriteBase64(buffer, index, count);
	}

	public override void Close()
	{
		writer.Close();
	}

	public override void Flush()
	{
		writer.Flush();
	}

	public override string LookupPrefix(string ns)
	{
		return writer.LookupPrefix(ns);
	}

	public override void WriteValue(object value)
	{
		writer.WriteValue(value);
	}

	public override void WriteValue(string value)
	{
		writer.WriteValue(value);
	}

	public override void WriteValue(bool value)
	{
		writer.WriteValue(value);
	}

	public override void WriteValue(DateTime value)
	{
		writer.WriteValue(value);
	}

	public override void WriteValue(DateTimeOffset value)
	{
		writer.WriteValue(value);
	}

	public override void WriteValue(double value)
	{
		writer.WriteValue(value);
	}

	public override void WriteValue(float value)
	{
		writer.WriteValue(value);
	}

	public override void WriteValue(decimal value)
	{
		writer.WriteValue(value);
	}

	public override void WriteValue(int value)
	{
		writer.WriteValue(value);
	}

	public override void WriteValue(long value)
	{
		writer.WriteValue(value);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			((IDisposable)writer).Dispose();
		}
	}

	public override Task WriteStartDocumentAsync()
	{
		return writer.WriteStartDocumentAsync();
	}

	public override Task WriteStartDocumentAsync(bool standalone)
	{
		return writer.WriteStartDocumentAsync(standalone);
	}

	public override Task WriteEndDocumentAsync()
	{
		return writer.WriteEndDocumentAsync();
	}

	public override Task WriteDocTypeAsync(string name, string pubid, string sysid, string subset)
	{
		return writer.WriteDocTypeAsync(name, pubid, sysid, subset);
	}

	public override Task WriteStartElementAsync(string prefix, string localName, string ns)
	{
		return writer.WriteStartElementAsync(prefix, localName, ns);
	}

	public override Task WriteEndElementAsync()
	{
		return writer.WriteEndElementAsync();
	}

	public override Task WriteFullEndElementAsync()
	{
		return writer.WriteFullEndElementAsync();
	}

	protected internal override Task WriteStartAttributeAsync(string prefix, string localName, string ns)
	{
		return writer.WriteStartAttributeAsync(prefix, localName, ns);
	}

	protected internal override Task WriteEndAttributeAsync()
	{
		return writer.WriteEndAttributeAsync();
	}

	public override Task WriteCDataAsync(string text)
	{
		return writer.WriteCDataAsync(text);
	}

	public override Task WriteCommentAsync(string text)
	{
		return writer.WriteCommentAsync(text);
	}

	public override Task WriteProcessingInstructionAsync(string name, string text)
	{
		return writer.WriteProcessingInstructionAsync(name, text);
	}

	public override Task WriteEntityRefAsync(string name)
	{
		return writer.WriteEntityRefAsync(name);
	}

	public override Task WriteCharEntityAsync(char ch)
	{
		return writer.WriteCharEntityAsync(ch);
	}

	public override Task WriteWhitespaceAsync(string ws)
	{
		return writer.WriteWhitespaceAsync(ws);
	}

	public override Task WriteStringAsync(string text)
	{
		return writer.WriteStringAsync(text);
	}

	public override Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
	{
		return writer.WriteSurrogateCharEntityAsync(lowChar, highChar);
	}

	public override Task WriteCharsAsync(char[] buffer, int index, int count)
	{
		return writer.WriteCharsAsync(buffer, index, count);
	}

	public override Task WriteRawAsync(char[] buffer, int index, int count)
	{
		return writer.WriteRawAsync(buffer, index, count);
	}

	public override Task WriteRawAsync(string data)
	{
		return writer.WriteRawAsync(data);
	}

	public override Task WriteBase64Async(byte[] buffer, int index, int count)
	{
		return writer.WriteBase64Async(buffer, index, count);
	}

	public override Task FlushAsync()
	{
		return writer.FlushAsync();
	}
}
