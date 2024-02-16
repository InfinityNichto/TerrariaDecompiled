namespace System.Xml.Schema;

internal sealed class XmlNumeric2Converter : XmlBaseConverter
{
	private XmlNumeric2Converter(XmlSchemaType schemaType)
		: base(schemaType)
	{
	}

	public static XmlValueConverter Create(XmlSchemaType schemaType)
	{
		return new XmlNumeric2Converter(schemaType);
	}

	public override double ToDouble(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (base.TypeCode == XmlTypeCode.Float)
		{
			return XmlConvert.ToSingle(value);
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
		if (type == XmlBaseConverter.DoubleType)
		{
			return (double)value;
		}
		if (type == XmlBaseConverter.SingleType)
		{
			return (float)value;
		}
		if (type == XmlBaseConverter.StringType)
		{
			return ToDouble((string)value);
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).ValueAsDouble;
		}
		return (double)ChangeListType(value, XmlBaseConverter.DoubleType, null);
	}

	public override float ToSingle(double value)
	{
		return (float)value;
	}

	public override float ToSingle(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (base.TypeCode == XmlTypeCode.Float)
		{
			return XmlConvert.ToSingle(value);
		}
		return (float)XmlConvert.ToDouble(value);
	}

	public override float ToSingle(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.DoubleType)
		{
			return (float)(double)value;
		}
		if (type == XmlBaseConverter.SingleType)
		{
			return (float)value;
		}
		if (type == XmlBaseConverter.StringType)
		{
			return ToSingle((string)value);
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return (float)((XmlAtomicValue)value).ValueAs(XmlBaseConverter.SingleType);
		}
		return (float)ChangeListType(value, XmlBaseConverter.SingleType, null);
	}

	public override string ToString(double value)
	{
		if (base.TypeCode == XmlTypeCode.Float)
		{
			return XmlConvert.ToString(ToSingle(value));
		}
		return XmlConvert.ToString(value);
	}

	public override string ToString(float value)
	{
		if (base.TypeCode == XmlTypeCode.Float)
		{
			return XmlConvert.ToString(value);
		}
		return XmlConvert.ToString((double)value);
	}

	public override string ToString(object value, IXmlNamespaceResolver nsResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.DoubleType)
		{
			return ToString((double)value);
		}
		if (type == XmlBaseConverter.SingleType)
		{
			return ToString((float)value);
		}
		if (type == XmlBaseConverter.StringType)
		{
			return (string)value;
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).Value;
		}
		return (string)ChangeListType(value, XmlBaseConverter.StringType, nsResolver);
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
		if (destinationType == XmlBaseConverter.DoubleType)
		{
			return value;
		}
		if (destinationType == XmlBaseConverter.SingleType)
		{
			return (float)value;
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
		return ChangeListType(value, destinationType, null);
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
		if (destinationType == XmlBaseConverter.DoubleType)
		{
			return ToDouble(value);
		}
		if (destinationType == XmlBaseConverter.SingleType)
		{
			return ToSingle(value);
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
		return ChangeListType(value, destinationType, nsResolver);
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
		if (destinationType == XmlBaseConverter.DoubleType)
		{
			return ToDouble(value);
		}
		if (destinationType == XmlBaseConverter.SingleType)
		{
			return ToSingle(value);
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return ToString(value, nsResolver);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			if (type == XmlBaseConverter.DoubleType)
			{
				return new XmlAtomicValue(base.SchemaType, (double)value);
			}
			if (type == XmlBaseConverter.SingleType)
			{
				return new XmlAtomicValue(base.SchemaType, value);
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
			if (type == XmlBaseConverter.DoubleType)
			{
				return new XmlAtomicValue(base.SchemaType, (double)value);
			}
			if (type == XmlBaseConverter.SingleType)
			{
				return new XmlAtomicValue(base.SchemaType, value);
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
		return ChangeListType(value, destinationType, nsResolver);
	}
}
