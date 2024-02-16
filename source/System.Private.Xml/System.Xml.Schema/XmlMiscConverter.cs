using System.Xml.XPath;

namespace System.Xml.Schema;

internal sealed class XmlMiscConverter : XmlBaseConverter
{
	private XmlMiscConverter(XmlSchemaType schemaType)
		: base(schemaType)
	{
	}

	public static XmlValueConverter Create(XmlSchemaType schemaType)
	{
		return new XmlMiscConverter(schemaType);
	}

	public override string ToString(object value, IXmlNamespaceResolver nsResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == XmlBaseConverter.ByteArrayType)
		{
			switch (base.TypeCode)
			{
			case XmlTypeCode.Base64Binary:
				return XmlBaseConverter.Base64BinaryToString((byte[])value);
			case XmlTypeCode.HexBinary:
				return XmlConvert.ToBinHexString((byte[])value);
			}
		}
		if (type == XmlBaseConverter.StringType)
		{
			return (string)value;
		}
		if (XmlBaseConverter.IsDerivedFrom(type, XmlBaseConverter.UriType) && base.TypeCode == XmlTypeCode.AnyUri)
		{
			return XmlBaseConverter.AnyUriToString((Uri)value);
		}
		if (type == XmlBaseConverter.TimeSpanType)
		{
			switch (base.TypeCode)
			{
			case XmlTypeCode.DayTimeDuration:
				return XmlBaseConverter.DayTimeDurationToString((TimeSpan)value);
			case XmlTypeCode.Duration:
				return XmlBaseConverter.DurationToString((TimeSpan)value);
			case XmlTypeCode.YearMonthDuration:
				return XmlBaseConverter.YearMonthDurationToString((TimeSpan)value);
			}
		}
		if (XmlBaseConverter.IsDerivedFrom(type, XmlBaseConverter.XmlQualifiedNameType))
		{
			switch (base.TypeCode)
			{
			case XmlTypeCode.Notation:
				return XmlBaseConverter.QNameToString((XmlQualifiedName)value, nsResolver);
			case XmlTypeCode.QName:
				return XmlBaseConverter.QNameToString((XmlQualifiedName)value, nsResolver);
			}
		}
		return (string)ChangeTypeWildcardDestination(value, XmlBaseConverter.StringType, nsResolver);
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
		if (destinationType == XmlBaseConverter.ByteArrayType)
		{
			switch (base.TypeCode)
			{
			case XmlTypeCode.Base64Binary:
				return XmlBaseConverter.StringToBase64Binary(value);
			case XmlTypeCode.HexBinary:
				return XmlBaseConverter.StringToHexBinary(value);
			}
		}
		if (destinationType == XmlBaseConverter.XmlQualifiedNameType)
		{
			switch (base.TypeCode)
			{
			case XmlTypeCode.Notation:
				return XmlBaseConverter.StringToQName(value, nsResolver);
			case XmlTypeCode.QName:
				return XmlBaseConverter.StringToQName(value, nsResolver);
			}
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return value;
		}
		if (destinationType == XmlBaseConverter.TimeSpanType)
		{
			switch (base.TypeCode)
			{
			case XmlTypeCode.DayTimeDuration:
				return XmlBaseConverter.StringToDayTimeDuration(value);
			case XmlTypeCode.Duration:
				return XmlBaseConverter.StringToDuration(value);
			case XmlTypeCode.YearMonthDuration:
				return XmlBaseConverter.StringToYearMonthDuration(value);
			}
		}
		if (destinationType == XmlBaseConverter.UriType && base.TypeCode == XmlTypeCode.AnyUri)
		{
			return XmlConvert.ToUri(value);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			return new XmlAtomicValue(base.SchemaType, value, nsResolver);
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
		if (destinationType == XmlBaseConverter.ByteArrayType)
		{
			if (type == XmlBaseConverter.ByteArrayType)
			{
				switch (base.TypeCode)
				{
				case XmlTypeCode.Base64Binary:
					return (byte[])value;
				case XmlTypeCode.HexBinary:
					return (byte[])value;
				}
			}
			if (type == XmlBaseConverter.StringType)
			{
				switch (base.TypeCode)
				{
				case XmlTypeCode.Base64Binary:
					return XmlBaseConverter.StringToBase64Binary((string)value);
				case XmlTypeCode.HexBinary:
					return XmlBaseConverter.StringToHexBinary((string)value);
				}
			}
		}
		if (destinationType == XmlBaseConverter.XmlQualifiedNameType)
		{
			if (type == XmlBaseConverter.StringType)
			{
				switch (base.TypeCode)
				{
				case XmlTypeCode.Notation:
					return XmlBaseConverter.StringToQName((string)value, nsResolver);
				case XmlTypeCode.QName:
					return XmlBaseConverter.StringToQName((string)value, nsResolver);
				}
			}
			if (XmlBaseConverter.IsDerivedFrom(type, XmlBaseConverter.XmlQualifiedNameType))
			{
				switch (base.TypeCode)
				{
				case XmlTypeCode.Notation:
					return (XmlQualifiedName)value;
				case XmlTypeCode.QName:
					return (XmlQualifiedName)value;
				}
			}
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return ToString(value, nsResolver);
		}
		if (destinationType == XmlBaseConverter.TimeSpanType)
		{
			if (type == XmlBaseConverter.StringType)
			{
				switch (base.TypeCode)
				{
				case XmlTypeCode.DayTimeDuration:
					return XmlBaseConverter.StringToDayTimeDuration((string)value);
				case XmlTypeCode.Duration:
					return XmlBaseConverter.StringToDuration((string)value);
				case XmlTypeCode.YearMonthDuration:
					return XmlBaseConverter.StringToYearMonthDuration((string)value);
				}
			}
			if (type == XmlBaseConverter.TimeSpanType)
			{
				switch (base.TypeCode)
				{
				case XmlTypeCode.DayTimeDuration:
					return (TimeSpan)value;
				case XmlTypeCode.Duration:
					return (TimeSpan)value;
				case XmlTypeCode.YearMonthDuration:
					return (TimeSpan)value;
				}
			}
		}
		if (destinationType == XmlBaseConverter.UriType)
		{
			if (type == XmlBaseConverter.StringType && base.TypeCode == XmlTypeCode.AnyUri)
			{
				return XmlConvert.ToUri((string)value);
			}
			if (XmlBaseConverter.IsDerivedFrom(type, XmlBaseConverter.UriType) && base.TypeCode == XmlTypeCode.AnyUri)
			{
				return (Uri)value;
			}
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			if (type == XmlBaseConverter.ByteArrayType)
			{
				switch (base.TypeCode)
				{
				case XmlTypeCode.Base64Binary:
					return new XmlAtomicValue(base.SchemaType, value);
				case XmlTypeCode.HexBinary:
					return new XmlAtomicValue(base.SchemaType, value);
				}
			}
			if (type == XmlBaseConverter.StringType)
			{
				return new XmlAtomicValue(base.SchemaType, (string)value, nsResolver);
			}
			if (type == XmlBaseConverter.TimeSpanType)
			{
				switch (base.TypeCode)
				{
				case XmlTypeCode.DayTimeDuration:
					return new XmlAtomicValue(base.SchemaType, value);
				case XmlTypeCode.Duration:
					return new XmlAtomicValue(base.SchemaType, value);
				case XmlTypeCode.YearMonthDuration:
					return new XmlAtomicValue(base.SchemaType, value);
				}
			}
			if (XmlBaseConverter.IsDerivedFrom(type, XmlBaseConverter.UriType) && base.TypeCode == XmlTypeCode.AnyUri)
			{
				return new XmlAtomicValue(base.SchemaType, value);
			}
			if (type == XmlBaseConverter.XmlAtomicValueType)
			{
				return (XmlAtomicValue)value;
			}
			if (XmlBaseConverter.IsDerivedFrom(type, XmlBaseConverter.XmlQualifiedNameType))
			{
				switch (base.TypeCode)
				{
				case XmlTypeCode.Notation:
					return new XmlAtomicValue(base.SchemaType, value, nsResolver);
				case XmlTypeCode.QName:
					return new XmlAtomicValue(base.SchemaType, value, nsResolver);
				}
			}
		}
		if (destinationType == XmlBaseConverter.XPathItemType && type == XmlBaseConverter.XmlAtomicValueType)
		{
			return (XmlAtomicValue)value;
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return (XPathItem)ChangeType(value, XmlBaseConverter.XmlAtomicValueType, nsResolver);
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
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return (XPathItem)ChangeType(value, XmlBaseConverter.XmlAtomicValueType, nsResolver);
		}
		return ChangeListType(value, destinationType, nsResolver);
	}
}
