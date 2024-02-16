namespace System.Xml.Schema;

internal sealed class Datatype_yearMonthDuration : Datatype_duration
{
	public override XmlTypeCode TypeCode => XmlTypeCode.YearMonthDuration;

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		if (s == null || s.Length == 0)
		{
			return new XmlSchemaException(System.SR.Sch_EmptyAttributeValue, string.Empty);
		}
		Exception ex = DatatypeImplementation.durationFacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			ex = XsdDuration.TryParse(s, XsdDuration.DurationType.YearMonthDuration, out var result);
			if (ex == null)
			{
				ex = result.TryToTimeSpan(XsdDuration.DurationType.YearMonthDuration, out var result2);
				if (ex == null)
				{
					ex = DatatypeImplementation.durationFacetsChecker.CheckValueFacets(result2, this);
					if (ex == null)
					{
						typedValue = result2;
						return null;
					}
				}
			}
		}
		return ex;
	}
}
