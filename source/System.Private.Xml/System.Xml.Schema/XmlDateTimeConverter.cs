namespace System.Xml.Schema;

internal sealed class XmlDateTimeConverter : XmlBaseConverter
{
	private XmlDateTimeConverter(XmlSchemaType schemaType)
		: base(schemaType)
	{
	}

	public static XmlValueConverter Create(XmlSchemaType schemaType)
	{
		return new XmlDateTimeConverter(schemaType);
	}

	public override DateTime ToDateTime(DateTimeOffset value)
	{
		return XmlBaseConverter.DateTimeOffsetToDateTime(value);
	}

	public override DateTime ToDateTime(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return base.TypeCode switch
		{
			XmlTypeCode.Date => XmlBaseConverter.StringToDate(value), 
			XmlTypeCode.Time => XmlBaseConverter.StringToTime(value), 
			XmlTypeCode.GDay => XmlBaseConverter.StringToGDay(value), 
			XmlTypeCode.GMonth => XmlBaseConverter.StringToGMonth(value), 
			XmlTypeCode.GMonthDay => XmlBaseConverter.StringToGMonthDay(value), 
			XmlTypeCode.GYear => XmlBaseConverter.StringToGYear(value), 
			XmlTypeCode.GYearMonth => XmlBaseConverter.StringToGYearMonth(value), 
			_ => XmlBaseConverter.StringToDateTime(value), 
		};
	}

	public override DateTime ToDateTime(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.DateTimeType)
		{
			return (DateTime)value;
		}
		if (type == XmlBaseConverter.DateTimeOffsetType)
		{
			return ToDateTime((DateTimeOffset)value);
		}
		if (type == XmlBaseConverter.StringType)
		{
			return ToDateTime((string)value);
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).ValueAsDateTime;
		}
		return (DateTime)ChangeListType(value, XmlBaseConverter.DateTimeType, null);
	}

	public override DateTimeOffset ToDateTimeOffset(DateTime value)
	{
		return new DateTimeOffset(value);
	}

	public override DateTimeOffset ToDateTimeOffset(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return base.TypeCode switch
		{
			XmlTypeCode.Date => XmlBaseConverter.StringToDateOffset(value), 
			XmlTypeCode.Time => XmlBaseConverter.StringToTimeOffset(value), 
			XmlTypeCode.GDay => XmlBaseConverter.StringToGDayOffset(value), 
			XmlTypeCode.GMonth => XmlBaseConverter.StringToGMonthOffset(value), 
			XmlTypeCode.GMonthDay => XmlBaseConverter.StringToGMonthDayOffset(value), 
			XmlTypeCode.GYear => XmlBaseConverter.StringToGYearOffset(value), 
			XmlTypeCode.GYearMonth => XmlBaseConverter.StringToGYearMonthOffset(value), 
			_ => XmlBaseConverter.StringToDateTimeOffset(value), 
		};
	}

	public override DateTimeOffset ToDateTimeOffset(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.DateTimeType)
		{
			return ToDateTimeOffset((DateTime)value);
		}
		if (type == XmlBaseConverter.DateTimeOffsetType)
		{
			return (DateTimeOffset)value;
		}
		if (type == XmlBaseConverter.StringType)
		{
			return ToDateTimeOffset((string)value);
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).ValueAsDateTime;
		}
		return (DateTimeOffset)ChangeListType(value, XmlBaseConverter.DateTimeOffsetType, null);
	}

	public override string ToString(DateTime value)
	{
		return base.TypeCode switch
		{
			XmlTypeCode.Date => XmlBaseConverter.DateToString(value), 
			XmlTypeCode.Time => XmlBaseConverter.TimeToString(value), 
			XmlTypeCode.GDay => XmlBaseConverter.GDayToString(value), 
			XmlTypeCode.GMonth => XmlBaseConverter.GMonthToString(value), 
			XmlTypeCode.GMonthDay => XmlBaseConverter.GMonthDayToString(value), 
			XmlTypeCode.GYear => XmlBaseConverter.GYearToString(value), 
			XmlTypeCode.GYearMonth => XmlBaseConverter.GYearMonthToString(value), 
			_ => XmlBaseConverter.DateTimeToString(value), 
		};
	}

	public override string ToString(DateTimeOffset value)
	{
		return base.TypeCode switch
		{
			XmlTypeCode.Date => XmlBaseConverter.DateOffsetToString(value), 
			XmlTypeCode.Time => XmlBaseConverter.TimeOffsetToString(value), 
			XmlTypeCode.GDay => XmlBaseConverter.GDayOffsetToString(value), 
			XmlTypeCode.GMonth => XmlBaseConverter.GMonthOffsetToString(value), 
			XmlTypeCode.GMonthDay => XmlBaseConverter.GMonthDayOffsetToString(value), 
			XmlTypeCode.GYear => XmlBaseConverter.GYearOffsetToString(value), 
			XmlTypeCode.GYearMonth => XmlBaseConverter.GYearMonthOffsetToString(value), 
			_ => XmlBaseConverter.DateTimeOffsetToString(value), 
		};
	}

	public override string ToString(object value, IXmlNamespaceResolver nsResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.DateTimeType)
		{
			return ToString((DateTime)value);
		}
		if (type == XmlBaseConverter.DateTimeOffsetType)
		{
			return ToString((DateTimeOffset)value);
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
		if (destinationType == XmlBaseConverter.DateTimeType)
		{
			return value;
		}
		if (destinationType == XmlBaseConverter.DateTimeOffsetType)
		{
			return ToDateTimeOffset(value);
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
		if (destinationType == XmlBaseConverter.DateTimeType)
		{
			return ToDateTime(value);
		}
		if (destinationType == XmlBaseConverter.DateTimeOffsetType)
		{
			return ToDateTimeOffset(value);
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
		if (destinationType == XmlBaseConverter.DateTimeType)
		{
			return ToDateTime(value);
		}
		if (destinationType == XmlBaseConverter.DateTimeOffsetType)
		{
			return ToDateTimeOffset(value);
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return ToString(value, nsResolver);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			if (type == XmlBaseConverter.DateTimeType)
			{
				return new XmlAtomicValue(base.SchemaType, (DateTime)value);
			}
			if (type == XmlBaseConverter.DateTimeOffsetType)
			{
				return new XmlAtomicValue(base.SchemaType, (DateTimeOffset)value);
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
			if (type == XmlBaseConverter.DateTimeType)
			{
				return new XmlAtomicValue(base.SchemaType, (DateTime)value);
			}
			if (type == XmlBaseConverter.DateTimeOffsetType)
			{
				return new XmlAtomicValue(base.SchemaType, (DateTimeOffset)value);
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
