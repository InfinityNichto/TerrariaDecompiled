using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace System.Runtime.Serialization;

internal class XmlReaderDelegator
{
	protected XmlReader reader;

	protected XmlDictionaryReader dictionaryReader;

	protected bool isEndOfEmptyElement;

	internal XmlReader UnderlyingReader => reader;

	internal ExtensionDataReader UnderlyingExtensionDataReader => reader as ExtensionDataReader;

	internal int AttributeCount
	{
		get
		{
			if (!isEndOfEmptyElement)
			{
				return reader.AttributeCount;
			}
			return 0;
		}
	}

	internal bool IsEmptyElement => false;

	internal XmlNodeType NodeType
	{
		get
		{
			if (!isEndOfEmptyElement)
			{
				return reader.NodeType;
			}
			return XmlNodeType.EndElement;
		}
	}

	internal int LineNumber
	{
		get
		{
			if (reader is IXmlLineInfo xmlLineInfo)
			{
				return xmlLineInfo.LineNumber;
			}
			return 0;
		}
	}

	internal int LinePosition
	{
		get
		{
			if (reader is IXmlLineInfo xmlLineInfo)
			{
				return xmlLineInfo.LinePosition;
			}
			return 0;
		}
	}

	internal bool Normalized
	{
		get
		{
			if (!(reader is XmlTextReader xmlTextReader))
			{
				if (reader is IXmlTextParser xmlTextParser)
				{
					return xmlTextParser.Normalized;
				}
				return false;
			}
			return xmlTextReader.Normalization;
		}
		set
		{
			if (!(reader is XmlTextReader xmlTextReader))
			{
				if (reader is IXmlTextParser xmlTextParser)
				{
					xmlTextParser.Normalized = value;
				}
			}
			else
			{
				xmlTextReader.Normalization = value;
			}
		}
	}

	internal WhitespaceHandling WhitespaceHandling
	{
		get
		{
			if (!(reader is XmlTextReader xmlTextReader))
			{
				if (reader is IXmlTextParser xmlTextParser)
				{
					return xmlTextParser.WhitespaceHandling;
				}
				return WhitespaceHandling.None;
			}
			return xmlTextReader.WhitespaceHandling;
		}
		set
		{
			if (!(reader is XmlTextReader xmlTextReader))
			{
				if (reader is IXmlTextParser xmlTextParser)
				{
					xmlTextParser.WhitespaceHandling = value;
				}
			}
			else
			{
				xmlTextReader.WhitespaceHandling = value;
			}
		}
	}

	internal string Name => reader.Name;

	internal string LocalName => reader.LocalName;

	internal string NamespaceURI => reader.NamespaceURI;

	internal string Value => reader.Value;

	internal Type ValueType => reader.ValueType;

	internal int Depth => reader.Depth;

	internal bool EOF => reader.EOF;

	public XmlReaderDelegator(XmlReader reader)
	{
		XmlObjectSerializer.CheckNull(reader, "reader");
		this.reader = reader;
		dictionaryReader = reader as XmlDictionaryReader;
	}

	internal string GetAttribute(string name)
	{
		if (!isEndOfEmptyElement)
		{
			return reader.GetAttribute(name);
		}
		return null;
	}

	internal string GetAttribute(string name, string namespaceUri)
	{
		if (!isEndOfEmptyElement)
		{
			return reader.GetAttribute(name, namespaceUri);
		}
		return null;
	}

