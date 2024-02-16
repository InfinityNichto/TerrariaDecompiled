using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml;

public abstract class XmlDictionaryWriter : XmlWriter
{
	private sealed class XmlWrappedWriter : XmlDictionaryWriter
	{
		private readonly XmlWriter _writer;

		private int _depth;

		private int _prefix;

		public override WriteState WriteState => _writer.WriteState;

		public override string XmlLang => _writer.XmlLang;

		public override XmlSpace XmlSpace => _writer.XmlSpace;

		public XmlWrappedWriter(XmlWriter writer)
		{
			_writer = writer;
			_depth = 0;
		}

		public override void Close()
		{
			_writer.Dispose();
		}

		public override void Flush()
		{
			_writer.Flush();
		}

		public override string LookupPrefix(string namespaceUri)
		{
			return _writer.LookupPrefix(namespaceUri);
		}

		public override void WriteAttributes(XmlReader reader, bool defattr)
		{
			_writer.WriteAttributes(reader, defattr);
		}

		public override void WriteBase64(byte[] buffer, int index, int count)
		{
			_writer.WriteBase64(buffer, index, count);
		}

		public override void WriteBinHex(byte[] buffer, int index, int count)
		{
			_writer.WriteBinHex(buffer, index, count);
		}

		public override void WriteCData(string text)
		{
			_writer.WriteCData(text);
		}

		public override void WriteCharEntity(char ch)
		{
			_writer.WriteCharEntity(ch);
		}

		public override void WriteChars(char[] buffer, int index, int count)
		{
			_writer.WriteChars(buffer, index, count);
		}

		public override void WriteComment(string text)
		{
			_writer.WriteComment(text);
		}

		public override void WriteDocType(string name, string pubid, string sysid, string subset)
		{
			_writer.WriteDocType(name, pubid, sysid, subset);
		}

		public override void WriteEndAttribute()
		{
			_writer.WriteEndAttribute();
		}

		public override void WriteEndDocument()
		{
			_writer.WriteEndDocument();
		}

		public override void WriteEndElement()
		{
			_writer.WriteEndElement();
			_depth--;
		}

		public override void WriteEntityRef(string name)
		{
			_writer.WriteEntityRef(name);
		}

		public override void WriteFullEndElement()
		{
			_writer.WriteFullEndElement();
		}

		public override void WriteName(string name)
		{
			_writer.WriteName(name);
		}

		public override void WriteNmToken(string name)
		{
			_writer.WriteNmToken(name);
		}

		public override void WriteNode(XmlReader reader, bool defattr)
		{
			_writer.WriteNode(reader, defattr);
		}

		public override void WriteProcessingInstruction(string name, string text)
		{
			_writer.WriteProcessingInstruction(name, text);
		}

		public override void WriteQualifiedName(string localName, string namespaceUri)
		{
			_writer.WriteQualifiedName(localName, namespaceUri);
		}

		public override void WriteRaw(char[] buffer, int index, int count)
		{
			_writer.WriteRaw(buffer, index, count);
		}

		public override void WriteRaw(string data)
		{
			_writer.WriteRaw(data);
		}

		public override void WriteStartAttribute(string prefix, string localName, string namespaceUri)
		{
			_writer.WriteStartAttribute(prefix, localName, namespaceUri);
			_prefix++;
		}

		public override void WriteStartDocument()
		{
			_writer.WriteStartDocument();
		}

		public override void WriteStartDocument(bool standalone)
		{
			_writer.WriteStartDocument(standalone);
		}

		public override void WriteStartElement(string prefix, string localName, string namespaceUri)
		{
			_writer.WriteStartElement(prefix, localName, namespaceUri);
			_depth++;
			_prefix = 1;
		}

		public override void WriteString(string text)
		{
			_writer.WriteString(text);
		}

		public override void WriteSurrogateCharEntity(char lowChar, char highChar)
		{
			_writer.WriteSurrogateCharEntity(lowChar, highChar);
		}

		public override void WriteWhitespace(string whitespace)
		{
			_writer.WriteWhitespace(whitespace);
		}

