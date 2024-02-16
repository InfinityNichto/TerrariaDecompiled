namespace System.Xml.Schema;

internal sealed class Datatype_uuid : Datatype_anySimpleType
{
	private static readonly Type s_atomicValueType = typeof(Guid);

	private static readonly Type s_listValueType = typeof(Guid[]);

	public override Type ValueType => s_atomicValueType;

	internal override Type ListValueType => s_listValueType;

	internal override RestrictionFlags ValidRestrictionFlags => (RestrictionFlags)0;

	internal override int Compare(object value1, object value2)
	{
		if (!((Guid)value1).Equals(value2))
		{
			return -1;
		}
		return 0;
	}

	public override object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr)
	{
		try
		{
			return XmlConvert.ToGuid(s);
		}
		catch (XmlSchemaException)
		{
			throw;
		}
		catch (Exception innerException)
		{
			throw new XmlSchemaException(System.SR.Format(System.SR.Sch_InvalidValue, s), innerException);
		}
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		Guid result;
		Exception ex = XmlConvert.TryToGuid(s, out result);
		if (ex == null)
		{
			typedValue = result;
			return null;
		}
		return ex;
	}
}
