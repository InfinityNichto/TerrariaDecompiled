namespace System.Xml.Schema;

internal sealed class Datatype_QNameXdr : Datatype_anySimpleType
{
	private static readonly Type s_atomicValueType = typeof(XmlQualifiedName);

	private static readonly Type s_listValueType = typeof(XmlQualifiedName[]);

	public override XmlTokenizedType TokenizedType => XmlTokenizedType.QName;

	public override Type ValueType => s_atomicValueType;

	internal override Type ListValueType => s_listValueType;

	public override object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr)
	{
		if (s == null || s.Length == 0)
		{
			throw new XmlSchemaException(System.SR.Sch_EmptyAttributeValue, string.Empty);
		}
		if (nsmgr == null)
		{
			throw new ArgumentNullException("nsmgr");
		}
		try
		{
			string prefix;
			return XmlQualifiedName.Parse(s.Trim(), nsmgr, out prefix);
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
}
