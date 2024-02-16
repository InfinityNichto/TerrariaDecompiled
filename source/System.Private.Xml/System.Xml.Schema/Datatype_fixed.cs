namespace System.Xml.Schema;

internal sealed class Datatype_fixed : Datatype_decimal
{
	public override object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr)
	{
		Exception ex;
		try
		{
			Numeric10FacetsChecker numeric10FacetsChecker = FacetsChecker as Numeric10FacetsChecker;
			decimal num = XmlConvert.ToDecimal(s);
			ex = numeric10FacetsChecker.CheckTotalAndFractionDigits(num, 18, 4, checkTotal: true, checkFraction: true);
			if (ex == null)
			{
				return num;
			}
		}
		catch (XmlSchemaException)
		{
			throw;
		}
		catch (Exception innerException)
		{
			throw new XmlSchemaException(System.SR.Format(System.SR.Sch_InvalidValue, s), innerException);
		}
		throw ex;
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		decimal result;
		Exception ex = XmlConvert.TryToDecimal(s, out result);
		if (ex == null)
		{
			Numeric10FacetsChecker numeric10FacetsChecker = FacetsChecker as Numeric10FacetsChecker;
			ex = numeric10FacetsChecker.CheckTotalAndFractionDigits(result, 18, 4, checkTotal: true, checkFraction: true);
			if (ex == null)
			{
				typedValue = result;
				return null;
			}
		}
		return ex;
	}
}
