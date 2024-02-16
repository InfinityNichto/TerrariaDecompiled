namespace System.Xml.Xsl.XsltOld;

internal sealed class NamespaceInfo
{
	internal string prefix;

	internal string nameSpace;

	internal int stylesheetId;

	internal NamespaceInfo(string prefix, string nameSpace, int stylesheetId)
	{
		this.prefix = prefix;
		this.nameSpace = nameSpace;
		this.stylesheetId = stylesheetId;
	}
}