	internal string GetAttribute(int i)
	{
		if (isEndOfEmptyElement)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("i", System.SR.XmlElementAttributes));
		}
		return reader.GetAttribute(i);
	}

	internal bool IsNamespaceURI(string ns)
	{
		if (dictionaryReader == null)
		{
			return ns == reader.NamespaceURI;
		}
		return dictionaryReader.IsNamespaceUri(ns);
	}

	internal bool IsLocalName(string localName)
	{
		if (dictionaryReader == null)
		{
			return localName == reader.LocalName;
		}
		return dictionaryReader.IsLocalName(localName);
	}

	internal bool IsNamespaceUri(XmlDictionaryString ns)
	{
		if (dictionaryReader == null)
		{
			return ns.Value == reader.NamespaceURI;
		}
		return dictionaryReader.IsNamespaceUri(ns);
	}

	internal bool IsLocalName(XmlDictionaryString localName)
	{
		if (dictionaryReader == null)
		{
			return localName.Value == reader.LocalName;
		}
		return dictionaryReader.IsLocalName(localName);
	}

	internal int IndexOfLocalName(XmlDictionaryString[] localNames, XmlDictionaryString ns)
	{
		if (dictionaryReader != null)
		{
			return dictionaryReader.IndexOfLocalName(localNames, ns);
		}
		if (reader.NamespaceURI == ns.Value)
		{
			string localName = LocalName;
			for (int i = 0; i < localNames.Length; i++)
			{
				if (localName == localNames[i].Value)
				{
					return i;
				}
			}
		}
		return -1;
	}

	internal bool IsStartElement()
	{
		if (!isEndOfEmptyElement)
		{
			return reader.IsStartElement();
		}
		return false;
	}

	internal bool IsStartElement(string localname, string ns)
	{
		if (!isEndOfEmptyElement)
		{
			return reader.IsStartElement(localname, ns);
		}
		return false;
	}

	internal bool IsStartElement(XmlDictionaryString localname, XmlDictionaryString ns)
	{
		if (dictionaryReader == null)
		{
			if (!isEndOfEmptyElement)
			{
				return reader.IsStartElement(localname.Value, ns.Value);
			}
			return false;
		}
		if (!isEndOfEmptyElement)
		{
			return dictionaryReader.IsStartElement(localname, ns);
		}
		return false;
	}

	internal bool MoveToAttribute(string name)
	{
		if (!isEndOfEmptyElement)
		{
			return reader.MoveToAttribute(name);
		}
		return false;
	}

	internal bool MoveToAttribute(string name, string ns)
	{
		if (!isEndOfEmptyElement)
		{
			return reader.MoveToAttribute(name, ns);
		}
		return false;
	}

	internal void MoveToAttribute(int i)
	{
		if (isEndOfEmptyElement)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("i", System.SR.XmlElementAttributes));
		}
		reader.MoveToAttribute(i);
	}

	internal bool MoveToElement()
	{
		if (!isEndOfEmptyElement)
		{
			return reader.MoveToElement();
		}
		return false;
	}

	internal bool MoveToFirstAttribute()
	{
		if (!isEndOfEmptyElement)
		{
			return reader.MoveToFirstAttribute();
		}
		return false;
	}

	internal bool MoveToNextAttribute()
	{
		if (!isEndOfEmptyElement)
		{
			return reader.MoveToNextAttribute();
		}
		return false;
	}

	internal bool Read()
	{
		reader.MoveToElement();
		if (!reader.IsEmptyElement)
		{
			return reader.Read();
		}
		if (isEndOfEmptyElement)
		{
			isEndOfEmptyElement = false;
			return reader.Read();
		}
		isEndOfEmptyElement = true;
		return true;
	}

	internal XmlNodeType MoveToContent()
	{
		if (isEndOfEmptyElement)
		{
			return XmlNodeType.EndElement;
		}
		return reader.MoveToContent();
	}

	internal bool ReadAttributeValue()
	{
		if (!isEndOfEmptyElement)
		{
			return reader.ReadAttributeValue();
		}
		return false;
	}

	internal void ReadEndElement()
	{
		if (isEndOfEmptyElement)
		{
			Read();
		}
		else
		{
			reader.ReadEndElement();
		}
	}

	private Exception CreateInvalidPrimitiveTypeException(Type type)
	{
		return new InvalidDataContractException(System.SR.Format(type.IsInterface ? System.SR.InterfaceTypeCannotBeCreated : System.SR.InvalidPrimitiveType_Serialization, DataContract.GetClrTypeFullName(type)));
	}

	public object ReadElementContentAsAnyType(Type valueType)
	{
		Read();
		object result = ReadContentAsAnyType(valueType);
		ReadEndElement();
		return result;
	}

	internal object ReadContentAsAnyType(Type valueType)
	{
		switch (Type.GetTypeCode(valueType))
		{
		case TypeCode.Boolean:
			return ReadContentAsBoolean();
		case TypeCode.Char:
			return ReadContentAsChar();
		case TypeCode.Byte:
			return ReadContentAsUnsignedByte();
		case TypeCode.Int16:
			return ReadContentAsShort();
		case TypeCode.Int32:
			return ReadContentAsInt();
		case TypeCode.Int64:
			return ReadContentAsLong();
		case TypeCode.Single:
			return ReadContentAsSingle();
		case TypeCode.Double:
			return ReadContentAsDouble();
		case TypeCode.Decimal:
			return ReadContentAsDecimal();
		case TypeCode.DateTime:
			return ReadContentAsDateTime();
		case TypeCode.String:
			return ReadContentAsString();
		case TypeCode.SByte:
			return ReadContentAsSignedByte();
		case TypeCode.UInt16:
			return ReadContentAsUnsignedShort();
		case TypeCode.UInt32:
			return ReadContentAsUnsignedInt();
		case TypeCode.UInt64:
			return ReadContentAsUnsignedLong();
		default:
			if (valueType == Globals.TypeOfByteArray)
			{
				return ReadContentAsBase64();
			}
			if (valueType == Globals.TypeOfObject)
			{
				return new object();
			}
			if (valueType == Globals.TypeOfTimeSpan)
			{
				return ReadContentAsTimeSpan();
			}
			if (valueType == Globals.TypeOfGuid)
			{
				return ReadContentAsGuid();
			}
			if (valueType == Globals.TypeOfUri)
			{
				return ReadContentAsUri();
			}
			if (valueType == Globals.TypeOfXmlQualifiedName)
			{
				return ReadContentAsQName();
			}
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidPrimitiveTypeException(valueType));
		}
	}

	internal IDataNode ReadExtensionData(Type valueType)
	{
		switch (Type.GetTypeCode(valueType))
		{
		case TypeCode.Boolean:
			return new DataNode<bool>(ReadContentAsBoolean());
		case TypeCode.Char:
			return new DataNode<char>(ReadContentAsChar());
		case TypeCode.Byte:
			return new DataNode<byte>(ReadContentAsUnsignedByte());
		case TypeCode.Int16:
			return new DataNode<short>(ReadContentAsShort());
		case TypeCode.Int32:
			return new DataNode<int>(ReadContentAsInt());
		case TypeCode.Int64:
			return new DataNode<long>(ReadContentAsLong());
		case TypeCode.Single:
			return new DataNode<float>(ReadContentAsSingle());
		case TypeCode.Double:
			return new DataNode<double>(ReadContentAsDouble());
		case TypeCode.Decimal:
			return new DataNode<decimal>(ReadContentAsDecimal());
		case TypeCode.DateTime:
			return new DataNode<DateTime>(ReadContentAsDateTime());
		case TypeCode.String:
			return new DataNode<string>(ReadContentAsString());
		case TypeCode.SByte:
			return new DataNode<sbyte>(ReadContentAsSignedByte());
		case TypeCode.UInt16:
			return new DataNode<ushort>(ReadContentAsUnsignedShort());
		case TypeCode.UInt32:
			return new DataNode<uint>(ReadContentAsUnsignedInt());
		case TypeCode.UInt64:
			return new DataNode<ulong>(ReadContentAsUnsignedLong());
		default:
			if (valueType == Globals.TypeOfByteArray)
			{
				return new DataNode<byte[]>(ReadContentAsBase64());
			}
			if (valueType == Globals.TypeOfObject)
			{
				return new DataNode<object>(new object());
			}
			if (valueType == Globals.TypeOfTimeSpan)
			{
				return new DataNode<TimeSpan>(ReadContentAsTimeSpan());
			}
			if (valueType == Globals.TypeOfGuid)
			{
				return new DataNode<Guid>(ReadContentAsGuid());
			}
			if (valueType == Globals.TypeOfUri)
			{
				return new DataNode<Uri>(ReadContentAsUri());
			}
			if (valueType == Globals.TypeOfXmlQualifiedName)
			{
				return new DataNode<XmlQualifiedName>(ReadContentAsQName());
			}
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidPrimitiveTypeException(valueType));
		}
	}

	[DoesNotReturn]
	private void ThrowConversionException(string value, string type)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(XmlObjectSerializer.TryAddLineInfo(this, System.SR.Format(System.SR.XmlInvalidConversion, value, type))));
	}

	[DoesNotReturn]
	private void ThrowNotAtElement()
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(System.SR.Format(System.SR.XmlStartElementExpected, "EndElement")));
	}

	internal virtual char ReadElementContentAsChar()
	{
		return ToChar(ReadElementContentAsInt());
	}

	internal virtual char ReadContentAsChar()
	{
		return ToChar(ReadContentAsInt());
	}

	private char ToChar(int value)
	{
		if (value < 0 || value > 65535)
		{
			ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "Char");
		}
		return (char)value;
	}

	internal string ReadElementContentAsString()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		return reader.ReadElementContentAsString();
	}

	internal string ReadContentAsString()
	{
		if (!isEndOfEmptyElement)
		{
			return reader.ReadContentAsString();
		}
		return string.Empty;
	}

	internal bool ReadElementContentAsBoolean()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		return reader.ReadElementContentAsBoolean();
	}

	internal bool ReadContentAsBoolean()
	{
		if (isEndOfEmptyElement)
		{
			ThrowConversionException(string.Empty, "Boolean");
		}
		return reader.ReadContentAsBoolean();
	}

	internal float ReadElementContentAsFloat()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		return reader.ReadElementContentAsFloat();
	}

	internal float ReadContentAsSingle()
	{
		if (isEndOfEmptyElement)
		{
			ThrowConversionException(string.Empty, "Float");
		}
		return reader.ReadContentAsFloat();
	}

	internal double ReadElementContentAsDouble()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		return reader.ReadElementContentAsDouble();
	}

	internal double ReadContentAsDouble()
	{
		if (isEndOfEmptyElement)
		{
			ThrowConversionException(string.Empty, "Double");
		}
		return reader.ReadContentAsDouble();
	}

	internal decimal ReadElementContentAsDecimal()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		return reader.ReadElementContentAsDecimal();
	}

	internal decimal ReadContentAsDecimal()
	{
		if (isEndOfEmptyElement)
		{
			ThrowConversionException(string.Empty, "Decimal");
		}
		return reader.ReadContentAsDecimal();
	}

	internal virtual byte[] ReadElementContentAsBase64()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		if (dictionaryReader == null)
		{
			return ReadContentAsBase64(reader.ReadElementContentAsString());
		}
		return dictionaryReader.ReadElementContentAsBase64();
	}

	public virtual byte[] ReadContentAsBase64()
	{
		if (isEndOfEmptyElement)
		{
			return Array.Empty<byte>();
		}
		if (dictionaryReader == null)
		{
			return ReadContentAsBase64(reader.ReadContentAsString());
		}
		return dictionaryReader.ReadContentAsBase64();
	}

	[return: NotNullIfNotNull("str")]
	internal byte[] ReadContentAsBase64(string str)
	{
		if (str == null)
		{
			return null;
		}
		str = str.Trim();
		if (str.Length == 0)
		{
			return Array.Empty<byte>();
		}
		try
		{
			return Convert.FromBase64String(str);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(str, "byte[]", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(str, "byte[]", exception2));
		}
	}

	internal virtual DateTime ReadElementContentAsDateTime()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		return XmlConvert.ToDateTime(reader.ReadElementContentAsString(), XmlDateTimeSerializationMode.RoundtripKind);
	}

	internal virtual DateTime ReadContentAsDateTime()
	{
		if (isEndOfEmptyElement)
		{
			ThrowConversionException(string.Empty, "DateTime");
		}
		return reader.ReadContentAsDateTime();
	}

	internal int ReadElementContentAsInt()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		return reader.ReadElementContentAsInt();
	}

	internal int ReadContentAsInt()
	{
		if (isEndOfEmptyElement)
		{
			ThrowConversionException(string.Empty, "Int32");
		}
		return reader.ReadContentAsInt();
	}

	internal long ReadElementContentAsLong()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		return reader.ReadElementContentAsLong();
	}

	internal long ReadContentAsLong()
	{
		if (isEndOfEmptyElement)
		{
			ThrowConversionException(string.Empty, "Int64");
		}
		return reader.ReadContentAsLong();
	}

	internal short ReadElementContentAsShort()
	{
		return ToShort(ReadElementContentAsInt());
	}

	internal short ReadContentAsShort()
	{
		return ToShort(ReadContentAsInt());
	}

	private short ToShort(int value)
	{
		if (value < -32768 || value > 32767)
		{
			ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "Int16");
		}
		return (short)value;
	}

	internal byte ReadElementContentAsUnsignedByte()
	{
		return ToByte(ReadElementContentAsInt());
	}

	internal byte ReadContentAsUnsignedByte()
	{
		return ToByte(ReadContentAsInt());
	}

	private byte ToByte(int value)
	{
		if (value < 0 || value > 255)
		{
			ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "Byte");
		}
		return (byte)value;
	}

	internal sbyte ReadElementContentAsSignedByte()
	{
		return ToSByte(ReadElementContentAsInt());
	}

	internal sbyte ReadContentAsSignedByte()
	{
		return ToSByte(ReadContentAsInt());
	}

	private sbyte ToSByte(int value)
	{
		if (value < -128 || value > 127)
		{
			ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "SByte");
		}
		return (sbyte)value;
	}

	internal uint ReadElementContentAsUnsignedInt()
	{
		return ToUInt32(ReadElementContentAsLong());
	}

	internal uint ReadContentAsUnsignedInt()
	{
		return ToUInt32(ReadContentAsLong());
	}

	private uint ToUInt32(long value)
	{
		if (value < 0 || value > uint.MaxValue)
		{
			ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "UInt32");
		}
		return (uint)value;
	}

	internal virtual ulong ReadElementContentAsUnsignedLong()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		string text = reader.ReadElementContentAsString();
		if (text == null || text.Length == 0)
		{
			ThrowConversionException(string.Empty, "UInt64");
		}
		return XmlConverter.ToUInt64(text);
	}

	internal virtual ulong ReadContentAsUnsignedLong()
	{
		string text = reader.ReadContentAsString();
		if (text == null || text.Length == 0)
		{
			ThrowConversionException(string.Empty, "UInt64");
		}
		return XmlConverter.ToUInt64(text);
	}

	internal ushort ReadElementContentAsUnsignedShort()
	{
		return ToUInt16(ReadElementContentAsInt());
	}

	internal ushort ReadContentAsUnsignedShort()
	{
		return ToUInt16(ReadContentAsInt());
	}

	private ushort ToUInt16(int value)
	{
		if (value < 0 || value > 65535)
		{
			ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "UInt16");
		}
		return (ushort)value;
	}

	internal TimeSpan ReadElementContentAsTimeSpan()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		string value = reader.ReadElementContentAsString();
		return XmlConverter.ToTimeSpan(value);
	}

	internal TimeSpan ReadContentAsTimeSpan()
	{
		string value = reader.ReadContentAsString();
		return XmlConverter.ToTimeSpan(value);
	}

	internal Guid ReadElementContentAsGuid()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		string text = reader.ReadElementContentAsString();
		try
		{
			return new Guid(text);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(text, "Guid", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(text, "Guid", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(text, "Guid", exception3));
		}
	}

	internal Guid ReadContentAsGuid()
	{
		string text = reader.ReadContentAsString();
		try
		{
			return Guid.Parse(text);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(text, "Guid", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(text, "Guid", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(text, "Guid", exception3));
		}
	}

	internal Uri ReadElementContentAsUri()
	{
		if (isEndOfEmptyElement)
		{
			ThrowNotAtElement();
		}
		string text = ReadElementContentAsString();
		try
		{
			return new Uri(text, UriKind.RelativeOrAbsolute);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(text, "Uri", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(text, "Uri", exception2));
		}
	}

	internal Uri ReadContentAsUri()
	{
		string text = ReadContentAsString();
		try
		{
			return new Uri(text, UriKind.RelativeOrAbsolute);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(text, "Uri", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(text, "Uri", exception2));
		}
	}

	internal XmlQualifiedName ReadElementContentAsQName()
	{
		Read();
		XmlQualifiedName result = ReadContentAsQName();
		ReadEndElement();
		return result;
	}

	internal virtual XmlQualifiedName ReadContentAsQName()
	{
		return ParseQualifiedName(ReadContentAsString());
	}

	private XmlQualifiedName ParseQualifiedName(string str)
	{
		string name;
		string ns;
		if (str == null || str.Length == 0)
		{
			name = (ns = string.Empty);
		}
		else
		{
			XmlObjectSerializerReadContext.ParseQualifiedName(str, this, out name, out ns, out var _);
		}
		return new XmlQualifiedName(name, ns);
	}

	private void CheckExpectedArrayLength(XmlObjectSerializerReadContext context, int arrayLength)
	{
		context.IncrementItemCount(arrayLength);
	}

	protected int GetArrayLengthQuota(XmlObjectSerializerReadContext context)
	{
		return Math.Min(context.RemainingItemCount, int.MaxValue);
	}

	private void CheckActualArrayLength(int expectedLength, int actualLength, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
	{
		if (expectedLength != actualLength)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ArrayExceededSizeAttribute, expectedLength, itemName.Value, itemNamespace.Value)));
		}
	}

	internal bool TryReadBooleanArray(XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int arrayLength, [NotNullWhen(true)] out bool[] array)
	{
		if (dictionaryReader == null)
		{
			array = null;
			return false;
		}
		if (arrayLength != -1)
		{
			CheckExpectedArrayLength(context, arrayLength);
			array = new bool[arrayLength];
			int num = 0;
			int num2 = 0;
			while ((num = dictionaryReader.ReadArray(itemName, itemNamespace, array, num2, arrayLength - num2)) > 0)
			{
				num2 += num;
			}
			CheckActualArrayLength(arrayLength, num2, itemName, itemNamespace);
		}
		else
		{
			array = BooleanArrayHelperWithDictionaryString.Instance.ReadArray(dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
			context.IncrementItemCount(array.Length);
		}
		return true;
	}

	internal virtual bool TryReadDateTimeArray(XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int arrayLength, [NotNullWhen(true)] out DateTime[] array)
	{
		if (dictionaryReader == null)
		{
			array = null;
			return false;
		}
		if (arrayLength != -1)
		{
			CheckExpectedArrayLength(context, arrayLength);
			array = new DateTime[arrayLength];
			int num = 0;
			int num2 = 0;
			while ((num = dictionaryReader.ReadArray(itemName, itemNamespace, array, num2, arrayLength - num2)) > 0)
			{
				num2 += num;
			}
			CheckActualArrayLength(arrayLength, num2, itemName, itemNamespace);
		}
		else
		{
			array = DateTimeArrayHelperWithDictionaryString.Instance.ReadArray(dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
			context.IncrementItemCount(array.Length);
		}
		return true;
	}

	internal bool TryReadDecimalArray(XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int arrayLength, [NotNullWhen(true)] out decimal[] array)
	{
		if (dictionaryReader == null)
		{
			array = null;
			return false;
		}
		if (arrayLength != -1)
		{
			CheckExpectedArrayLength(context, arrayLength);
			array = new decimal[arrayLength];
			int num = 0;
			int num2 = 0;
			while ((num = dictionaryReader.ReadArray(itemName, itemNamespace, array, num2, arrayLength - num2)) > 0)
			{
				num2 += num;
			}
			CheckActualArrayLength(arrayLength, num2, itemName, itemNamespace);
		}
		else
		{
			array = DecimalArrayHelperWithDictionaryString.Instance.ReadArray(dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
			context.IncrementItemCount(array.Length);
		}
		return true;
	}

	internal bool TryReadInt32Array(XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int arrayLength, [NotNullWhen(true)] out int[] array)
	{
		if (dictionaryReader == null)
		{
			array = null;
			return false;
		}
		if (arrayLength != -1)
		{
			CheckExpectedArrayLength(context, arrayLength);
			array = new int[arrayLength];
			int num = 0;
			int num2 = 0;
			while ((num = dictionaryReader.ReadArray(itemName, itemNamespace, array, num2, arrayLength - num2)) > 0)
			{
				num2 += num;
			}
			CheckActualArrayLength(arrayLength, num2, itemName, itemNamespace);
		}
		else
		{
			array = Int32ArrayHelperWithDictionaryString.Instance.ReadArray(dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
			context.IncrementItemCount(array.Length);
		}
		return true;
	}

	internal bool TryReadInt64Array(XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int arrayLength, [NotNullWhen(true)] out long[] array)
	{
		if (dictionaryReader == null)
		{
			array = null;
			return false;
		}
		if (arrayLength != -1)
		{
			CheckExpectedArrayLength(context, arrayLength);
			array = new long[arrayLength];
			int num = 0;
			int num2 = 0;
			while ((num = dictionaryReader.ReadArray(itemName, itemNamespace, array, num2, arrayLength - num2)) > 0)
			{
				num2 += num;
			}
			CheckActualArrayLength(arrayLength, num2, itemName, itemNamespace);
		}
		else
		{
			array = Int64ArrayHelperWithDictionaryString.Instance.ReadArray(dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
			context.IncrementItemCount(array.Length);
		}
		return true;
	}

	internal bool TryReadSingleArray(XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int arrayLength, [NotNullWhen(true)] out float[] array)
	{
		if (dictionaryReader == null)
		{
			array = null;
			return false;
		}
		if (arrayLength != -1)
		{
			CheckExpectedArrayLength(context, arrayLength);
			array = new float[arrayLength];
			int num = 0;
			int num2 = 0;
			while ((num = dictionaryReader.ReadArray(itemName, itemNamespace, array, num2, arrayLength - num2)) > 0)
			{
				num2 += num;
			}
			CheckActualArrayLength(arrayLength, num2, itemName, itemNamespace);
		}
		else
		{
			array = SingleArrayHelperWithDictionaryString.Instance.ReadArray(dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
			context.IncrementItemCount(array.Length);
		}
		return true;
	}

	internal bool TryReadDoubleArray(XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int arrayLength, [NotNullWhen(true)] out double[] array)
	{
		if (dictionaryReader == null)
		{
			array = null;
			return false;
		}
		if (arrayLength != -1)
		{
			CheckExpectedArrayLength(context, arrayLength);
			array = new double[arrayLength];
			int num = 0;
			int num2 = 0;
			while ((num = dictionaryReader.ReadArray(itemName, itemNamespace, array, num2, arrayLength - num2)) > 0)
			{
				num2 += num;
			}
			CheckActualArrayLength(arrayLength, num2, itemName, itemNamespace);
		}
		else
		{
			array = DoubleArrayHelperWithDictionaryString.Instance.ReadArray(dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
			context.IncrementItemCount(array.Length);
		}
		return true;
	}

	internal IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
	{
		if (!(reader is IXmlNamespaceResolver))
		{
			return null;
		}
		return ((IXmlNamespaceResolver)reader).GetNamespacesInScope(scope);
	}

	internal bool HasLineInfo()
	{
		if (reader is IXmlLineInfo xmlLineInfo)
		{
			return xmlLineInfo.HasLineInfo();
		}
		return false;
	}

	internal string LookupNamespace(string prefix)
	{
		return reader.LookupNamespace(prefix);
	}

	internal void Skip()
	{
		reader.Skip();
		isEndOfEmptyElement = false;
	}
}
