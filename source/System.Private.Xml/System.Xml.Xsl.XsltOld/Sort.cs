using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class Sort
{
	internal int select;

	internal string lang;

	internal XmlDataType dataType;

	internal XmlSortOrder order;

	internal XmlCaseOrder caseOrder;

	public Sort(int sortkey, string xmllang, XmlDataType datatype, XmlSortOrder xmlorder, XmlCaseOrder xmlcaseorder)
	{
		select = sortkey;
		lang = xmllang;
		dataType = datatype;
		order = xmlorder;
		caseOrder = xmlcaseorder;
	}
}
