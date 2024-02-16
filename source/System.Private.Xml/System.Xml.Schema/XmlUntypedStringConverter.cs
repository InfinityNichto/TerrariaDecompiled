namespace System.Xml.Schema;

internal sealed class XmlUntypedStringConverter
{
	private readonly bool _listsAllowed;

	private readonly XmlUntypedStringConverter _listItemConverter;

	private static readonly Type s_decimalType = typeof(decimal);

	private static readonly Type s_int32Type = typeof(int);

	private static readonly Type s_int64Type = typeof(long);

	private static readonly Type s_stringType = typeof(string);

	private static readonly Type s_objectType = typeof(object);

	private static readonly Type s_byteType = typeof(byte);

	private static readonly Type s_int16Type = typeof(short);

	private static readonly Type s_SByteType = typeof(sbyte);

	private static readonly Type s_UInt16Type = typeof(ushort);

	private static readonly Type s_UInt32Type = typeof(uint);

	private static readonly Type s_UInt64Type = typeof(ulong);

	private static readonly Type s_doubleType = typeof(double);

	private static readonly Type s_singleType = typeof(float);

	private static readonly Type s_dateTimeType = typeof(DateTime);

	private static readonly Type s_dateTimeOffsetType = typeof(DateTimeOffset);

	private static readonly Type s_booleanType = typeof(bool);

	private static readonly Type s_byteArrayType = typeof(byte[]);

	private static readonly Type s_xmlQualifiedNameType = typeof(XmlQualifiedName);

	private static readonly Type s_uriType = typeof(Uri);

	private static readonly Type s_timeSpanType = typeof(TimeSpan);

	internal static XmlUntypedStringConverter Instance = new XmlUntypedStringConverter(listsAllowed: true);

	private XmlUntypedStringConverter(bool listsAllowed)
	{
		_listsAllowed = listsAllowed;
		if (listsAllowed)
		{
			_listItemConverter = new XmlUntypedStringConverter(listsAllowed: false);
		}
	}

