namespace System.Xml.Xsl.Xslt;

internal sealed class NsDecl
{
	public readonly NsDecl Prev;

	public readonly string Prefix;

	public readonly string NsUri;

	public NsDecl(NsDecl prev, string prefix, string nsUri)
	{
		Prev = prev;
		Prefix = prefix;
		NsUri = nsUri;
	}
}
