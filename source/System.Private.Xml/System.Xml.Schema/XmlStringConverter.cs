namespace System.Xml.Schema;

internal sealed class XmlStringConverter : XmlBaseConverter
{
	private XmlStringConverter(XmlSchemaType schemaType)
		: base(schemaType)
	{
	}

	public static XmlValueConverter Create(XmlSchemaType schemaType)
	{
		return new XmlStringConverter(schemaType);
	}

	public override string ToString(object value, IXmlNamespaceResolver nsResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Type type = value.GetType();
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
		if (destinationType == XmlBaseConverter.StringType)
		{
			return ToString(value, nsResolver);
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
		return ChangeListType(value, destinationType, nsResolver);
	}
}