	internal object FromString(string value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == s_objectType)
		{
			destinationType = typeof(string);
		}
		if (destinationType == s_booleanType)
		{
			return XmlConvert.ToBoolean(value);
		}
		if (destinationType == s_byteType)
		{
			return Int32ToByte(XmlConvert.ToInt32(value));
		}
		if (destinationType == s_byteArrayType)
		{
			return StringToBase64Binary(value);
		}
		if (destinationType == s_dateTimeType)
		{
			return StringToDateTime(value);
		}
		if (destinationType == s_dateTimeOffsetType)
		{
			return StringToDateTimeOffset(value);
		}
		if (destinationType == s_decimalType)
		{
			return XmlConvert.ToDecimal(value);
		}
		if (destinationType == s_doubleType)
		{
			return XmlConvert.ToDouble(value);
		}
		if (destinationType == s_int16Type)
		{
			return Int32ToInt16(XmlConvert.ToInt32(value));
		}
		if (destinationType == s_int32Type)
		{
			return XmlConvert.ToInt32(value);
		}
		if (destinationType == s_int64Type)
		{
			return XmlConvert.ToInt64(value);
		}
		if (destinationType == s_SByteType)
		{
			return Int32ToSByte(XmlConvert.ToInt32(value));
		}
		if (destinationType == s_singleType)
		{
			return XmlConvert.ToSingle(value);
		}
		if (destinationType == s_timeSpanType)
		{
			return StringToDuration(value);
		}
		if (destinationType == s_UInt16Type)
		{
			return Int32ToUInt16(XmlConvert.ToInt32(value));
		}
		if (destinationType == s_UInt32Type)
		{
			return Int64ToUInt32(XmlConvert.ToInt64(value));
		}
		if (destinationType == s_UInt64Type)
		{
			return DecimalToUInt64(XmlConvert.ToDecimal(value));
		}
		if (destinationType == s_uriType)
		{
			return XmlConvert.ToUri(value);
		}
		if (destinationType == s_xmlQualifiedNameType)
		{
			return StringToQName(value, nsResolver);
		}
		if (destinationType == s_stringType)
		{
			return value;
		}
		return StringToListType(value, destinationType, nsResolver);
	}

	private byte Int32ToByte(int value)
	{
		if (value < 0 || value > 255)
		{
			string xmlConvert_Overflow = System.SR.XmlConvert_Overflow;
			object[] args = new string[2]
			{
				XmlConvert.ToString(value),
				"Byte"
			};
			throw new OverflowException(System.SR.Format(xmlConvert_Overflow, args));
		}
		return (byte)value;
	}

	private short Int32ToInt16(int value)
	{
		if (value < -32768 || value > 32767)
		{
			string xmlConvert_Overflow = System.SR.XmlConvert_Overflow;
			object[] args = new string[2]
			{
				XmlConvert.ToString(value),
				"Int16"
			};
			throw new OverflowException(System.SR.Format(xmlConvert_Overflow, args));
		}
		return (short)value;
	}

	private sbyte Int32ToSByte(int value)
	{
		if (value < -128 || value > 127)
		{
			string xmlConvert_Overflow = System.SR.XmlConvert_Overflow;
			object[] args = new string[2]
			{
				XmlConvert.ToString(value),
				"SByte"
			};
			throw new OverflowException(System.SR.Format(xmlConvert_Overflow, args));
		}
		return (sbyte)value;
	}

	private ushort Int32ToUInt16(int value)
	{
		if (value < 0 || value > 65535)
		{
			string xmlConvert_Overflow = System.SR.XmlConvert_Overflow;
			object[] args = new string[2]
			{
				XmlConvert.ToString(value),
				"UInt16"
			};
			throw new OverflowException(System.SR.Format(xmlConvert_Overflow, args));
		}
		return (ushort)value;
	}

	private uint Int64ToUInt32(long value)
	{
		if (value < 0 || value > uint.MaxValue)
		{
			string xmlConvert_Overflow = System.SR.XmlConvert_Overflow;
			object[] args = new string[2]
			{
				XmlConvert.ToString(value),
				"UInt32"
			};
			throw new OverflowException(System.SR.Format(xmlConvert_Overflow, args));
		}
		return (uint)value;
	}

	private ulong DecimalToUInt64(decimal value)
	{
		if (value < 0m || value > 18446744073709551615m)
		{
			string xmlConvert_Overflow = System.SR.XmlConvert_Overflow;
			object[] args = new string[2]
			{
				XmlConvert.ToString(value),
				"UInt64"
			};
			throw new OverflowException(System.SR.Format(xmlConvert_Overflow, args));
		}
		return (ulong)value;
	}

	private byte[] StringToBase64Binary(string value)
	{
		return Convert.FromBase64String(XmlConvert.TrimString(value));
	}

	private static DateTime StringToDateTime(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.AllXsd);
	}

	private static DateTimeOffset StringToDateTimeOffset(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.AllXsd);
	}

	private TimeSpan StringToDuration(string value)
	{
		return new XsdDuration(value, XsdDuration.DurationType.Duration).ToTimeSpan(XsdDuration.DurationType.Duration);
	}

	private static XmlQualifiedName StringToQName(string value, IXmlNamespaceResolver nsResolver)
	{
		value = value.Trim();
		string prefix;
		string localName;
		try
		{
			ValidateNames.ParseQNameThrow(value, out prefix, out localName);
		}
		catch (XmlException ex)
		{
			throw new FormatException(ex.Message);
		}
		if (nsResolver == null)
		{
			throw new InvalidCastException(System.SR.Format(System.SR.XmlConvert_TypeNoNamespace, value, prefix));
		}
		string text = nsResolver.LookupNamespace(prefix);
		if (text == null)
		{
			throw new InvalidCastException(System.SR.Format(System.SR.XmlConvert_TypeNoNamespace, value, prefix));
		}
		return new XmlQualifiedName(localName, text);
	}

	private object StringToListType(string value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		if (_listsAllowed && destinationType.IsArray)
		{
			Type elementType = destinationType.GetElementType();
			if (elementType == s_objectType)
			{
				return ToArray<object>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_booleanType)
			{
				return ToArray<bool>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_byteType)
			{
				return ToArray<byte>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_byteArrayType)
			{
				return ToArray<byte[]>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_dateTimeType)
			{
				return ToArray<DateTime>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_dateTimeOffsetType)
			{
				return ToArray<DateTimeOffset>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_decimalType)
			{
				return ToArray<decimal>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_doubleType)
			{
				return ToArray<double>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_int16Type)
			{
				return ToArray<short>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_int32Type)
			{
				return ToArray<int>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_int64Type)
			{
				return ToArray<long>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_SByteType)
			{
				return ToArray<sbyte>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_singleType)
			{
				return ToArray<float>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_stringType)
			{
				return ToArray<string>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_timeSpanType)
			{
				return ToArray<TimeSpan>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_UInt16Type)
			{
				return ToArray<ushort>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_UInt32Type)
			{
				return ToArray<uint>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_UInt64Type)
			{
				return ToArray<ulong>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_uriType)
			{
				return ToArray<Uri>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
			if (elementType == s_xmlQualifiedNameType)
			{
				return ToArray<XmlQualifiedName>(XmlConvert.SplitString(value, StringSplitOptions.RemoveEmptyEntries), nsResolver);
			}
		}
		throw CreateInvalidClrMappingException(typeof(string), destinationType);
	}

	private T[] ToArray<T>(string[] stringArray, IXmlNamespaceResolver nsResolver)
	{
		T[] array = new T[stringArray.Length];
		for (int i = 0; i < stringArray.Length; i++)
		{
			array[i] = (T)_listItemConverter.FromString(stringArray[i], typeof(T), nsResolver);
		}
		return array;
	}

	private Exception CreateInvalidClrMappingException(Type sourceType, Type destinationType)
	{
		return new InvalidCastException(System.SR.Format(System.SR.XmlConvert_TypeListBadMapping2, "xdt:untypedAtomic", sourceType.Name, destinationType.Name));
	}
}
