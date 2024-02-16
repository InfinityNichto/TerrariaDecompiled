namespace System.Xml.Schema;

internal sealed class Datatype_doubleXdr : Datatype_double
{
	public override object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr)
	{
		double num;
		try
		{
			num = XmlConvert.ToDouble(s);
		}
		catch (Exception innerException)
		{
			throw new XmlSchemaException(System.SR.Format(System.SR.Sch_InvalidValue, s), innerException);
		}
		if (!double.IsFinite(num))
		{
			throw new XmlSchemaException(System.SR.Sch_InvalidValue, s);
		}
		return num;
	}
}
