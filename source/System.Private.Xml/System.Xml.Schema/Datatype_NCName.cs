namespace System.Xml.Schema;

internal class Datatype_NCName : Datatype_Name
{
	public override XmlTypeCode TypeCode => XmlTypeCode.NCName;

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		Exception ex = DatatypeImplementation.stringFacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			ex = DatatypeImplementation.stringFacetsChecker.CheckValueFacets(s, this);
			if (ex == null)
			{
				nameTable.Add(s);
				typedValue = s;
				return null;
			}
		}
		return ex;
	}
}
