namespace System.Xml.Schema;

internal sealed class Datatype_floatXdr : Datatype_float
{
	public override object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr)
	{
		float num;
		try
		{
			num = XmlConvert.ToSingle(s);
		}
		catch (Exception innerException)
		{
			throw new XmlSchemaException(System.SR.Format(System.SR.Sch_InvalidValue, s), innerException);
		}
		if (!float.IsFinite(num))
		{
			throw new XmlSchemaException(System.SR.Sch_InvalidValue, s);
		}
		return num;
	}
}
