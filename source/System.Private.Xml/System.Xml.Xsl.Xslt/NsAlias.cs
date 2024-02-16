namespace System.Xml.Xsl.Xslt;

internal sealed class NsAlias
{
	public readonly string ResultNsUri;

	public readonly string ResultPrefix;

	public readonly int ImportPrecedence;

	public NsAlias(string resultNsUri, string resultPrefix, int importPrecedence)
	{
		ResultNsUri = resultNsUri;
		ResultPrefix = resultPrefix;
		ImportPrecedence = importPrecedence;
	}
}
