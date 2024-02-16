namespace System.Xml.Schema;

internal sealed class Datatype_NOTATION : Datatype_anySimpleType
{
	private static readonly Type s_atomicValueType = typeof(XmlQualifiedName);

	private static readonly Type s_listValueType = typeof(XmlQualifiedName[]);

	internal override FacetsChecker FacetsChecker => DatatypeImplementation.qnameFacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.Notation;

	public override XmlTokenizedType TokenizedType => XmlTokenizedType.NOTATION;

	internal override RestrictionFlags ValidRestrictionFlags => RestrictionFlags.Length | RestrictionFlags.MinLength | RestrictionFlags.MaxLength | RestrictionFlags.Pattern | RestrictionFlags.Enumeration | RestrictionFlags.WhiteSpace;

	public override Type ValueType => s_atomicValueType;

	internal override Type ListValueType => s_listValueType;

	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Collapse;

	internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
	{
		return XmlMiscConverter.Create(schemaType);
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		if (s == null || s.Length == 0)
		{
			return new XmlSchemaException(System.SR.Sch_EmptyAttributeValue, string.Empty);
		}
		Exception ex = DatatypeImplementation.qnameFacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			XmlQualifiedName xmlQualifiedName = null;
			try
			{
				xmlQualifiedName = XmlQualifiedName.Parse(s, nsmgr, out var _);
			}
			catch (ArgumentException ex2)
			{
				ex = ex2;
				goto IL_0060;
			}
			catch (XmlException ex3)
			{
				ex = ex3;
				goto IL_0060;
			}
			ex = DatatypeImplementation.qnameFacetsChecker.CheckValueFacets(xmlQualifiedName, this);
			if (ex == null)
			{
				typedValue = xmlQualifiedName;
				return null;
			}
		}
		goto IL_0060;
		IL_0060:
		return ex;
	}

	internal override void VerifySchemaValid(XmlSchemaObjectTable notations, XmlSchemaObject caller)
	{
		for (Datatype_NOTATION datatype_NOTATION = this; datatype_NOTATION != null; datatype_NOTATION = (Datatype_NOTATION)datatype_NOTATION.Base)
		{
			if (datatype_NOTATION.Restriction != null && (datatype_NOTATION.Restriction.Flags & RestrictionFlags.Enumeration) != 0)
			{
				for (int i = 0; i < datatype_NOTATION.Restriction.Enumeration.Count; i++)
				{
					XmlQualifiedName name = (XmlQualifiedName)datatype_NOTATION.Restriction.Enumeration[i];
					if (!notations.Contains(name))
					{
						throw new XmlSchemaException(System.SR.Sch_NotationRequired, caller);
					}
				}
				return;
			}
		}
		throw new XmlSchemaException(System.SR.Sch_NotationRequired, caller);
	}
}
