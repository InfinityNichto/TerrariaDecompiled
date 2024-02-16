namespace System.Xml.Schema;

internal sealed class XmlUntypedConverter : XmlListConverter
{
	private readonly bool _allowListToList;

	public static readonly XmlValueConverter Untyped = new XmlUntypedConverter(new XmlUntypedConverter(), allowListToList: false);

	public static readonly XmlValueConverter UntypedList = new XmlUntypedConverter(new XmlUntypedConverter(), allowListToList: true);

	private XmlUntypedConverter()
		: base(DatatypeImplementation.UntypedAtomicType)
	{
	}

	private XmlUntypedConverter(XmlUntypedConverter atomicConverter, bool allowListToList)
		: base(atomicConverter, allowListToList ? XmlBaseConverter.StringArrayType : XmlBaseConverter.StringType)
	{
		_allowListToList = allowListToList;
	}

	public override bool ToBoolean(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return XmlConvert.ToBoolean(value);
	}

	public override bool ToBoolean(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToBoolean((string)value);
		}
		return (bool)ChangeTypeWildcardDestination(value, XmlBaseConverter.BooleanType, null);
	}

	public override DateTime ToDateTime(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return XmlBaseConverter.UntypedAtomicToDateTime(value);
	}

	public override DateTime ToDateTime(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.UntypedAtomicToDateTime((string)value);
		}
		return (DateTime)ChangeTypeWildcardDestination(value, XmlBaseConverter.DateTimeType, null);
	}

	public override DateTimeOffset ToDateTimeOffset(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return XmlBaseConverter.UntypedAtomicToDateTimeOffset(value);
	}

	public override DateTimeOffset ToDateTimeOffset(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.UntypedAtomicToDateTimeOffset((string)value);
		}
		return (DateTimeOffset)ChangeTypeWildcardDestination(value, XmlBaseConverter.DateTimeOffsetType, null);
	}

	public override decimal ToDecimal(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return XmlConvert.ToDecimal(value);
	}

	public override decimal ToDecimal(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToDecimal((string)value);
		}
		return (decimal)ChangeTypeWildcardDestination(value, XmlBaseConverter.DecimalType, null);
	}

	public override double ToDouble(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return XmlConvert.ToDouble(value);
	}

	public override double ToDouble(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToDouble((string)value);
		}
		return (double)ChangeTypeWildcardDestination(value, XmlBaseConverter.DoubleType, null);
	}

	public override int ToInt32(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return XmlConvert.ToInt32(value);
	}

	public override int ToInt32(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToInt32((string)value);
		}
		return (int)ChangeTypeWildcardDestination(value, XmlBaseConverter.Int32Type, null);
	}

	public override long ToInt64(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return XmlConvert.ToInt64(value);
	}

	public override long ToInt64(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToInt64((string)value);
		}
		return (long)ChangeTypeWildcardDestination(value, XmlBaseConverter.Int64Type, null);
	}

	public override float ToSingle(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return XmlConvert.ToSingle(value);
	}

	public override float ToSingle(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToSingle((string)value);
		}
		return (float)ChangeTypeWildcardDestination(value, XmlBaseConverter.SingleType, null);
	}

	public override string ToString(bool value)
	{
		return XmlConvert.ToString(value);
	}

	public override string ToString(DateTime value)
	{
		return XmlBaseConverter.DateTimeToString(value);
	}

	public override string ToString(DateTimeOffset value)
	{
		return XmlBaseConverter.DateTimeOffsetToString(value);
	}

	public override string ToString(decimal value)
	{
		return XmlConvert.ToString(value);
	}

	public override string ToString(double value)
	{
		return XmlConvert.ToString(value);
	}

	public override string ToString(int value)
	{
		return XmlConvert.ToString(value);
	}

	public override string ToString(long value)
	{
		return XmlConvert.ToString(value);
	}

	public override string ToString(float value)
	{
		return XmlConvert.ToString(value);
	}

	public override string ToString(object value, IXmlNamespaceResolver nsResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.BooleanType)
		{
			return XmlConvert.ToString((bool)value);
		}
		if (type == XmlBaseConverter.ByteType)
		{
			return XmlConvert.ToString((byte)value);
		}
		if (type == XmlBaseConverter.ByteArrayType)
		{
			return XmlBaseConverter.Base64BinaryToString((byte[])value);
		}
		if (type == XmlBaseConverter.DateTimeType)
		{
			return XmlBaseConverter.DateTimeToString((DateTime)value);
		}
		if (type == XmlBaseConverter.DateTimeOffsetType)
		{
			return XmlBaseConverter.DateTimeOffsetToString((DateTimeOffset)value);
		}
		if (type == XmlBaseConverter.DecimalType)
		{
			return XmlConvert.ToString((decimal)value);
		}
		if (type == XmlBaseConverter.DoubleType)
		{
			return XmlConvert.ToString((double)value);
		}
		if (type == XmlBaseConverter.Int16Type)
		{
			return XmlConvert.ToString((short)value);
		}
		if (type == XmlBaseConverter.Int32Type)
		{
			return XmlConvert.ToString((int)value);
		}
		if (type == XmlBaseConverter.Int64Type)
		{
			return XmlConvert.ToString((long)value);
		}
		if (type == XmlBaseConverter.SByteType)
		{
			return XmlConvert.ToString((sbyte)value);
		}
		if (type == XmlBaseConverter.SingleType)
		{
			return XmlConvert.ToString((float)value);
		}
		if (type == XmlBaseConverter.StringType)
		{
			return (string)value;
		}
		if (type == XmlBaseConverter.TimeSpanType)
		{
			return XmlBaseConverter.DurationToString((TimeSpan)value);
		}
		if (type == XmlBaseConverter.UInt16Type)
		{
			return XmlConvert.ToString((ushort)value);
		}
		if (type == XmlBaseConverter.UInt32Type)
		{
			return XmlConvert.ToString((uint)value);
		}
		if (type == XmlBaseConverter.UInt64Type)
		{
			return XmlConvert.ToString((ulong)value);
		}
		if (XmlBaseConverter.IsDerivedFrom(type, XmlBaseConverter.UriType))
		{
			return XmlBaseConverter.AnyUriToString((Uri)value);
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return (string)((XmlAtomicValue)value).ValueAs(XmlBaseConverter.StringType, nsResolver);
		}
		if (XmlBaseConverter.IsDerivedFrom(type, XmlBaseConverter.XmlQualifiedNameType))
		{
			return XmlBaseConverter.QNameToString((XmlQualifiedName)value, nsResolver);
		}
		return (string)ChangeTypeWildcardDestination(value, XmlBaseConverter.StringType, nsResolver);
	}

	public override object ChangeType(bool value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToString(value);
		}
		return ChangeTypeWildcardSource(value, destinationType, null);
	}

	public override object ChangeType(DateTime value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.DateTimeToString(value);
		}
		return ChangeTypeWildcardSource(value, destinationType, null);
	}

	public override object ChangeType(decimal value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToString(value);
		}
		return ChangeTypeWildcardSource(value, destinationType, null);
	}

	public override object ChangeType(double value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToString(value);
		}
		return ChangeTypeWildcardSource(value, destinationType, null);
	}

	public override object ChangeType(int value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToString(value);
		}
		return ChangeTypeWildcardSource(value, destinationType, null);
	}

	public override object ChangeType(long value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToString(value);
		}
		return ChangeTypeWildcardSource(value, destinationType, null);
	}

	public override object ChangeType(string value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.BooleanType)
		{
			return XmlConvert.ToBoolean(value);
		}
		if (destinationType == XmlBaseConverter.ByteType)
		{
			return XmlBaseConverter.Int32ToByte(XmlConvert.ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.ByteArrayType)
		{
			return XmlBaseConverter.StringToBase64Binary(value);
		}
		if (destinationType == XmlBaseConverter.DateTimeType)
		{
			return XmlBaseConverter.UntypedAtomicToDateTime(value);
		}
		if (destinationType == XmlBaseConverter.DateTimeOffsetType)
		{
			return XmlBaseConverter.UntypedAtomicToDateTimeOffset(value);
		}
		if (destinationType == XmlBaseConverter.DecimalType)
		{
			return XmlConvert.ToDecimal(value);
		}
		if (destinationType == XmlBaseConverter.DoubleType)
		{
			return XmlConvert.ToDouble(value);
		}
		if (destinationType == XmlBaseConverter.Int16Type)
		{
			return XmlBaseConverter.Int32ToInt16(XmlConvert.ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.Int32Type)
		{
			return XmlConvert.ToInt32(value);
		}
		if (destinationType == XmlBaseConverter.Int64Type)
		{
			return XmlConvert.ToInt64(value);
		}
		if (destinationType == XmlBaseConverter.SByteType)
		{
			return XmlBaseConverter.Int32ToSByte(XmlConvert.ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.SingleType)
		{
			return XmlConvert.ToSingle(value);
		}
		if (destinationType == XmlBaseConverter.TimeSpanType)
		{
			return XmlBaseConverter.StringToDuration(value);
		}
		if (destinationType == XmlBaseConverter.UInt16Type)
		{
			return XmlBaseConverter.Int32ToUInt16(XmlConvert.ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.UInt32Type)
		{
			return XmlBaseConverter.Int64ToUInt32(XmlConvert.ToInt64(value));
		}
		if (destinationType == XmlBaseConverter.UInt64Type)
		{
			return XmlBaseConverter.DecimalToUInt64(XmlConvert.ToDecimal(value));
		}
		if (destinationType == XmlBaseConverter.UriType)
		{
			return XmlConvert.ToUri(value);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
		}
		if (destinationType == XmlBaseConverter.XmlQualifiedNameType)
		{
			return XmlBaseConverter.StringToQName(value, nsResolver);
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return value;
		}
		return ChangeTypeWildcardSource(value, destinationType, nsResolver);
	}

	public override object ChangeType(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		Type type = value.GetType();
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.BooleanType && type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToBoolean((string)value);
		}
		if (destinationType == XmlBaseConverter.ByteType && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.Int32ToByte(XmlConvert.ToInt32((string)value));
		}
		if (destinationType == XmlBaseConverter.ByteArrayType && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.StringToBase64Binary((string)value);
		}
		if (destinationType == XmlBaseConverter.DateTimeType && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.UntypedAtomicToDateTime((string)value);
		}
		if (destinationType == XmlBaseConverter.DateTimeOffsetType && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.UntypedAtomicToDateTimeOffset((string)value);
		}
		if (destinationType == XmlBaseConverter.DecimalType && type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToDecimal((string)value);
		}
		if (destinationType == XmlBaseConverter.DoubleType && type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToDouble((string)value);
		}
		if (destinationType == XmlBaseConverter.Int16Type && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.Int32ToInt16(XmlConvert.ToInt32((string)value));
		}
		if (destinationType == XmlBaseConverter.Int32Type && type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToInt32((string)value);
		}
		if (destinationType == XmlBaseConverter.Int64Type && type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToInt64((string)value);
		}
		if (destinationType == XmlBaseConverter.SByteType && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.Int32ToSByte(XmlConvert.ToInt32((string)value));
		}
		if (destinationType == XmlBaseConverter.SingleType && type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToSingle((string)value);
		}
		if (destinationType == XmlBaseConverter.TimeSpanType && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.StringToDuration((string)value);
		}
		if (destinationType == XmlBaseConverter.UInt16Type && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.Int32ToUInt16(XmlConvert.ToInt32((string)value));
		}
		if (destinationType == XmlBaseConverter.UInt32Type && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.Int64ToUInt32(XmlConvert.ToInt64((string)value));
		}
		if (destinationType == XmlBaseConverter.UInt64Type && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.DecimalToUInt64(XmlConvert.ToDecimal((string)value));
		}
		if (destinationType == XmlBaseConverter.UriType && type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToUri((string)value);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			if (type == XmlBaseConverter.StringType)
			{
				return new XmlAtomicValue(base.SchemaType, (string)value);
			}
			if (type == XmlBaseConverter.XmlAtomicValueType)
			{
				return (XmlAtomicValue)value;
			}
		}
		if (destinationType == XmlBaseConverter.XmlQualifiedNameType && type == XmlBaseConverter.StringType)
		{
			return XmlBaseConverter.StringToQName((string)value, nsResolver);
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			if (type == XmlBaseConverter.StringType)
			{
				return new XmlAtomicValue(base.SchemaType, (string)value);
			}
			if (type == XmlBaseConverter.XmlAtomicValueType)
			{
				return (XmlAtomicValue)value;
			}
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return ToString(value, nsResolver);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			return new XmlAtomicValue(base.SchemaType, ToString(value, nsResolver));
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return new XmlAtomicValue(base.SchemaType, ToString(value, nsResolver));
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).ValueAs(destinationType, nsResolver);
		}
		return ChangeListType(value, destinationType, nsResolver);
	}

	private object ChangeTypeWildcardDestination(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		Type type = value.GetType();
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).ValueAs(destinationType, nsResolver);
		}
		return ChangeListType(value, destinationType, nsResolver);
	}

	private object ChangeTypeWildcardSource(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			return new XmlAtomicValue(base.SchemaType, ToString(value, nsResolver));
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return new XmlAtomicValue(base.SchemaType, ToString(value, nsResolver));
		}
		return ChangeListType(value, destinationType, nsResolver);
	}

	protected override object ChangeListType(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		Type type = value.GetType();
		if (atomicConverter == null || (!_allowListToList && type != XmlBaseConverter.StringType && destinationType != XmlBaseConverter.StringType))
		{
			if (SupportsType(type))
			{
				throw new InvalidCastException(System.SR.Format(System.SR.XmlConvert_TypeToString, base.XmlTypeName, type.Name));
			}
			if (SupportsType(destinationType))
			{
				throw new InvalidCastException(System.SR.Format(System.SR.XmlConvert_TypeFromString, base.XmlTypeName, destinationType.Name));
			}
			throw CreateInvalidClrMappingException(type, destinationType);
		}
		return base.ChangeListType(value, destinationType, nsResolver);
	}

	private bool SupportsType(Type clrType)
	{
		if (clrType == XmlBaseConverter.BooleanType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.ByteType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.ByteArrayType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.DateTimeType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.DateTimeOffsetType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.DecimalType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.DoubleType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.Int16Type)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.Int32Type)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.Int64Type)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.SByteType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.SingleType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.TimeSpanType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.UInt16Type)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.UInt32Type)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.UInt64Type)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.UriType)
		{
			return true;
		}
		if (clrType == XmlBaseConverter.XmlQualifiedNameType)
		{
			return true;
		}
		return false;
	}
}
