using System.Collections;
using System.Xml.XPath;

namespace System.Xml.Schema;

internal abstract class XmlBaseConverter : XmlValueConverter
{
	private readonly XmlSchemaType _schemaType;

	private readonly XmlTypeCode _typeCode;

	private readonly Type _clrTypeDefault;

	protected static readonly Type ICollectionType = typeof(ICollection);

	protected static readonly Type IEnumerableType = typeof(IEnumerable);

	protected static readonly Type IListType = typeof(IList);

	protected static readonly Type ObjectArrayType = typeof(object[]);

	protected static readonly Type StringArrayType = typeof(string[]);

	protected static readonly Type XmlAtomicValueArrayType = typeof(XmlAtomicValue[]);

	protected static readonly Type DecimalType = typeof(decimal);

	protected static readonly Type Int32Type = typeof(int);

	protected static readonly Type Int64Type = typeof(long);

	protected static readonly Type StringType = typeof(string);

	protected static readonly Type XmlAtomicValueType = typeof(XmlAtomicValue);

	protected static readonly Type ObjectType = typeof(object);

	protected static readonly Type ByteType = typeof(byte);

	protected static readonly Type Int16Type = typeof(short);

	protected static readonly Type SByteType = typeof(sbyte);

	protected static readonly Type UInt16Type = typeof(ushort);

	protected static readonly Type UInt32Type = typeof(uint);

	protected static readonly Type UInt64Type = typeof(ulong);

	protected static readonly Type XPathItemType = typeof(XPathItem);

	protected static readonly Type DoubleType = typeof(double);

	protected static readonly Type SingleType = typeof(float);

	protected static readonly Type DateTimeType = typeof(DateTime);

