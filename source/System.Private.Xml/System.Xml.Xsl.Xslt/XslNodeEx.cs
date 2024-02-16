using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class XslNodeEx : XslNode
{
	public readonly ISourceLineInfo ElemNameLi;

	public readonly ISourceLineInfo EndTagLi;

	public XslNodeEx(XslNodeType t, QilName name, object arg, XsltInput.ContextInfo ctxInfo, XslVersion xslVer)
		: base(t, name, arg, xslVer)
	{
		ElemNameLi = ctxInfo.elemNameLi;
		EndTagLi = ctxInfo.endTagLi;
	}

	public XslNodeEx(XslNodeType t, QilName name, object arg, XslVersion xslVer)
		: base(t, name, arg, xslVer)
	{
	}
}
