namespace System.Xml.Schema;

internal sealed class XmlNumeric10Converter : XmlBaseConverter
{
	private XmlNumeric10Converter(XmlSchemaType schemaType)
		: base(schemaType)
	{
	}

	public static XmlValueConverter Create(XmlSchemaType schemaType)
	{
		return new XmlNumeric10Converter(schemaType);
	}

	public override decimal ToDecimal(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (base.TypeCode == XmlTypeCode.Decimal)
		{
			return XmlConvert.ToDecimal(value);
		}
		return XmlConvert.ToInteger(value);
	}

	public override decimal ToDecimal(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.DecimalType)
		{
			return (decimal)value;
		}
		if (type == XmlBaseConverter.Int32Type)
		{
			return (int)value;
		}
		if (type == XmlBaseConverter.Int64Type)
		{
			return (long)value;
		}
		if (type == XmlBaseConverter.StringType)
		{
			return ToDecimal((string)value);
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return (decimal)((XmlAtomicValue)value).ValueAs(XmlBaseConverter.DecimalType);
		}
		return (decimal)ChangeTypeWildcardDestination(value, XmlBaseConverter.DecimalType, null);
	}

	public override int ToInt32(long value)
	{
		return XmlBaseConverter.Int64ToInt32(value);
	}

	public override int ToInt32(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (base.TypeCode == XmlTypeCode.Decimal)
		{
			return XmlBaseConverter.DecimalToInt32(XmlConvert.ToDecimal(value));
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
		if (type == XmlBaseConverter.DecimalType)
		{
			return XmlBaseConverter.DecimalToInt32((decimal)value);
		}
		if (type == XmlBaseConverter.Int32Type)
		{
			return (int)value;
		}
		if (type == XmlBaseConverter.Int64Type)
		{
			return XmlBaseConverter.Int64ToInt32((long)value);
		}
		if (type == XmlBaseConverter.StringType)
		{
			return ToInt32((string)value);
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).ValueAsInt;
		}
		return (int)ChangeTypeWildcardDestination(value, XmlBaseConverter.Int32Type, null);
	}

	public override long ToInt64(int value)
	{
		return value;
	}

	public override long ToInt64(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (base.TypeCode == XmlTypeCode.Decimal)
		{
			return XmlBaseConverter.DecimalToInt64(XmlConvert.ToDecimal(value));
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
		if (type == XmlBaseConverter.DecimalType)
		{
			return XmlBaseConverter.DecimalToInt64((decimal)value);
		}
		if (type == XmlBaseConverter.Int32Type)
		{
			return (int)value;
		}
		if (type == XmlBaseConverter.Int64Type)
		{
			return (long)value;
		}
		if (type == XmlBaseConverter.StringType)
		{
			return ToInt64((string)value);
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).ValueAsLong;
		}
		return (long)ChangeTypeWildcardDestination(value, XmlBaseConverter.Int64Type, null);
	}

	public override string ToString(decimal value)
	{
		if (base.TypeCode == XmlTypeCode.Decimal)
		{
			return XmlConvert.ToString(value);
		}
		return XmlConvert.ToString(decimal.Truncate(value));
	}

	public override string ToString(int value)
	{
		return XmlConvert.ToString(value);
	}

	public override string ToString(long value)
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
		if (type == XmlBaseConverter.DecimalType)
		{
			return ToString((decimal)value);
		}
		if (type == XmlBaseConverter.Int32Type)
		{
			return XmlConvert.ToString((int)value);
		}
		if (type == XmlBaseConverter.Int64Type)
		{
			return XmlConvert.ToString((long)value);
		}
		if (type == XmlBaseConverter.StringType)
		{
			return (string)value;
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).Value;
		}
		return (string)ChangeTypeWildcardDestination(value, XmlBaseConverter.StringType, nsResolver);
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
		if (destinationType == XmlBaseConverter.DecimalType)
		{
			return value;
		}
		if (destinationType == XmlBaseConverter.Int32Type)
		{
			return XmlBaseConverter.DecimalToInt32(value);
		}
		if (destinationType == XmlBaseConverter.Int64Type)
		{
			return XmlBaseConverter.DecimalToInt64(value);
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return ToString(value);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
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
		if (destinationType == XmlBaseConverter.DecimalType)
		{
			return (decimal)value;
		}
		if (destinationType == XmlBaseConverter.Int32Type)
		{
			return value;
		}
		if (destinationType == XmlBaseConverter.Int64Type)
		{
			return (long)value;
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToString(value);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
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
		if (destinationType == XmlBaseConverter.DecimalType)
		{
			return (decimal)value;
		}
		if (destinationType == XmlBaseConverter.Int32Type)
		{
			return XmlBaseConverter.Int64ToInt32(value);
		}
		if (destinationType == XmlBaseConverter.Int64Type)
		{
			return value;
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToString(value);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
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
		if (destinationType == XmlBaseConverter.DecimalType)
		{
			return ToDecimal(value);
		}
		if (destinationType == XmlBaseConverter.Int32Type)
		{
			return ToInt32(value);
		}
		if (destinationType == XmlBaseConverter.Int64Type)
		{
			return ToInt64(value);
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return value;
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
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
		if (destinationType == XmlBaseConverter.DecimalType)
		{
			return ToDecimal(value);
		}
		if (destinationType == XmlBaseConverter.Int32Type)
		{
			return ToInt32(value);
		}
		if (destinationType == XmlBaseConverter.Int64Type)
		{
			return ToInt64(value);
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return ToString(value, nsResolver);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			if (type == XmlBaseConverter.DecimalType)
			{
				return new XmlAtomicValue(base.SchemaType, value);
			}
			if (type == XmlBaseConverter.Int32Type)
			{
				return new XmlAtomicValue(base.SchemaType, (int)value);
			}
			if (type == XmlBaseConverter.Int64Type)
			{
				return new XmlAtomicValue(base.SchemaType, (long)value);
			}
			if (type == XmlBaseConverter.StringType)
			{
				return new XmlAtomicValue(base.SchemaType, (string)value);
			}
			if (type == XmlBaseConverter.XmlAtomicValueType)
			{
				return (XmlAtomicValue)value;
			}
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			if (type == XmlBaseConverter.DecimalType)
			{
				return new XmlAtomicValue(base.SchemaType, value);
			}
			if (type == XmlBaseConverter.Int32Type)
			{
				return new XmlAtomicValue(base.SchemaType, (int)value);
			}
			if (type == XmlBaseConverter.Int64Type)
			{
				return new XmlAtomicValue(base.SchemaType, (long)value);
			}
			if (type == XmlBaseConverter.StringType)
			{
				return new XmlAtomicValue(base.SchemaType, (string)value);
			}
			if (type == XmlBaseConverter.XmlAtomicValueType)
			{
				return (XmlAtomicValue)value;
			}
		}
		if (destinationType == XmlBaseConverter.ByteType)
		{
			return XmlBaseConverter.Int32ToByte(ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.Int16Type)
		{
			return XmlBaseConverter.Int32ToInt16(ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.SByteType)
		{
			return XmlBaseConverter.Int32ToSByte(ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.UInt16Type)
		{
			return XmlBaseConverter.Int32ToUInt16(ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.UInt32Type)
		{
			return XmlBaseConverter.Int64ToUInt32(ToInt64(value));
		}
		if (destinationType == XmlBaseConverter.UInt64Type)
		{
			return XmlBaseConverter.DecimalToUInt64(ToDecimal(value));
		}
		if (type == XmlBaseConverter.ByteType)
		{
			return ChangeType((byte)value, destinationType);
		}
		if (type == XmlBaseConverter.Int16Type)
		{
			return ChangeType((short)value, destinationType);
		}
		if (type == XmlBaseConverter.SByteType)
		{
			return ChangeType((sbyte)value, destinationType);
		}
		if (type == XmlBaseConverter.UInt16Type)
		{
			return ChangeType((ushort)value, destinationType);
		}
		if (type == XmlBaseConverter.UInt32Type)
		{
			return ChangeType((uint)value, destinationType);
		}
		if (type == XmlBaseConverter.UInt64Type)
		{
			return ChangeType((decimal)(ulong)value, destinationType);
		}
		return ChangeListType(value, destinationType, nsResolver);
	}

	private object ChangeTypeWildcardDestination(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		Type type = value.GetType();
		if (type == XmlBaseConverter.ByteType)
		{
			return ChangeType((byte)value, destinationType);
		}
		if (type == XmlBaseConverter.Int16Type)
		{
			return ChangeType((short)value, destinationType);
		}
		if (type == XmlBaseConverter.SByteType)
		{
			return ChangeType((sbyte)value, destinationType);
		}
		if (type == XmlBaseConverter.UInt16Type)
		{
			return ChangeType((ushort)value, destinationType);
		}
		if (type == XmlBaseConverter.UInt32Type)
		{
			return ChangeType((uint)value, destinationType);
		}
		if (type == XmlBaseConverter.UInt64Type)
		{
			return ChangeType((decimal)(ulong)value, destinationType);
		}
		return ChangeListType(value, destinationType, nsResolver);
	}

	private object ChangeTypeWildcardSource(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		if (destinationType == XmlBaseConverter.ByteType)
		{
			return XmlBaseConverter.Int32ToByte(ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.Int16Type)
		{
			return XmlBaseConverter.Int32ToInt16(ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.SByteType)
		{
			return XmlBaseConverter.Int32ToSByte(ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.UInt16Type)
		{
			return XmlBaseConverter.Int32ToUInt16(ToInt32(value));
		}
		if (destinationType == XmlBaseConverter.UInt32Type)
		{
			return XmlBaseConverter.Int64ToUInt32(ToInt64(value));
		}
		if (destinationType == XmlBaseConverter.UInt64Type)
		{
			return XmlBaseConverter.DecimalToUInt64(ToDecimal(value));
		}
		return ChangeListType(value, destinationType, nsResolver);
	}
}
