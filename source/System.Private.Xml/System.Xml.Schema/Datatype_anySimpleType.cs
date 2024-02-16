namespace System.Xml.Schema;

internal class Datatype_anySimpleType : DatatypeImplementation
{
	private static readonly Type s_atomicValueType = typeof(string);

	private static readonly Type s_listValueType = typeof(string[]);

	internal override FacetsChecker FacetsChecker => DatatypeImplementation.miscFacetsChecker;

	public override Type ValueType => s_atomicValueType;

	public override XmlTypeCode TypeCode => XmlTypeCode.AnyAtomicType;

	internal override Type ListValueType => s_listValueType;

	public override XmlTokenizedType TokenizedType => XmlTokenizedType.None;

	internal override RestrictionFlags ValidRestrictionFlags => (RestrictionFlags)0;

	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Collapse;

	internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
	{
		return XmlUntypedConverter.Untyped;
	}

	internal override int Compare(object value1, object value2)
	{
		return string.Compare(value1.ToString(), value2.ToString(), StringComparison.Ordinal);
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = XmlComplianceUtil.NonCDataNormalize(s);
		return null;
	}
}
