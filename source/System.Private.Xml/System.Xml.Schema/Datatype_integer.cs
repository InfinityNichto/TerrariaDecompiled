namespace System.Xml.Schema;

internal class Datatype_integer : Datatype_decimal
{
	public override XmlTypeCode TypeCode => XmlTypeCode.Integer;

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		Exception ex = FacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			ex = XmlConvert.TryToInteger(s, out var result);
			if (ex == null)
			{
				ex = FacetsChecker.CheckValueFacets(result, this);
				if (ex == null)
				{
					typedValue = result;
					return null;
				}
			}
		}
		return ex;
	}
}
