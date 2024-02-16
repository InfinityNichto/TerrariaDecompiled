namespace System.Xml.Xsl.Xslt;

internal sealed class NodeCtor : XslNode
{
	public readonly string NameAvt;

	public readonly string NsAvt;

	public NodeCtor(XslNodeType nt, string nameAvt, string nsAvt, XslVersion xslVer)
		: base(nt, null, null, xslVer)
	{
		NameAvt = nameAvt;
		NsAvt = nsAvt;
	}
}
