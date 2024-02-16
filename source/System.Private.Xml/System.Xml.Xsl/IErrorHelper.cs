namespace System.Xml.Xsl;

internal interface IErrorHelper
{
	void ReportError(string res, params string[] args);
}
