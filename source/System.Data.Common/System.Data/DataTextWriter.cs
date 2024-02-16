using System.IO;
using System.Xml;

namespace System.Data;

internal sealed class DataTextWriter : XmlWriter
{
	private readonly XmlWriter _xmltextWriter;

	internal Stream BaseStream
	{
		get
		{
			if (_xmltextWriter is XmlTextWriter xmlTextWriter)
			{
				return xmlTextWriter.BaseStream;
			}
			return null;
		}
	}

	public override WriteState WriteState => _xmltextWriter.WriteState;

	public override XmlSpace XmlSpace => _xmltextWriter.XmlSpace;

	public override string XmlLang => _xmltextWriter.XmlLang;

	internal static XmlWriter CreateWriter(XmlWriter xw)
	{
		return new DataTextWriter(xw);
	}

	private DataTextWriter(XmlWriter w)
	{
		_xmltextWriter = w;
	}

	public override void WriteStartDocument()
	{
		_xmltextWriter.WriteStartDocument();
	}

	public override void WriteStartDocument(bool standalone)
	{
		_xmltextWriter.WriteStartDocument(standalone);
	}

	public override void WriteEndDocument()
	{
		_xmltextWriter.WriteEndDocument();
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		_xmltextWriter.WriteDocType(name, pubid, sysid, subset);
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		_xmltextWriter.WriteStartElement(prefix, localName, ns);
	}

	public override void WriteEndElement()
	{
		_xmltextWriter.WriteEndElement();
	}

	public override void WriteFullEndElement()
	{
		_xmltextWriter.WriteFullEndElement();
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		_xmltextWriter.WriteStartAttribute(prefix, localName, ns);
	}

	public override void WriteEndAttribute()
	{
		_xmltextWriter.WriteEndAttribute();
	}

	public override void WriteCData(string text)
	{
		_xmltextWriter.WriteCData(text);
	}

	public override void WriteComment(string text)
	{
		_xmltextWriter.WriteComment(text);
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		_xmltextWriter.WriteProcessingInstruction(name, text);
	}

	public override void WriteEntityRef(string name)
	{
		_xmltextWriter.WriteEntityRef(name);
	}

	public override void WriteCharEntity(char ch)
	{
		_xmltextWriter.WriteCharEntity(ch);
	}

	public override void WriteWhitespace(string ws)
	{
		_xmltextWriter.WriteWhitespace(ws);
	}

	public override void WriteString(string text)
	{
		_xmltextWriter.WriteString(text);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		_xmltextWriter.WriteSurrogateCharEntity(lowChar, highChar);
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		_xmltextWriter.WriteChars(buffer, index, count);
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		_xmltextWriter.WriteRaw(buffer, index, count);
	}

	public override void WriteRaw(string data)
	{
		_xmltextWriter.WriteRaw(data);
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		_xmltextWriter.WriteBase64(buffer, index, count);
	}

	public override void WriteBinHex(byte[] buffer, int index, int count)
	{
		_xmltextWriter.WriteBinHex(buffer, index, count);
	}

	public override void Close()
	{
		_xmltextWriter.Close();
	}

	public override void Flush()
	{
		_xmltextWriter.Flush();
	}

	public override void WriteName(string name)
	{
		_xmltextWriter.WriteName(name);
	}

	public override void WriteQualifiedName(string localName, string ns)
	{
		_xmltextWriter.WriteQualifiedName(localName, ns);
	}

	public override string LookupPrefix(string ns)
	{
		return _xmltextWriter.LookupPrefix(ns);
	}

	public override void WriteNmToken(string name)
	{
		_xmltextWriter.WriteNmToken(name);
	}
}
