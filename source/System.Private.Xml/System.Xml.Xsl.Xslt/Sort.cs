namespace System.Xml.Xsl.Xslt;

internal sealed class Sort : XslNode
{
	public readonly string Lang;

	public readonly string DataType;

	public readonly string Order;

	public readonly string CaseOrder;

	public Sort(string select, string lang, string dataType, string order, string caseOrder, XslVersion xslVer)
		: base(XslNodeType.Sort, null, select, xslVer)
	{
		Lang = lang;
		DataType = dataType;
		Order = order;
		CaseOrder = caseOrder;
	}
}
