using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Xml;

internal sealed class XmlAutoDetectWriter : XmlRawWriter, IRemovableWriter
{
	private XmlRawWriter _wrapped;

	private OnRemoveWriter _onRemove;

	private readonly XmlWriterSettings _writerSettings;

	private readonly XmlEventCache _eventCache;

	private readonly TextWriter _textWriter;

	private readonly Stream _strm;

	public OnRemoveWriter OnRemoveWriterEvent
	{
		set
		{
			_onRemove = value;
		}
	}

	public override XmlWriterSettings Settings => _writerSettings;

	internal override IXmlNamespaceResolver NamespaceResolver
	{
		set
		{
			_resolver = value;
			if (_wrapped == null)
			{
				_eventCache.NamespaceResolver = value;
			}
			else
			{
				_wrapped.NamespaceResolver = value;
			}
		}
	}

	internal override bool SupportsNamespaceDeclarationInChunks => _wrapped.SupportsNamespaceDeclarationInChunks;

	private XmlAutoDetectWriter(XmlWriterSettings writerSettings)
	{
		_writerSettings = writerSettings.Clone();
		_writerSettings.ReadOnly = true;
		_eventCache = new XmlEventCache(string.Empty, hasRootNode: true);
	}

	public XmlAutoDetectWriter(TextWriter textWriter, XmlWriterSettings writerSettings)
		: this(writerSettings)
	{
		_textWriter = textWriter;
	}

	public XmlAutoDetectWriter(Stream strm, XmlWriterSettings writerSettings)
		: this(writerSettings)
	{
		_strm = strm;
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteDocType(name, pubid, sysid, subset);
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		if (_wrapped == null)
		{
			if (ns.Length == 0 && IsHtmlTag(localName))
			{
				CreateWrappedWriter(XmlOutputMethod.Html);
			}
			else
			{
				CreateWrappedWriter(XmlOutputMethod.Xml);
			}
		}
		_wrapped.WriteStartElement(prefix, localName, ns);
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteStartAttribute(prefix, localName, ns);
	}

	public override void WriteEndAttribute()
	{
		_wrapped.WriteEndAttribute();
	}

	public override void WriteCData(string text)
	{
		if (TextBlockCreatesWriter(text))
		{
			_wrapped.WriteCData(text);
		}
		else
		{
			_eventCache.WriteCData(text);
		}
	}

	public override void WriteComment(string text)
	{
		if (_wrapped == null)
		{
			_eventCache.WriteComment(text);
		}
		else
		{
			_wrapped.WriteComment(text);
		}
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		if (_wrapped == null)
		{
			_eventCache.WriteProcessingInstruction(name, text);
		}
		else
		{
			_wrapped.WriteProcessingInstruction(name, text);
		}
	}

	public override void WriteWhitespace(string ws)
	{
		if (_wrapped == null)
		{
			_eventCache.WriteWhitespace(ws);
		}
		else
		{
			_wrapped.WriteWhitespace(ws);
		}
	}

	public override void WriteString(string text)
	{
		if (TextBlockCreatesWriter(text))
		{
			_wrapped.WriteString(text);
		}
		else
		{
			_eventCache.WriteString(text);
		}
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		WriteString(new string(buffer, index, count));
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		WriteRaw(new string(buffer, index, count));
	}

	public override void WriteRaw(string data)
	{
		if (TextBlockCreatesWriter(data))
		{
			_wrapped.WriteRaw(data);
		}
		else
		{
			_eventCache.WriteRaw(data);
		}
	}

	public override void WriteEntityRef(string name)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteEntityRef(name);
	}

	public override void WriteCharEntity(char ch)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteCharEntity(ch);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteSurrogateCharEntity(lowChar, highChar);
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteBase64(buffer, index, count);
	}

	public override void WriteBinHex(byte[] buffer, int index, int count)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteBinHex(buffer, index, count);
	}

	public override void Close()
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.Close();
	}

	public override void Flush()
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.Flush();
	}

	public override void WriteValue(object value)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(string value)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(bool value)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(DateTime value)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(DateTimeOffset value)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(double value)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(float value)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(decimal value)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(int value)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteValue(value);
	}

	public override void WriteValue(long value)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteValue(value);
	}

	internal override void WriteXmlDeclaration(XmlStandalone standalone)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteXmlDeclaration(standalone);
	}

	internal override void WriteXmlDeclaration(string xmldecl)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteXmlDeclaration(xmldecl);
	}

	internal override void StartElementContent()
	{
		_wrapped.StartElementContent();
	}

	internal override void WriteEndElement(string prefix, string localName, string ns)
	{
		_wrapped.WriteEndElement(prefix, localName, ns);
	}

	internal override void WriteFullEndElement(string prefix, string localName, string ns)
	{
		_wrapped.WriteFullEndElement(prefix, localName, ns);
	}

	internal override void WriteNamespaceDeclaration(string prefix, string ns)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteNamespaceDeclaration(prefix, ns);
	}

	internal override void WriteStartNamespaceDeclaration(string prefix)
	{
		EnsureWrappedWriter(XmlOutputMethod.Xml);
		_wrapped.WriteStartNamespaceDeclaration(prefix);
	}

	internal override void WriteEndNamespaceDeclaration()
	{
		_wrapped.WriteEndNamespaceDeclaration();
	}

	private static bool IsHtmlTag(string tagName)
	{
		if (tagName.Length != 4)
		{
			return false;
		}
		if (tagName[0] != 'H' && tagName[0] != 'h')
		{
			return false;
		}
		if (tagName[1] != 'T' && tagName[1] != 't')
		{
			return false;
		}
		if (tagName[2] != 'M' && tagName[2] != 'm')
		{
			return false;
		}
		if (tagName[3] != 'L' && tagName[3] != 'l')
		{
			return false;
		}
		return true;
	}

	[MemberNotNull("_wrapped")]
	private void EnsureWrappedWriter(XmlOutputMethod outMethod)
	{
		if (_wrapped == null)
		{
			CreateWrappedWriter(outMethod);
		}
	}

	[MemberNotNullWhen(true, "_wrapped")]
	private bool TextBlockCreatesWriter(string textBlock)
	{
		if (_wrapped == null)
		{
			if (XmlCharType.IsOnlyWhitespace(textBlock))
			{
				return false;
			}
			CreateWrappedWriter(XmlOutputMethod.Xml);
		}
		return true;
	}

	[MemberNotNull("_wrapped")]
	private void CreateWrappedWriter(XmlOutputMethod outMethod)
	{
		_writerSettings.ReadOnly = false;
		_writerSettings.OutputMethod = outMethod;
		if (outMethod == XmlOutputMethod.Html && _writerSettings.IndentInternal == TriState.Unknown)
		{
			_writerSettings.Indent = true;
		}
		_writerSettings.ReadOnly = true;
		if (_textWriter != null)
		{
			_wrapped = ((XmlWellFormedWriter)XmlWriter.Create(_textWriter, _writerSettings)).RawWriter;
		}
		else
		{
			_wrapped = ((XmlWellFormedWriter)XmlWriter.Create(_strm, _writerSettings)).RawWriter;
		}
		_eventCache.EndEvents();
		_eventCache.EventsToWriter(_wrapped);
		if (_onRemove != null)
		{
			_onRemove(_wrapped);
		}
	}
}