	protected static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);

	protected static readonly Type BooleanType = typeof(bool);

	protected static readonly Type ByteArrayType = typeof(byte[]);

	protected static readonly Type XmlQualifiedNameType = typeof(XmlQualifiedName);

	protected static readonly Type UriType = typeof(Uri);

	protected static readonly Type TimeSpanType = typeof(TimeSpan);

	protected static readonly Type XPathNavigatorType = typeof(XPathNavigator);

	protected XmlSchemaType SchemaType => _schemaType;

	protected XmlTypeCode TypeCode => _typeCode;

	protected string XmlTypeName
	{
		get
		{
			XmlSchemaType xmlSchemaType = _schemaType;
			if (xmlSchemaType != null)
			{
				while (xmlSchemaType.QualifiedName.IsEmpty)
				{
					xmlSchemaType = xmlSchemaType.BaseXmlSchemaType;
				}
				return QNameToString(xmlSchemaType.QualifiedName);
			}
			if (_typeCode == XmlTypeCode.Node)
			{
				return "node";
			}
			if (_typeCode == XmlTypeCode.AnyAtomicType)
			{
				return "xdt:anyAtomicType";
			}
			return "item";
		}
	}

	protected Type DefaultClrType => _clrTypeDefault;

	protected XmlBaseConverter(XmlSchemaType schemaType)
	{
		XmlSchemaDatatype datatype = schemaType.Datatype;
		while (schemaType != null && !(schemaType is XmlSchemaSimpleType))
		{
			schemaType = schemaType.BaseXmlSchemaType;
		}
		if (schemaType == null)
		{
			schemaType = XmlSchemaType.GetBuiltInSimpleType(datatype.TypeCode);
		}
		_schemaType = schemaType;
		_typeCode = schemaType.TypeCode;
		_clrTypeDefault = schemaType.Datatype.ValueType;
	}

	protected XmlBaseConverter(XmlTypeCode typeCode)
	{
		switch (typeCode)
		{
		case XmlTypeCode.Item:
			_clrTypeDefault = XPathItemType;
			break;
		case XmlTypeCode.Node:
			_clrTypeDefault = XPathNavigatorType;
			break;
		case XmlTypeCode.AnyAtomicType:
			_clrTypeDefault = XmlAtomicValueType;
			break;
		}
		_typeCode = typeCode;
	}

	protected XmlBaseConverter(XmlBaseConverter converterAtomic)
	{
		_schemaType = converterAtomic._schemaType;
		_typeCode = converterAtomic._typeCode;
		_clrTypeDefault = Array.CreateInstance(converterAtomic.DefaultClrType, 0).GetType();
	}

	protected XmlBaseConverter(XmlBaseConverter converterAtomic, Type clrTypeDefault)
	{
		_schemaType = converterAtomic._schemaType;
		_typeCode = converterAtomic._typeCode;
		_clrTypeDefault = clrTypeDefault;
	}

	public override bool ToBoolean(DateTime value)
	{
		return (bool)ChangeType(value, BooleanType, null);
	}

	public override bool ToBoolean(double value)
	{
		return (bool)ChangeType(value, BooleanType, null);
	}

	public override bool ToBoolean(int value)
	{
		return (bool)ChangeType(value, BooleanType, null);
	}

	public override bool ToBoolean(long value)
	{
		return (bool)ChangeType(value, BooleanType, null);
	}

	public override bool ToBoolean(string value)
	{
		return (bool)ChangeType((object)value, BooleanType, (IXmlNamespaceResolver)null);
	}

	public override bool ToBoolean(object value)
	{
		return (bool)ChangeType(value, BooleanType, null);
	}

	public override DateTime ToDateTime(bool value)
	{
		return (DateTime)ChangeType(value, DateTimeType, null);
	}

	public override DateTime ToDateTime(DateTimeOffset value)
	{
		return (DateTime)ChangeType(value, DateTimeType, null);
	}

	public override DateTime ToDateTime(double value)
	{
		return (DateTime)ChangeType(value, DateTimeType, null);
	}

	public override DateTime ToDateTime(int value)
	{
		return (DateTime)ChangeType(value, DateTimeType, null);
	}

	public override DateTime ToDateTime(long value)
	{
		return (DateTime)ChangeType(value, DateTimeType, null);
	}

	public override DateTime ToDateTime(string value)
	{
		return (DateTime)ChangeType((object)value, DateTimeType, (IXmlNamespaceResolver)null);
	}

	public override DateTime ToDateTime(object value)
	{
		return (DateTime)ChangeType(value, DateTimeType, null);
	}

	public override DateTimeOffset ToDateTimeOffset(DateTime value)
	{
		return (DateTimeOffset)ChangeType(value, DateTimeOffsetType, null);
	}

	public override DateTimeOffset ToDateTimeOffset(string value)
	{
		return (DateTimeOffset)ChangeType((object)value, DateTimeOffsetType, (IXmlNamespaceResolver)null);
	}

	public override DateTimeOffset ToDateTimeOffset(object value)
	{
		return (DateTimeOffset)ChangeType(value, DateTimeOffsetType, null);
	}

	public override decimal ToDecimal(string value)
	{
		return (decimal)ChangeType((object)value, DecimalType, (IXmlNamespaceResolver)null);
	}

	public override decimal ToDecimal(object value)
	{
		return (decimal)ChangeType(value, DecimalType, null);
	}

	public override double ToDouble(bool value)
	{
		return (double)ChangeType(value, DoubleType, null);
	}

	public override double ToDouble(DateTime value)
	{
		return (double)ChangeType(value, DoubleType, null);
	}

	public override double ToDouble(int value)
	{
		return (double)ChangeType(value, DoubleType, null);
	}

	public override double ToDouble(long value)
	{
		return (double)ChangeType(value, DoubleType, null);
	}

	public override double ToDouble(string value)
	{
		return (double)ChangeType((object)value, DoubleType, (IXmlNamespaceResolver)null);
	}

	public override double ToDouble(object value)
	{
		return (double)ChangeType(value, DoubleType, null);
	}

	public override int ToInt32(bool value)
	{
		return (int)ChangeType(value, Int32Type, null);
	}

	public override int ToInt32(DateTime value)
	{
		return (int)ChangeType(value, Int32Type, null);
	}

	public override int ToInt32(double value)
	{
		return (int)ChangeType(value, Int32Type, null);
	}

	public override int ToInt32(long value)
	{
		return (int)ChangeType(value, Int32Type, null);
	}

	public override int ToInt32(string value)
	{
		return (int)ChangeType((object)value, Int32Type, (IXmlNamespaceResolver)null);
	}

	public override int ToInt32(object value)
	{
		return (int)ChangeType(value, Int32Type, null);
	}

	public override long ToInt64(bool value)
	{
		return (long)ChangeType(value, Int64Type, null);
	}

	public override long ToInt64(DateTime value)
	{
		return (long)ChangeType(value, Int64Type, null);
	}

	public override long ToInt64(double value)
	{
		return (long)ChangeType(value, Int64Type, null);
	}

	public override long ToInt64(int value)
	{
		return (long)ChangeType(value, Int64Type, null);
	}

	public override long ToInt64(string value)
	{
		return (long)ChangeType((object)value, Int64Type, (IXmlNamespaceResolver)null);
	}

	public override long ToInt64(object value)
	{
		return (long)ChangeType(value, Int64Type, null);
	}

	public override float ToSingle(double value)
	{
		return (float)ChangeType(value, SingleType, null);
	}

	public override float ToSingle(string value)
	{
		return (float)ChangeType((object)value, SingleType, (IXmlNamespaceResolver)null);
	}

	public override float ToSingle(object value)
	{
		return (float)ChangeType(value, SingleType, null);
	}

	public override string ToString(bool value)
	{
		return (string)ChangeType(value, StringType, null);
	}

	public override string ToString(DateTime value)
	{
		return (string)ChangeType(value, StringType, null);
	}

	public override string ToString(DateTimeOffset value)
	{
		return (string)ChangeType(value, StringType, null);
	}

	public override string ToString(decimal value)
	{
		return (string)ChangeType(value, StringType, null);
	}

	public override string ToString(double value)
	{
		return (string)ChangeType(value, StringType, null);
	}

	public override string ToString(int value)
	{
		return (string)ChangeType(value, StringType, null);
	}

	public override string ToString(long value)
	{
		return (string)ChangeType(value, StringType, null);
	}

	public override string ToString(float value)
	{
		return (string)ChangeType(value, StringType, null);
	}

	public override string ToString(object value, IXmlNamespaceResolver nsResolver)
	{
		return (string)ChangeType(value, StringType, nsResolver);
	}

	public override string ToString(object value)
	{
		return ToString(value, null);
	}

	public override object ChangeType(bool value, Type destinationType)
	{
		return ChangeType(value, destinationType, null);
	}

	public override object ChangeType(DateTime value, Type destinationType)
	{
		return ChangeType(value, destinationType, null);
	}

	public override object ChangeType(decimal value, Type destinationType)
	{
		return ChangeType(value, destinationType, null);
	}

	public override object ChangeType(double value, Type destinationType)
	{
		return ChangeType(value, destinationType, null);
	}

	public override object ChangeType(int value, Type destinationType)
	{
		return ChangeType(value, destinationType, null);
	}

	public override object ChangeType(long value, Type destinationType)
	{
		return ChangeType(value, destinationType, null);
	}

	public override object ChangeType(string value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		return ChangeType((object)value, destinationType, nsResolver);
	}

	public override object ChangeType(object value, Type destinationType)
	{
		return ChangeType(value, destinationType, null);
	}

	protected static bool IsDerivedFrom(Type derivedType, Type baseType)
	{
		while (derivedType != null)
		{
			if (derivedType == baseType)
			{
				return true;
			}
			derivedType = derivedType.BaseType;
		}
		return false;
	}

	protected Exception CreateInvalidClrMappingException(Type sourceType, Type destinationType)
	{
		if (sourceType == destinationType)
		{
			return new InvalidCastException(System.SR.Format(System.SR.XmlConvert_TypeBadMapping, XmlTypeName, sourceType.Name));
		}
		return new InvalidCastException(System.SR.Format(System.SR.XmlConvert_TypeBadMapping2, XmlTypeName, sourceType.Name, destinationType.Name));
	}

	protected static string QNameToString(XmlQualifiedName name)
	{
		if (name.Namespace.Length == 0)
		{
			return name.Name;
		}
		if (name.Namespace == "http://www.w3.org/2001/XMLSchema")
		{
			return "xs:" + name.Name;
		}
		if (name.Namespace == "http://www.w3.org/2003/11/xpath-datatypes")
		{
			return "xdt:" + name.Name;
		}
		return "{" + name.Namespace + "}" + name.Name;
	}

	protected virtual object ChangeListType(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		throw CreateInvalidClrMappingException(value.GetType(), destinationType);
	}

	protected static byte[] StringToBase64Binary(string value)
	{
		return Convert.FromBase64String(XmlConvert.TrimString(value));
	}

	protected static DateTime StringToDate(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.Date);
	}

	protected static DateTime StringToDateTime(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.DateTime);
	}

	protected static TimeSpan StringToDayTimeDuration(string value)
	{
		return new XsdDuration(value, XsdDuration.DurationType.DayTimeDuration).ToTimeSpan(XsdDuration.DurationType.DayTimeDuration);
	}

	protected static TimeSpan StringToDuration(string value)
	{
		return new XsdDuration(value, XsdDuration.DurationType.Duration).ToTimeSpan(XsdDuration.DurationType.Duration);
	}

	protected static DateTime StringToGDay(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GDay);
	}

	protected static DateTime StringToGMonth(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GMonth);
	}

	protected static DateTime StringToGMonthDay(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GMonthDay);
	}

	protected static DateTime StringToGYear(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GYear);
	}

	protected static DateTime StringToGYearMonth(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GYearMonth);
	}

	protected static DateTimeOffset StringToDateOffset(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.Date);
	}

	protected static DateTimeOffset StringToDateTimeOffset(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.DateTime);
	}

	protected static DateTimeOffset StringToGDayOffset(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GDay);
	}

	protected static DateTimeOffset StringToGMonthOffset(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GMonth);
	}

	protected static DateTimeOffset StringToGMonthDayOffset(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GMonthDay);
	}

	protected static DateTimeOffset StringToGYearOffset(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GYear);
	}

	protected static DateTimeOffset StringToGYearMonthOffset(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GYearMonth);
	}

	protected static byte[] StringToHexBinary(string value)
	{
		try
		{
			return XmlConvert.FromBinHexString(XmlConvert.TrimString(value), allowOddCount: false);
		}
		catch (XmlException ex)
		{
			throw new FormatException(ex.Message);
		}
	}

	protected static XmlQualifiedName StringToQName(string value, IXmlNamespaceResolver nsResolver)
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

	protected static DateTime StringToTime(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.Time);
	}

	protected static DateTimeOffset StringToTimeOffset(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.Time);
	}

	protected static TimeSpan StringToYearMonthDuration(string value)
	{
		return new XsdDuration(value, XsdDuration.DurationType.YearMonthDuration).ToTimeSpan(XsdDuration.DurationType.YearMonthDuration);
	}

	protected static string AnyUriToString(Uri value)
	{
		return value.OriginalString;
	}

	protected static string Base64BinaryToString(byte[] value)
	{
		return Convert.ToBase64String(value);
	}

	protected static string DateToString(DateTime value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.Date).ToString();
	}

	protected static string DateTimeToString(DateTime value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.DateTime).ToString();
	}

	protected static string DayTimeDurationToString(TimeSpan value)
	{
		return new XsdDuration(value, XsdDuration.DurationType.DayTimeDuration).ToString(XsdDuration.DurationType.DayTimeDuration);
	}

	protected static string DurationToString(TimeSpan value)
	{
		return new XsdDuration(value, XsdDuration.DurationType.Duration).ToString(XsdDuration.DurationType.Duration);
	}

	protected static string GDayToString(DateTime value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GDay).ToString();
	}

	protected static string GMonthToString(DateTime value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GMonth).ToString();
	}

	protected static string GMonthDayToString(DateTime value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GMonthDay).ToString();
	}

	protected static string GYearToString(DateTime value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GYear).ToString();
	}

	protected static string GYearMonthToString(DateTime value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GYearMonth).ToString();
	}

	protected static string DateOffsetToString(DateTimeOffset value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.Date).ToString();
	}

	protected static string DateTimeOffsetToString(DateTimeOffset value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.DateTime).ToString();
	}

	protected static string GDayOffsetToString(DateTimeOffset value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GDay).ToString();
	}

	protected static string GMonthOffsetToString(DateTimeOffset value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GMonth).ToString();
	}

	protected static string GMonthDayOffsetToString(DateTimeOffset value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GMonthDay).ToString();
	}

	protected static string GYearOffsetToString(DateTimeOffset value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GYear).ToString();
	}

	protected static string GYearMonthOffsetToString(DateTimeOffset value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.GYearMonth).ToString();
	}

	protected static string QNameToString(XmlQualifiedName qname, IXmlNamespaceResolver nsResolver)
	{
		if (nsResolver == null)
		{
			return "{" + qname.Namespace + "}" + qname.Name;
		}
		string text = nsResolver.LookupPrefix(qname.Namespace);
		if (text == null)
		{
			throw new InvalidCastException(System.SR.Format(System.SR.XmlConvert_TypeNoPrefix, qname, qname.Namespace));
		}
		if (text.Length == 0)
		{
			return qname.Name;
		}
		return text + ":" + qname.Name;
	}

	protected static string TimeToString(DateTime value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.Time).ToString();
	}

	protected static string TimeOffsetToString(DateTimeOffset value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.Time).ToString();
	}

	protected static string YearMonthDurationToString(TimeSpan value)
	{
		return new XsdDuration(value, XsdDuration.DurationType.YearMonthDuration).ToString(XsdDuration.DurationType.YearMonthDuration);
	}

	internal static DateTime DateTimeOffsetToDateTime(DateTimeOffset value)
	{
		return value.LocalDateTime;
	}

	internal static int DecimalToInt32(decimal value)
	{
		if (value < -2147483648m || value > 2147483647m)
		{
			string xmlConvert_Overflow = System.SR.XmlConvert_Overflow;
			object[] args = new string[2]
			{
				XmlConvert.ToString(value),
				"Int32"
			};
			throw new OverflowException(System.SR.Format(xmlConvert_Overflow, args));
		}
		return (int)value;
	}

	protected static long DecimalToInt64(decimal value)
	{
		if (value < -9223372036854775808m || value > 9223372036854775807m)
		{
			string xmlConvert_Overflow = System.SR.XmlConvert_Overflow;
			object[] args = new string[2]
			{
				XmlConvert.ToString(value),
				"Int64"
			};
			throw new OverflowException(System.SR.Format(xmlConvert_Overflow, args));
		}
		return (long)value;
	}

	protected static ulong DecimalToUInt64(decimal value)
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

	protected static byte Int32ToByte(int value)
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

	protected static short Int32ToInt16(int value)
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

	protected static sbyte Int32ToSByte(int value)
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

	protected static ushort Int32ToUInt16(int value)
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

	protected static int Int64ToInt32(long value)
	{
		if (value < int.MinValue || value > int.MaxValue)
		{
			string xmlConvert_Overflow = System.SR.XmlConvert_Overflow;
			object[] args = new string[2]
			{
				XmlConvert.ToString(value),
				"Int32"
			};
			throw new OverflowException(System.SR.Format(xmlConvert_Overflow, args));
		}
		return (int)value;
	}

	protected static uint Int64ToUInt32(long value)
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

	protected static DateTime UntypedAtomicToDateTime(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.AllXsd);
	}

	protected static DateTimeOffset UntypedAtomicToDateTimeOffset(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.AllXsd);
	}
}