		public override void WriteValue(object value)
		{
			_writer.WriteValue(value);
		}

		public override void WriteValue(string value)
		{
			_writer.WriteValue(value);
		}

		public override void WriteValue(bool value)
		{
			_writer.WriteValue(value);
		}

		public override void WriteValue(DateTimeOffset value)
		{
			_writer.WriteValue(value);
		}

		public override void WriteValue(double value)
		{
			_writer.WriteValue(value);
		}

		public override void WriteValue(int value)
		{
			_writer.WriteValue(value);
		}

		public override void WriteValue(long value)
		{
			_writer.WriteValue(value);
		}

		public override void WriteXmlnsAttribute(string prefix, string namespaceUri)
		{
			if (namespaceUri == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("namespaceUri");
			}
			if (prefix == null)
			{
				if (LookupPrefix(namespaceUri) != null)
				{
					return;
				}
				if (namespaceUri.Length == 0)
				{
					prefix = string.Empty;
				}
				else
				{
					string text = _depth.ToString(NumberFormatInfo.InvariantInfo);
					string text2 = _prefix.ToString(NumberFormatInfo.InvariantInfo);
					prefix = "d" + text + "p" + text2;
				}
			}
			WriteAttributeString("xmlns", prefix, null, namespaceUri);
		}
	}

	private static readonly Encoding s_UTF8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	public virtual bool CanCanonicalize => false;

	public static XmlDictionaryWriter CreateBinaryWriter(Stream stream)
	{
		return CreateBinaryWriter(stream, null);
	}

	public static XmlDictionaryWriter CreateBinaryWriter(Stream stream, IXmlDictionary? dictionary)
	{
		return CreateBinaryWriter(stream, dictionary, null);
	}

	public static XmlDictionaryWriter CreateBinaryWriter(Stream stream, IXmlDictionary? dictionary, XmlBinaryWriterSession? session)
	{
		return CreateBinaryWriter(stream, dictionary, session, ownsStream: true);
	}

	public static XmlDictionaryWriter CreateBinaryWriter(Stream stream, IXmlDictionary? dictionary, XmlBinaryWriterSession? session, bool ownsStream)
	{
		XmlBinaryWriter xmlBinaryWriter = new XmlBinaryWriter();
		xmlBinaryWriter.SetOutput(stream, dictionary, session, ownsStream);
		return xmlBinaryWriter;
	}

	public static XmlDictionaryWriter CreateTextWriter(Stream stream)
	{
		return CreateTextWriter(stream, s_UTF8Encoding, ownsStream: true);
	}

	public static XmlDictionaryWriter CreateTextWriter(Stream stream, Encoding encoding)
	{
		return CreateTextWriter(stream, encoding, ownsStream: true);
	}

	public static XmlDictionaryWriter CreateTextWriter(Stream stream, Encoding encoding, bool ownsStream)
	{
		XmlUTF8TextWriter xmlUTF8TextWriter = new XmlUTF8TextWriter();
		xmlUTF8TextWriter.SetOutput(stream, encoding, ownsStream);
		return new XmlDictionaryAsyncCheckWriter(xmlUTF8TextWriter);
	}

	public static XmlDictionaryWriter CreateMtomWriter(Stream stream, Encoding encoding, int maxSizeInBytes, string startInfo)
	{
		return CreateMtomWriter(stream, encoding, maxSizeInBytes, startInfo, null, null, writeMessageHeaders: true, ownsStream: true);
	}

