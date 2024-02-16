namespace System.Xml.Schema;

internal sealed class XmlUnionConverter : XmlBaseConverter
{
	private readonly XmlValueConverter[] _converters;

	private readonly bool _hasAtomicMember;

	private readonly bool _hasListMember;

	private XmlUnionConverter(XmlSchemaType schemaType)
		: base(schemaType)
	{
		while (schemaType.DerivedBy == XmlSchemaDerivationMethod.Restriction)
		{
			schemaType = schemaType.BaseXmlSchemaType;
		}
		XmlSchemaSimpleType[] baseMemberTypes = ((XmlSchemaSimpleTypeUnion)((XmlSchemaSimpleType)schemaType).Content).BaseMemberTypes;
		_converters = new XmlValueConverter[baseMemberTypes.Length];
		for (int i = 0; i < baseMemberTypes.Length; i++)
		{
			_converters[i] = baseMemberTypes[i].ValueConverter;
			if (baseMemberTypes[i].Datatype.Variety == XmlSchemaDatatypeVariety.List)
			{
				_hasListMember = true;
			}
			else if (baseMemberTypes[i].Datatype.Variety == XmlSchemaDatatypeVariety.Atomic)
			{
				_hasAtomicMember = true;
			}
		}
	}

	public static XmlValueConverter Create(XmlSchemaType schemaType)
	{
		return new XmlUnionConverter(schemaType);
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
		if (type == XmlBaseConverter.XmlAtomicValueType && _hasAtomicMember)
		{
			return ((XmlAtomicValue)value).ValueAs(destinationType, nsResolver);
		}
		if (type == XmlBaseConverter.XmlAtomicValueArrayType && _hasListMember)
		{
			return XmlAnyListConverter.ItemList.ChangeType(value, destinationType, nsResolver);
		}
		if (type == XmlBaseConverter.StringType)
		{
			if (destinationType == XmlBaseConverter.StringType)
			{
				return value;
			}
			XsdSimpleValue xsdSimpleValue = (XsdSimpleValue)base.SchemaType.Datatype.ParseValue((string)value, new NameTable(), nsResolver, createAtomicValue: true);
			return xsdSimpleValue.XmlType.ValueConverter.ChangeType((string)value, destinationType, nsResolver);
		}
		throw CreateInvalidClrMappingException(type, destinationType);
	}
}
