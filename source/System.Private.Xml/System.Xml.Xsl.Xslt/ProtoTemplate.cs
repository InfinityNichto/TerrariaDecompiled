using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal abstract class ProtoTemplate : XslNode
{
	public QilFunction Function;

	public ProtoTemplate(XslNodeType nt, QilName name, XslVersion xslVer)
		: base(nt, name, null, xslVer)
	{
	}

	public abstract string GetDebugName();
}