	public static XmlDictionaryWriter CreateMtomWriter(Stream stream, Encoding encoding, int maxSizeInBytes, string startInfo, string? boundary, string? startUri, bool writeMessageHeaders, bool ownsStream)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_MtomEncoding);
	}

	public static XmlDictionaryWriter CreateDictionaryWriter(XmlWriter writer)
	{
		if (writer == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
		}
		XmlDictionaryWriter xmlDictionaryWriter = writer as XmlDictionaryWriter;
		if (xmlDictionaryWriter == null)
		{
			xmlDictionaryWriter = new XmlWrappedWriter(writer);
		}
		return xmlDictionaryWriter;
	}

	public override Task WriteBase64Async(byte[] buffer, int index, int count)
	{
		WriteBase64(buffer, index, count);
		return Task.CompletedTask;
	}

	public void WriteStartElement(XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
	{
		WriteStartElement(null, localName, namespaceUri);
	}

	public virtual void WriteStartElement(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
	{
		WriteStartElement(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri));
	}

	public void WriteStartAttribute(XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
	{
		WriteStartAttribute(null, localName, namespaceUri);
	}

	public virtual void WriteStartAttribute(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
	{
		WriteStartAttribute(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri));
	}

	public void WriteAttributeString(XmlDictionaryString localName, XmlDictionaryString? namespaceUri, string? value)
	{
		WriteAttributeString(null, localName, namespaceUri, value);
	}

	public virtual void WriteXmlnsAttribute(string? prefix, string namespaceUri)
	{
		if (namespaceUri == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("namespaceUri");
		}
		if (prefix == null)
		{
			if (LookupPrefix(namespaceUri) != null)
			{
				return;
			}
			prefix = ((namespaceUri.Length == 0) ? string.Empty : $"d{namespaceUri.Length}");
		}
		WriteAttributeString("xmlns", prefix, null, namespaceUri);
	}

	public virtual void WriteXmlnsAttribute(string? prefix, XmlDictionaryString namespaceUri)
	{
		WriteXmlnsAttribute(prefix, XmlDictionaryString.GetString(namespaceUri));
	}

	public virtual void WriteXmlAttribute(string localName, string? value)
	{
		WriteAttributeString("xml", localName, null, value);
	}

	public virtual void WriteXmlAttribute(XmlDictionaryString localName, XmlDictionaryString? value)
	{
		WriteXmlAttribute(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(value));
	}

	public void WriteAttributeString(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, string? value)
	{
		WriteStartAttribute(prefix, localName, namespaceUri);
		WriteString(value);
		WriteEndAttribute();
	}

	public void WriteElementString(XmlDictionaryString localName, XmlDictionaryString? namespaceUri, string? value)
	{
		WriteElementString(null, localName, namespaceUri, value);
	}

	public void WriteElementString(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, string? value)
	{
		WriteStartElement(prefix, localName, namespaceUri);
		WriteString(value);
		WriteEndElement();
	}

	public virtual void WriteString(XmlDictionaryString? value)
	{
		WriteString(XmlDictionaryString.GetString(value));
	}

	public virtual void WriteQualifiedName(XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
	{
		if (localName == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
		}
		if (namespaceUri == null)
		{
			namespaceUri = XmlDictionaryString.Empty;
		}
		WriteQualifiedName(localName.Value, namespaceUri.Value);
	}

	public virtual void WriteValue(XmlDictionaryString? value)
	{
		WriteValue(XmlDictionaryString.GetString(value));
	}

	public virtual void WriteValue(UniqueId value)
	{
		if (value == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
		}
		WriteString(value.ToString());
	}

	public virtual void WriteValue(Guid value)
	{
		WriteString(value.ToString());
	}

	public virtual void WriteValue(TimeSpan value)
	{
		WriteString(XmlConvert.ToString(value));
	}

	public virtual void WriteValue(IStreamProvider value)
	{
		if (value == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("value"));
		}
		Stream stream = value.GetStream();
		if (stream == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(System.SR.XmlInvalidStream));
		}
		int num = 256;
		int num2 = 0;
		byte[] buffer = new byte[num];
		while (true)
		{
			num2 = stream.Read(buffer, 0, num);
			if (num2 <= 0)
			{
				break;
			}
			WriteBase64(buffer, 0, num2);
			if (num < 65536 && num2 == num)
			{
				num *= 16;
				buffer = new byte[num];
			}
		}
		value.ReleaseStream(stream);
	}

	public virtual Task WriteValueAsync(IStreamProvider value)
	{
		WriteValue(value);
		return Task.CompletedTask;
	}

	public virtual void StartCanonicalization(Stream stream, bool includeComments, string[]? inclusivePrefixes)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
	}

	public virtual void EndCanonicalization()
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
	}

	private void WriteElementNode(XmlDictionaryReader reader, bool defattr)
	{
		if (reader.TryGetLocalNameAsDictionaryString(out XmlDictionaryString localName) && reader.TryGetNamespaceUriAsDictionaryString(out XmlDictionaryString namespaceUri))
		{
			WriteStartElement(reader.Prefix, localName, namespaceUri);
		}
		else
		{
			WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
		}
		if ((defattr || !reader.IsDefault) && reader.MoveToFirstAttribute())
		{
			do
			{
				if (reader.TryGetLocalNameAsDictionaryString(out localName) && reader.TryGetNamespaceUriAsDictionaryString(out namespaceUri))
				{
					WriteStartAttribute(reader.Prefix, localName, namespaceUri);
				}
				else
				{
					WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
				}
				while (reader.ReadAttributeValue())
				{
					if (reader.NodeType == XmlNodeType.EntityReference)
					{
						WriteEntityRef(reader.Name);
					}
					else
					{
						WriteTextNode(reader, isAttribute: true);
					}
				}
				WriteEndAttribute();
			}
			while (reader.MoveToNextAttribute());
			reader.MoveToElement();
		}
		if (reader.IsEmptyElement)
		{
			WriteEndElement();
		}
	}

	private void WriteArrayNode(XmlDictionaryReader reader, string prefix, string localName, string namespaceUri, Type type)
	{
		if (type == typeof(bool))
		{
			BooleanArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(short))
		{
			Int16ArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(int))
		{
			Int32ArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(long))
		{
			Int64ArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(float))
		{
			SingleArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(double))
		{
			DoubleArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(decimal))
		{
			DecimalArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(DateTime))
		{
			DateTimeArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(Guid))
		{
			GuidArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(TimeSpan))
		{
			TimeSpanArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		WriteElementNode(reader, defattr: false);
		reader.Read();
	}

	private void WriteArrayNode(XmlDictionaryReader reader, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, Type type)
	{
		if (type == typeof(bool))
		{
			BooleanArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(short))
		{
			Int16ArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(int))
		{
			Int32ArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(long))
		{
			Int64ArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(float))
		{
			SingleArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(double))
		{
			DoubleArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(decimal))
		{
			DecimalArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(DateTime))
		{
			DateTimeArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(Guid))
		{
			GuidArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		if (type == typeof(TimeSpan))
		{
			TimeSpanArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
			return;
		}
		WriteElementNode(reader, defattr: false);
		reader.Read();
	}

	private void WriteArrayNode(XmlDictionaryReader reader, Type type)
	{
		if (reader.TryGetLocalNameAsDictionaryString(out XmlDictionaryString localName) && reader.TryGetNamespaceUriAsDictionaryString(out XmlDictionaryString namespaceUri))
		{
			WriteArrayNode(reader, reader.Prefix, localName, namespaceUri, type);
		}
		else
		{
			WriteArrayNode(reader, reader.Prefix, reader.LocalName, reader.NamespaceURI, type);
		}
	}

	protected virtual void WriteTextNode(XmlDictionaryReader reader, bool isAttribute)
	{
		if (reader.TryGetValueAsDictionaryString(out XmlDictionaryString value))
		{
			WriteString(value);
		}
		else
		{
			WriteString(reader.Value);
		}
		if (!isAttribute)
		{
			reader.Read();
		}
	}

	public override void WriteNode(XmlReader reader, bool defattr)
	{
		if (reader is XmlDictionaryReader reader2)
		{
			WriteNode(reader2, defattr);
		}
		else
		{
			base.WriteNode(reader, defattr);
		}
	}

	public virtual void WriteNode(XmlDictionaryReader reader, bool defattr)
	{
		if (reader == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("reader"));
		}
		int num = ((reader.NodeType == XmlNodeType.None) ? (-1) : reader.Depth);
		do
		{
			XmlNodeType nodeType = reader.NodeType;
			if (nodeType == XmlNodeType.Text || nodeType == XmlNodeType.Whitespace || nodeType == XmlNodeType.SignificantWhitespace)
			{
				WriteTextNode(reader, isAttribute: false);
				continue;
			}
			if (reader.Depth > num && reader.IsStartArray(out Type type))
			{
				WriteArrayNode(reader, type);
				continue;
			}
			switch (nodeType)
			{
			case XmlNodeType.Element:
				WriteElementNode(reader, defattr);
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
			if (!reader.Read())
			{
				break;
			}
		}
		while (num < reader.Depth || (num == reader.Depth && reader.NodeType == XmlNodeType.EndElement));
	}

	private void CheckArray(Array array, int offset, int count)
	{
		if (array == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("array"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (offset > array.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, array.Length)));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > array.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, array.Length - offset)));
		}
	}

	public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, bool[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		for (int i = 0; i < count; i++)
		{
			WriteStartElement(prefix, localName, namespaceUri);
			WriteValue(array[offset + i]);
			WriteEndElement();
		}
	}

	public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, bool[] array, int offset, int count)
	{
		WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
	}

	public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, short[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		for (int i = 0; i < count; i++)
		{
			WriteStartElement(prefix, localName, namespaceUri);
			WriteValue(array[offset + i]);
			WriteEndElement();
		}
	}

	public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, short[] array, int offset, int count)
	{
		WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
	}

	public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, int[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		for (int i = 0; i < count; i++)
		{
			WriteStartElement(prefix, localName, namespaceUri);
			WriteValue(array[offset + i]);
			WriteEndElement();
		}
	}

	public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, int[] array, int offset, int count)
	{
		WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
	}

	public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, long[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		for (int i = 0; i < count; i++)
		{
			WriteStartElement(prefix, localName, namespaceUri);
			WriteValue(array[offset + i]);
			WriteEndElement();
		}
	}

	public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, long[] array, int offset, int count)
	{
		WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
	}

	public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, float[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		for (int i = 0; i < count; i++)
		{
			WriteStartElement(prefix, localName, namespaceUri);
			WriteValue(array[offset + i]);
			WriteEndElement();
		}
	}

	public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, float[] array, int offset, int count)
	{
		WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
	}

	public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, double[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		for (int i = 0; i < count; i++)
		{
			WriteStartElement(prefix, localName, namespaceUri);
			WriteValue(array[offset + i]);
			WriteEndElement();
		}
	}

	public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, double[] array, int offset, int count)
	{
		WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
	}

	public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, decimal[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		for (int i = 0; i < count; i++)
		{
			WriteStartElement(prefix, localName, namespaceUri);
			WriteValue(array[offset + i]);
			WriteEndElement();
		}
	}

	public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, decimal[] array, int offset, int count)
	{
		WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
	}

	public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, DateTime[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		for (int i = 0; i < count; i++)
		{
			WriteStartElement(prefix, localName, namespaceUri);
			WriteValue(array[offset + i]);
			WriteEndElement();
		}
	}

	public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, DateTime[] array, int offset, int count)
	{
		WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
	}

	public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, Guid[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		for (int i = 0; i < count; i++)
		{
			WriteStartElement(prefix, localName, namespaceUri);
			WriteValue(array[offset + i]);
			WriteEndElement();
		}
	}

	public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, Guid[] array, int offset, int count)
	{
		WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
	}

	public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, TimeSpan[] array, int offset, int count)
	{
		CheckArray(array, offset, count);
		for (int i = 0; i < count; i++)
		{
			WriteStartElement(prefix, localName, namespaceUri);
			WriteValue(array[offset + i]);
			WriteEndElement();
		}
	}

	public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, TimeSpan[] array, int offset, int count)
	{
		WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && WriteState != WriteState.Closed)
		{
			Close();
		}
	}

	public override void Close()
	{
	}
}
