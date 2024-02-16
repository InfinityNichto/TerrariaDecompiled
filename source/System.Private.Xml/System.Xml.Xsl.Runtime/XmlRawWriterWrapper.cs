namespace System.Xml.Xsl.Runtime;

internal sealed class XmlRawWriterWrapper : XmlRawWriter
{
	private readonly XmlWriter _wrapped;

	public override XmlWriterSettings Settings => _wrapped.Settings;

	public XmlRawWriterWrapper(XmlWriter writer)
	{
		_wrapped = writer;
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		_wrapped.WriteDocType(name, pubid, sysid, subset);
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		_wrapped.WriteStartElement(prefix, localName, ns);
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		_wrapped.WriteStartAttribute(prefix, localName, ns);
	}

	public override void WriteEndAttribute()
	{
		_wrapped.WriteEndAttribute();
	}

	public override void WriteCData(string text)
	{
		_wrapped.WriteCData(text);
	}

	public override void WriteComment(string text)
	{
		_wrapped.WriteComment(text);
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		_wrapped.WriteProcessingInstruction(name, text);
	}

	public override void WriteWhitespace(string ws)
	{
		_wrapped.WriteWhitespace(ws);
	}

	public override void WriteString(string text)
	{
		_wrapped.WriteString(text);
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		_wrapped.WriteChars(buffer, index, count);
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		_wrapped.WriteRaw(buffer, index, count);
	}

	public override void WriteRaw(string data)
	{
		_wrapped.WriteRaw(data);
	}

	public override void WriteEntityRef(string name)
	{
		_wrapped.WriteEntityRef(name);
	}

	public override void WriteCharEntity(char ch)
	{
		_wrapped.WriteCharEntity(ch);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		_wrapped.WriteSurrogateCharEntity(lowChar, highChar);
	}

	public override void Close()
	{
		_wrapped.Close();
	}

	public override void Flush()
	{
		_wrapped.Flush();
	}

	public override void WriteValue(object value)
	{
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(string value)
	{
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(bool value)
	{
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(DateTime value)
	{
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(float value)
	{
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(decimal value)
	{
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(double value)
	{
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(int value)
	{
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(long value)
	{
		_wrapped.WriteValue(value);
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing)
			{
				((IDisposable)_wrapped).Dispose();
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	internal override void WriteXmlDeclaration(XmlStandalone standalone)
	{
	}

	internal override void WriteXmlDeclaration(string xmldecl)
	{
	}

	internal override void StartElementContent()
	{
	}

	internal override void WriteEndElement(string prefix, string localName, string ns)
	{
		_wrapped.WriteEndElement();
	}

	internal override void WriteFullEndElement(string prefix, string localName, string ns)
	{
		_wrapped.WriteFullEndElement();
	}

	internal override void WriteNamespaceDeclaration(string prefix, string ns)
	{
		if (prefix.Length == 0)
		{
			_wrapped.WriteAttributeString(string.Empty, "xmlns", "http://www.w3.org/2000/xmlns/", ns);
		}
		else
		{
			_wrapped.WriteAttributeString("xmlns", prefix, "http://www.w3.org/2000/xmlns/", ns);
		}
	}
}
