using System.IO;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class XmlJsonWriter : XmlDictionaryWriter, IXmlJsonWriterInitializer
{
	private enum JsonDataType
	{
		None,
		Null,
		Boolean,
		Number,
		String,
		Object,
		Array
	}

	[Flags]
	private enum NameState
	{
		None = 0,
		IsWritingNameWithMapping = 1,
		IsWritingNameAttribute = 2,
		WrittenNameWithMapping = 4
	}

	private sealed class JsonNodeWriter : XmlUTF8NodeWriter
	{
		internal unsafe void WriteChars(char* chars, int charCount)
		{
			UnsafeWriteUTF8Chars(chars, charCount);
		}
	}

	private const char BACK_SLASH = '\\';

	private const char FORWARD_SLASH = '/';

	private const char HIGH_SURROGATE_START = '\ud800';

	private const char LOW_SURROGATE_END = '\udfff';

	private const char MAX_CHAR = '\ufffe';

	private const char WHITESPACE = ' ';

	private const char CARRIAGE_RETURN = '\r';

	private const char NEWLINE = '\n';

	private const string xmlNamespace = "http://www.w3.org/XML/1998/namespace";

	private const string xmlnsNamespace = "http://www.w3.org/2000/xmlns/";

	private static readonly string[] s_escapedJsonStringTable = new string[32]
	{
		"\\u0000", "\\u0001", "\\u0002", "\\u0003", "\\u0004", "\\u0005", "\\u0006", "\\u0007", "\\b", "\\t",
		"\\n", "\\u000b", "\\f", "\\r", "\\u000e", "\\u000f", "\\u0010", "\\u0011", "\\u0012", "\\u0013",
		"\\u0014", "\\u0015", "\\u0016", "\\u0017", "\\u0018", "\\u0019", "\\u001a", "\\u001b", "\\u001c", "\\u001d",
		"\\u001e", "\\u001f"
	};

	private static BinHexEncoding s_binHexEncoding;

	private string _attributeText;

	private JsonDataType _dataType;

	private int _depth;

	private bool _endElementBuffer;

	private bool _isWritingDataTypeAttribute;

	private bool _isWritingServerTypeAttribute;

	private bool _isWritingXmlnsAttribute;

	private bool _isWritingXmlnsAttributeDefaultNs;

	private NameState _nameState;

	private JsonNodeType _nodeType;

	private JsonNodeWriter _nodeWriter;

	private JsonNodeType[] _scopes;

	private string _serverTypeValue;

	private WriteState _writeState;

	private bool _wroteServerTypeAttribute;

	private readonly bool _indent;

	private readonly string _indentChars;

	private int _indentLevel;

	public override XmlWriterSettings Settings => null;

	public override WriteState WriteState
	{
		get
		{
			if (_writeState == WriteState.Closed)
			{
				return WriteState.Closed;
			}
			if (HasOpenAttribute)
			{
				return WriteState.Attribute;
			}
			switch (_nodeType)
			{
			case JsonNodeType.None:
				return WriteState.Start;
			case JsonNodeType.Element:
				return WriteState.Element;
			case JsonNodeType.EndElement:
			case JsonNodeType.QuotedText:
			case JsonNodeType.StandaloneText:
				return WriteState.Content;
			default:
				return WriteState.Error;
			}
		}
	}

	public override string XmlLang => null;

	public override XmlSpace XmlSpace => XmlSpace.None;

	private static BinHexEncoding BinHexEncoding
	{
		get
		{
			if (s_binHexEncoding == null)
			{
				s_binHexEncoding = new BinHexEncoding();
			}
			return s_binHexEncoding;
		}
	}

	private bool HasOpenAttribute
	{
		get
		{
			if (!_isWritingDataTypeAttribute && !_isWritingServerTypeAttribute && !IsWritingNameAttribute)
			{
				return _isWritingXmlnsAttribute;
			}
			return true;
		}
	}

	private bool IsClosed => WriteState == WriteState.Closed;

	private bool IsWritingCollection
	{
		get
		{
			if (_depth > 0)
			{
				return _scopes[_depth] == JsonNodeType.Collection;
			}
			return false;
		}
	}

	private bool IsWritingNameAttribute => (_nameState & NameState.IsWritingNameAttribute) == NameState.IsWritingNameAttribute;

	private bool IsWritingNameWithMapping => (_nameState & NameState.IsWritingNameWithMapping) == NameState.IsWritingNameWithMapping;

	private bool WrittenNameWithMapping => (_nameState & NameState.WrittenNameWithMapping) == NameState.WrittenNameWithMapping;

	public XmlJsonWriter()
		: this(indent: false, null)
	{
	}

	public XmlJsonWriter(bool indent, string indentChars)
	{
		_indent = indent;
		if (indent)
		{
			if (indentChars == null)
			{
				throw new ArgumentNullException("indentChars");
			}
			_indentChars = indentChars;
		}
		InitializeWriter();
	}

	public override void Close()
	{
		if (!IsClosed)
		{
			try
			{
				WriteEndDocument();
			}
			finally
			{
				try
				{
					_nodeWriter.Flush();
					_nodeWriter.Close();
				}
				finally
				{
					_writeState = WriteState.Closed;
					if (_depth != 0)
					{
						_depth = 0;
					}
				}
			}
		}
		base.Close();
	}

	public override void Flush()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		_nodeWriter.Flush();
	}

	public override string LookupPrefix(string ns)
	{
		if (ns == null)
		{
			throw new ArgumentNullException("ns");
		}
		if (ns == "http://www.w3.org/2000/xmlns/")
		{
			return "xmlns";
		}
		if (ns == "http://www.w3.org/XML/1998/namespace")
		{
			return "xml";
		}
		if (ns.Length == 0)
		{
			return string.Empty;
		}
		return null;
	}

	public void SetOutput(Stream stream, Encoding encoding, bool ownsStream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (encoding.WebName != Encoding.UTF8.WebName)
		{
			stream = new JsonEncodingStreamWrapper(stream, encoding, isReader: false);
		}
		else
		{
			encoding = null;
		}
		if (_nodeWriter == null)
		{
			_nodeWriter = new JsonNodeWriter();
		}
		_nodeWriter.SetOutput(stream, ownsStream, encoding);
		InitializeWriter();
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, bool[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, short[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, int[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, long[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, float[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, double[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, decimal[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, DateTime[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, Guid[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, TimeSpan[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, bool[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, decimal[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, double[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, float[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, int[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, long[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, short[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, DateTime[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, Guid[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, TimeSpan[] array, int offset, int count)
	{
		throw new NotSupportedException(System.SR.JsonWriteArrayNotSupported);
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ValueMustBeNonNegative);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative);
		}
		if (count > buffer.Length - index)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.JsonSizeExceedsRemainingBufferSpace, buffer.Length - index));
		}
		StartText();
		_nodeWriter.WriteBase64Text(buffer, 0, buffer, index, count);
	}

	public override void WriteBinHex(byte[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ValueMustBeNonNegative);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative);
		}
		if (count > buffer.Length - index)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.JsonSizeExceedsRemainingBufferSpace, buffer.Length - index));
		}
		StartText();
		WriteEscapedJsonString(BinHexEncoding.GetString(buffer, index, count));
	}

	public override void WriteCData(string text)
	{
		WriteString(text);
	}

	public override void WriteCharEntity(char ch)
	{
		WriteString(ch.ToString());
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ValueMustBeNonNegative);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative);
		}
		if (count > buffer.Length - index)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.JsonSizeExceedsRemainingBufferSpace, buffer.Length - index));
		}
		WriteString(new string(buffer, index, count));
	}

	public override void WriteComment(string text)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.JsonMethodNotSupported, "WriteComment"));
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.JsonMethodNotSupported, "WriteDocType"));
	}

	public override void WriteEndAttribute()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (!HasOpenAttribute)
		{
			throw new XmlException(System.SR.JsonNoMatchingStartAttribute);
		}
		if (_isWritingDataTypeAttribute)
		{
			switch (_attributeText)
			{
			case "number":
				ThrowIfServerTypeWritten("number");
				_dataType = JsonDataType.Number;
				break;
			case "string":
				ThrowIfServerTypeWritten("string");
				_dataType = JsonDataType.String;
				break;
			case "array":
				ThrowIfServerTypeWritten("array");
				_dataType = JsonDataType.Array;
				break;
			case "object":
				_dataType = JsonDataType.Object;
				break;
			case "null":
				ThrowIfServerTypeWritten("null");
				_dataType = JsonDataType.Null;
				break;
			case "boolean":
				ThrowIfServerTypeWritten("boolean");
				_dataType = JsonDataType.Boolean;
				break;
			default:
				throw new XmlException(System.SR.Format(System.SR.JsonUnexpectedAttributeValue, _attributeText));
			}
			_attributeText = null;
			_isWritingDataTypeAttribute = false;
			if (!IsWritingNameWithMapping || WrittenNameWithMapping)
			{
				WriteDataTypeServerType();
			}
		}
		else if (_isWritingServerTypeAttribute)
		{
			_serverTypeValue = _attributeText;
			_attributeText = null;
			_isWritingServerTypeAttribute = false;
			if ((!IsWritingNameWithMapping || WrittenNameWithMapping) && _dataType == JsonDataType.Object)
			{
				WriteServerTypeAttribute();
			}
		}
		else if (IsWritingNameAttribute)
		{
			WriteJsonElementName(_attributeText);
			_attributeText = null;
			_nameState = NameState.IsWritingNameWithMapping | NameState.WrittenNameWithMapping;
			WriteDataTypeServerType();
		}
		else if (_isWritingXmlnsAttribute)
		{
			if (!string.IsNullOrEmpty(_attributeText) && _isWritingXmlnsAttributeDefaultNs)
			{
				throw new ArgumentException(System.SR.Format(System.SR.JsonNamespaceMustBeEmpty, _attributeText));
			}
			_attributeText = null;
			_isWritingXmlnsAttribute = false;
			_isWritingXmlnsAttributeDefaultNs = false;
		}
	}

	public override void WriteEndDocument()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_nodeType != 0)
		{
			while (_depth > 0)
			{
				WriteEndElement();
			}
		}
	}

	public override void WriteEndElement()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_depth == 0)
		{
			throw new XmlException(System.SR.JsonEndElementNoOpenNodes);
		}
		if (HasOpenAttribute)
		{
			throw new XmlException(System.SR.Format(System.SR.JsonOpenAttributeMustBeClosedFirst, "WriteEndElement"));
		}
		_endElementBuffer = false;
		JsonNodeType jsonNodeType = ExitScope();
		if (jsonNodeType == JsonNodeType.Collection)
		{
			_indentLevel--;
			if (_indent)
			{
				if (_nodeType == JsonNodeType.Element)
				{
					_nodeWriter.WriteText(32);
				}
				else
				{
					WriteNewLine();
					WriteIndent();
				}
			}
			_nodeWriter.WriteText(93);
			jsonNodeType = ExitScope();
		}
		else if (_nodeType == JsonNodeType.QuotedText)
		{
			WriteJsonQuote();
		}
		else if (_nodeType == JsonNodeType.Element)
		{
			if (_dataType == JsonDataType.None && _serverTypeValue != null)
			{
				throw new XmlException(System.SR.Format(System.SR.JsonMustSpecifyDataType, "type", "object", "__type"));
			}
			if (IsWritingNameWithMapping && !WrittenNameWithMapping)
			{
				throw new XmlException(System.SR.Format(System.SR.JsonMustSpecifyDataType, "item", string.Empty, "item"));
			}
			if (_dataType == JsonDataType.None || _dataType == JsonDataType.String)
			{
				_nodeWriter.WriteText(34);
				_nodeWriter.WriteText(34);
			}
		}
		if (_depth != 0)
		{
			switch (jsonNodeType)
			{
			case JsonNodeType.Element:
				_endElementBuffer = true;
				break;
			case JsonNodeType.Object:
				_indentLevel--;
				if (_indent)
				{
					if (_nodeType == JsonNodeType.Element)
					{
						_nodeWriter.WriteText(32);
					}
					else
					{
						WriteNewLine();
						WriteIndent();
					}
				}
				_nodeWriter.WriteText(125);
				if (_depth > 0 && _scopes[_depth] == JsonNodeType.Element)
				{
					ExitScope();
					_endElementBuffer = true;
				}
				break;
			}
		}
		_dataType = JsonDataType.None;
		_nodeType = JsonNodeType.EndElement;
		_nameState = NameState.None;
		_wroteServerTypeAttribute = false;
	}

	public override void WriteEntityRef(string name)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.JsonMethodNotSupported, "WriteEntityRef"));
	}

	public override void WriteFullEndElement()
	{
		WriteEndElement();
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (!name.Equals("xml", StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException(System.SR.JsonXmlProcessingInstructionNotSupported, "name");
		}
		if (WriteState != 0)
		{
			throw new XmlException(System.SR.JsonXmlInvalidDeclaration);
		}
	}

	public override void WriteQualifiedName(string localName, string ns)
	{
		if (localName == null)
		{
			throw new ArgumentNullException("localName");
		}
		if (localName.Length == 0)
		{
			throw new ArgumentException(System.SR.JsonInvalidLocalNameEmpty, "localName");
		}
		if (ns == null)
		{
			ns = string.Empty;
		}
		base.WriteQualifiedName(localName, ns);
	}

	public override void WriteRaw(string data)
	{
		WriteString(data);
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ValueMustBeNonNegative);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative);
		}
		if (count > buffer.Length - index)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.JsonSizeExceedsRemainingBufferSpace, buffer.Length - index));
		}
		WriteString(new string(buffer, index, count));
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (!string.IsNullOrEmpty(prefix))
		{
			if (!IsWritingNameWithMapping || !(prefix == "xmlns"))
			{
				throw new ArgumentException(System.SR.Format(System.SR.JsonPrefixMustBeNullOrEmpty, prefix), "prefix");
			}
			if (ns != null && ns != "http://www.w3.org/2000/xmlns/")
			{
				throw new ArgumentException(System.SR.Format(System.SR.XmlPrefixBoundToNamespace, "xmlns", "http://www.w3.org/2000/xmlns/", ns), "ns");
			}
		}
		else if (IsWritingNameWithMapping && ns == "http://www.w3.org/2000/xmlns/" && localName != "xmlns")
		{
			prefix = "xmlns";
		}
		if (!string.IsNullOrEmpty(ns))
		{
			if (IsWritingNameWithMapping && ns == "http://www.w3.org/2000/xmlns/")
			{
				prefix = "xmlns";
			}
			else
			{
				if (!string.IsNullOrEmpty(prefix) || !(localName == "xmlns") || !(ns == "http://www.w3.org/2000/xmlns/"))
				{
					throw new ArgumentException(System.SR.Format(System.SR.JsonNamespaceMustBeEmpty, ns), "ns");
				}
				prefix = "xmlns";
				_isWritingXmlnsAttributeDefaultNs = true;
			}
		}
		if (localName == null)
		{
			throw new ArgumentNullException("localName");
		}
		if (localName.Length == 0)
		{
			throw new ArgumentException(System.SR.JsonInvalidLocalNameEmpty, "localName");
		}
		if (_nodeType != JsonNodeType.Element && !_wroteServerTypeAttribute)
		{
			throw new XmlException(System.SR.JsonAttributeMustHaveElement);
		}
		if (HasOpenAttribute)
		{
			throw new XmlException(System.SR.Format(System.SR.JsonOpenAttributeMustBeClosedFirst, "WriteStartAttribute"));
		}
		if (prefix == "xmlns")
		{
			_isWritingXmlnsAttribute = true;
			return;
		}
		switch (localName)
		{
		case "type":
			if (_dataType != 0)
			{
				throw new XmlException(System.SR.Format(System.SR.JsonAttributeAlreadyWritten, "type"));
			}
			_isWritingDataTypeAttribute = true;
			break;
		case "__type":
			if (_serverTypeValue != null)
			{
				throw new XmlException(System.SR.Format(System.SR.JsonAttributeAlreadyWritten, "__type"));
			}
			if (_dataType != 0 && _dataType != JsonDataType.Object)
			{
				throw new XmlException(System.SR.Format(System.SR.JsonServerTypeSpecifiedForInvalidDataType, "__type", "type", _dataType.ToString().ToLowerInvariant(), "object"));
			}
			_isWritingServerTypeAttribute = true;
			break;
		case "item":
			if (WrittenNameWithMapping)
			{
				throw new XmlException(System.SR.Format(System.SR.JsonAttributeAlreadyWritten, "item"));
			}
			if (!IsWritingNameWithMapping)
			{
				throw new XmlException(System.SR.JsonEndElementNoOpenNodes);
			}
			_nameState |= NameState.IsWritingNameAttribute;
			break;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.JsonUnexpectedAttributeLocalName, localName), "localName");
		}
	}

	public override void WriteStartDocument(bool standalone)
	{
		WriteStartDocument();
	}

	public override void WriteStartDocument()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (WriteState != 0)
		{
			throw new XmlException(System.SR.Format(System.SR.JsonInvalidWriteState, "WriteStartDocument", WriteState.ToString()));
		}
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		if (localName == null)
		{
			throw new ArgumentNullException("localName");
		}
		if (localName.Length == 0)
		{
			throw new ArgumentException(System.SR.JsonInvalidLocalNameEmpty, "localName");
		}
		if (!string.IsNullOrEmpty(prefix) && (string.IsNullOrEmpty(ns) || !TrySetWritingNameWithMapping(localName, ns)))
		{
			throw new ArgumentException(System.SR.Format(System.SR.JsonPrefixMustBeNullOrEmpty, prefix), "prefix");
		}
		if (!string.IsNullOrEmpty(ns) && !TrySetWritingNameWithMapping(localName, ns))
		{
			throw new ArgumentException(System.SR.Format(System.SR.JsonNamespaceMustBeEmpty, ns), "ns");
		}
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (HasOpenAttribute)
		{
			throw new XmlException(System.SR.Format(System.SR.JsonOpenAttributeMustBeClosedFirst, "WriteStartElement"));
		}
		if (_nodeType != 0 && _depth == 0)
		{
			throw new XmlException(System.SR.JsonMultipleRootElementsNotAllowedOnWriter);
		}
		switch (_nodeType)
		{
		case JsonNodeType.None:
			if (!localName.Equals("root"))
			{
				throw new XmlException(System.SR.Format(System.SR.JsonInvalidRootElementName, localName, "root"));
			}
			EnterScope(JsonNodeType.Element);
			break;
		case JsonNodeType.Element:
			if (_dataType != JsonDataType.Array && _dataType != JsonDataType.Object)
			{
				throw new XmlException(System.SR.JsonNodeTypeArrayOrObjectNotSpecified);
			}
			if (_indent)
			{
				WriteNewLine();
				WriteIndent();
			}
			if (!IsWritingCollection)
			{
				if (_nameState != NameState.IsWritingNameWithMapping)
				{
					WriteJsonElementName(localName);
				}
			}
			else if (!localName.Equals("item"))
			{
				throw new XmlException(System.SR.Format(System.SR.JsonInvalidItemNameForArrayElement, localName, "item"));
			}
			EnterScope(JsonNodeType.Element);
			break;
		case JsonNodeType.EndElement:
			if (_endElementBuffer)
			{
				_nodeWriter.WriteText(44);
			}
			if (_indent)
			{
				WriteNewLine();
				WriteIndent();
			}
			if (!IsWritingCollection)
			{
				if (_nameState != NameState.IsWritingNameWithMapping)
				{
					WriteJsonElementName(localName);
				}
			}
			else if (!localName.Equals("item"))
			{
				throw new XmlException(System.SR.Format(System.SR.JsonInvalidItemNameForArrayElement, localName, "item"));
			}
			EnterScope(JsonNodeType.Element);
			break;
		default:
			throw new XmlException(System.SR.JsonInvalidStartElementCall);
		}
		_isWritingDataTypeAttribute = false;
		_isWritingServerTypeAttribute = false;
		_isWritingXmlnsAttribute = false;
		_wroteServerTypeAttribute = false;
		_serverTypeValue = null;
		_dataType = JsonDataType.None;
		_nodeType = JsonNodeType.Element;
	}

	public override void WriteString(string text)
	{
		if (HasOpenAttribute && text != null)
		{
			_attributeText += text;
			return;
		}
		if (text == null)
		{
			text = string.Empty;
		}
		if ((_dataType != JsonDataType.Array && _dataType != JsonDataType.Object && _nodeType != JsonNodeType.EndElement) || !XmlConverter.IsWhitespace(text))
		{
			StartText();
			WriteEscapedJsonString(text);
		}
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		Span<char> span = stackalloc char[2] { highChar, lowChar };
		WriteString(new string(span));
	}

	public override void WriteValue(bool value)
	{
		StartText();
		_nodeWriter.WriteBoolText(value);
	}

	public override void WriteValue(decimal value)
	{
		StartText();
		_nodeWriter.WriteDecimalText(value);
	}

	public override void WriteValue(double value)
	{
		StartText();
		_nodeWriter.WriteDoubleText(value);
	}

	public override void WriteValue(float value)
	{
		StartText();
		_nodeWriter.WriteFloatText(value);
	}

	public override void WriteValue(int value)
	{
		StartText();
		_nodeWriter.WriteInt32Text(value);
	}

	public override void WriteValue(long value)
	{
		StartText();
		_nodeWriter.WriteInt64Text(value);
	}

	public override void WriteValue(Guid value)
	{
		StartText();
		_nodeWriter.WriteGuidText(value);
	}

	public override void WriteValue(DateTime value)
	{
		StartText();
		_nodeWriter.WriteDateTimeText(value);
	}

	public override void WriteValue(string value)
	{
		WriteString(value);
	}

	public override void WriteValue(TimeSpan value)
	{
		StartText();
		_nodeWriter.WriteTimeSpanText(value);
	}

	public override void WriteValue(UniqueId value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		StartText();
		_nodeWriter.WriteUniqueIdText(value);
	}

	public override void WriteValue(object value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (value is Array)
		{
			WriteValue((Array)value);
		}
		else if (value is IStreamProvider)
		{
			WriteValue((IStreamProvider)value);
		}
		else
		{
			WritePrimitiveValue(value);
		}
	}

	public override void WriteWhitespace(string ws)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (ws == null)
		{
			throw new ArgumentNullException("ws");
		}
		for (int i = 0; i < ws.Length; i++)
		{
			char c = ws[i];
			if (c != ' ' && c != '\t' && c != '\n' && c != '\r')
			{
				throw new ArgumentException(System.SR.Format(System.SR.JsonOnlyWhitespace, c.ToString(), "WriteWhitespace"), "ws");
			}
		}
		WriteString(ws);
	}

	public override void WriteXmlAttribute(string localName, string value)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.JsonMethodNotSupported, "WriteXmlAttribute"));
	}

	public override void WriteXmlAttribute(XmlDictionaryString localName, XmlDictionaryString value)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.JsonMethodNotSupported, "WriteXmlAttribute"));
	}

	public override void WriteXmlnsAttribute(string prefix, string namespaceUri)
	{
		if (!IsWritingNameWithMapping)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.JsonMethodNotSupported, "WriteXmlnsAttribute"));
		}
	}

	public override void WriteXmlnsAttribute(string prefix, XmlDictionaryString namespaceUri)
	{
		if (!IsWritingNameWithMapping)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.JsonMethodNotSupported, "WriteXmlnsAttribute"));
		}
	}

	internal static bool CharacterNeedsEscaping(char ch)
	{
		if (ch != '/' && ch != '"' && ch >= ' ' && ch != '\\')
		{
			if (ch >= '\ud800')
			{
				if (ch > '\udfff')
				{
					return ch >= '\ufffe';
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private static void ThrowClosed()
	{
		throw new InvalidOperationException(System.SR.JsonWriterClosed);
	}

	private void CheckText(JsonNodeType nextNodeType)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_depth == 0)
		{
			throw new InvalidOperationException(System.SR.XmlIllegalOutsideRoot);
		}
		if (nextNodeType == JsonNodeType.StandaloneText && _nodeType == JsonNodeType.QuotedText)
		{
			throw new XmlException(System.SR.JsonCannotWriteStandaloneTextAfterQuotedText);
		}
	}

	private void EnterScope(JsonNodeType currentNodeType)
	{
		_depth++;
		if (_scopes == null)
		{
			_scopes = new JsonNodeType[4];
		}
		else if (_scopes.Length == _depth)
		{
			JsonNodeType[] array = new JsonNodeType[_depth * 2];
			Array.Copy(_scopes, array, _depth);
			_scopes = array;
		}
		_scopes[_depth] = currentNodeType;
	}

	private JsonNodeType ExitScope()
	{
		JsonNodeType result = _scopes[_depth];
		_scopes[_depth] = JsonNodeType.None;
		_depth--;
		return result;
	}

	private void InitializeWriter()
	{
		_nodeType = JsonNodeType.None;
		_dataType = JsonDataType.None;
		_isWritingDataTypeAttribute = false;
		_wroteServerTypeAttribute = false;
		_isWritingServerTypeAttribute = false;
		_serverTypeValue = null;
		_attributeText = null;
		if (_depth != 0)
		{
			_depth = 0;
		}
		if (_scopes != null && _scopes.Length > 25)
		{
			_scopes = null;
		}
		_writeState = WriteState.Start;
		_endElementBuffer = false;
		_indentLevel = 0;
	}

	private static bool IsUnicodeNewlineCharacter(char c)
	{
		if (c != '\u0085' && c != '\u2028')
		{
			return c == '\u2029';
		}
		return true;
	}

	private void StartText()
	{
		if (HasOpenAttribute)
		{
			throw new InvalidOperationException(System.SR.JsonMustUseWriteStringForWritingAttributeValues);
		}
		if (_dataType == JsonDataType.None && _serverTypeValue != null)
		{
			throw new XmlException(System.SR.Format(System.SR.JsonMustSpecifyDataType, "type", "object", "__type"));
		}
		if (IsWritingNameWithMapping && !WrittenNameWithMapping)
		{
			throw new XmlException(System.SR.Format(System.SR.JsonMustSpecifyDataType, "item", string.Empty, "item"));
		}
		if (_dataType == JsonDataType.String || _dataType == JsonDataType.None)
		{
			CheckText(JsonNodeType.QuotedText);
			if (_nodeType != JsonNodeType.QuotedText)
			{
				WriteJsonQuote();
			}
			_nodeType = JsonNodeType.QuotedText;
		}
		else if (_dataType == JsonDataType.Number || _dataType == JsonDataType.Boolean)
		{
			CheckText(JsonNodeType.StandaloneText);
			_nodeType = JsonNodeType.StandaloneText;
		}
		else
		{
			ThrowInvalidAttributeContent();
		}
	}

	private void ThrowIfServerTypeWritten(string dataTypeSpecified)
	{
		if (_serverTypeValue != null)
		{
			throw new XmlException(System.SR.Format(System.SR.JsonInvalidDataTypeSpecifiedForServerType, "type", dataTypeSpecified, "__type", "object"));
		}
	}

	private void ThrowInvalidAttributeContent()
	{
		if (HasOpenAttribute)
		{
			throw new XmlException(System.SR.JsonInvalidMethodBetweenStartEndAttribute);
		}
		throw new XmlException(System.SR.Format(System.SR.JsonCannotWriteTextAfterNonTextAttribute, _dataType.ToString().ToLowerInvariant()));
	}

	private bool TrySetWritingNameWithMapping(string localName, string ns)
	{
		if (localName.Equals("item") && ns.Equals("item"))
		{
			_nameState = NameState.IsWritingNameWithMapping;
			return true;
		}
		return false;
	}

	private void WriteDataTypeServerType()
	{
		if (_dataType != 0)
		{
			switch (_dataType)
			{
			case JsonDataType.Array:
				EnterScope(JsonNodeType.Collection);
				_nodeWriter.WriteText(91);
				_indentLevel++;
				break;
			case JsonDataType.Object:
				EnterScope(JsonNodeType.Object);
				_nodeWriter.WriteText(123);
				_indentLevel++;
				break;
			case JsonDataType.Null:
				_nodeWriter.WriteText("null");
				break;
			}
			if (_serverTypeValue != null)
			{
				WriteServerTypeAttribute();
			}
		}
	}

	private unsafe void WriteEscapedJsonString(string str)
	{
		fixed (char* ptr = str)
		{
			int num = 0;
			int i;
			for (i = 0; i < str.Length; i++)
			{
				char c = ptr[i];
				if (c <= '/')
				{
					if (c == '/' || c == '"')
					{
						_nodeWriter.WriteChars(ptr + num, i - num);
						_nodeWriter.WriteText(92);
						_nodeWriter.WriteText(c);
						num = i + 1;
					}
					else if (c < ' ')
					{
						_nodeWriter.WriteChars(ptr + num, i - num);
						_nodeWriter.WriteText(s_escapedJsonStringTable[(uint)c]);
						num = i + 1;
					}
				}
				else if (c == '\\')
				{
					_nodeWriter.WriteChars(ptr + num, i - num);
					_nodeWriter.WriteText(92);
					_nodeWriter.WriteText(c);
					num = i + 1;
				}
				else if ((c >= '\ud800' && (c <= '\udfff' || c >= '\ufffe')) || IsUnicodeNewlineCharacter(c))
				{
					_nodeWriter.WriteChars(ptr + num, i - num);
					_nodeWriter.WriteText(92);
					_nodeWriter.WriteText(117);
					_nodeWriter.WriteText($"{c:x4}");
					num = i + 1;
				}
			}
			if (num < i)
			{
				_nodeWriter.WriteChars(ptr + num, i - num);
			}
		}
	}

	private void WriteIndent()
	{
		for (int i = 0; i < _indentLevel; i++)
		{
			_nodeWriter.WriteText(_indentChars);
		}
	}

	private void WriteNewLine()
	{
		_nodeWriter.WriteText(13);
		_nodeWriter.WriteText(10);
	}

	private void WriteJsonElementName(string localName)
	{
		WriteJsonQuote();
		WriteEscapedJsonString(localName);
		WriteJsonQuote();
		_nodeWriter.WriteText(58);
		if (_indent)
		{
			_nodeWriter.WriteText(32);
		}
	}

	private void WriteJsonQuote()
	{
		_nodeWriter.WriteText(34);
	}

	private void WritePrimitiveValue(object value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (value is ulong)
		{
			WriteValue((ulong)value);
			return;
		}
		if (value is string)
		{
			WriteValue((string)value);
			return;
		}
		if (value is int)
		{
			WriteValue((int)value);
			return;
		}
		if (value is long)
		{
			WriteValue((long)value);
			return;
		}
		if (value is bool)
		{
			WriteValue((bool)value);
			return;
		}
		if (value is double)
		{
			WriteValue((double)value);
			return;
		}
		if (value is DateTime)
		{
			WriteValue((DateTime)value);
			return;
		}
		if (value is float)
		{
			WriteValue((float)value);
			return;
		}
		if (value is decimal)
		{
			WriteValue((decimal)value);
			return;
		}
		if (value is XmlDictionaryString)
		{
			WriteValue((XmlDictionaryString)value);
			return;
		}
		if (value is UniqueId)
		{
			WriteValue((UniqueId)value);
			return;
		}
		if (value is Guid)
		{
			WriteValue((Guid)value);
			return;
		}
		if (value is TimeSpan)
		{
			WriteValue((TimeSpan)value);
			return;
		}
		if (value.GetType().IsArray)
		{
			throw new ArgumentException(System.SR.JsonNestedArraysNotSupported, "value");
		}
		base.WriteValue(value);
	}

	private void WriteServerTypeAttribute()
	{
		string serverTypeValue = _serverTypeValue;
		JsonDataType dataType = _dataType;
		NameState nameState = _nameState;
		WriteStartElement("__type");
		WriteValue(serverTypeValue);
		WriteEndElement();
		_dataType = dataType;
		_nameState = nameState;
		_wroteServerTypeAttribute = true;
	}

	private void WriteValue(ulong value)
	{
		StartText();
		_nodeWriter.WriteUInt64Text(value);
	}

	private void WriteValue(Array array)
	{
		JsonDataType dataType = _dataType;
		_dataType = JsonDataType.String;
		StartText();
		for (int i = 0; i < array.Length; i++)
		{
			if (i != 0)
			{
				_nodeWriter.WriteText(32);
			}
			WritePrimitiveValue(array.GetValue(i));
		}
		_dataType = dataType;
	}
}
