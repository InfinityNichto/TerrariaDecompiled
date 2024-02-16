using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal static class AstFactory
{
	private static readonly QilFactory s_f = new QilFactory();

	public static XslNode XslNode(XslNodeType nodeType, QilName name, string arg, XslVersion xslVer)
	{
		return new XslNode(nodeType, name, arg, xslVer);
	}

	public static XslNode ApplyImports(QilName mode, Stylesheet sheet, XslVersion xslVer)
	{
		return new XslNode(XslNodeType.ApplyImports, mode, sheet, xslVer);
	}

	public static XslNodeEx ApplyTemplates(QilName mode, string select, XsltInput.ContextInfo ctxInfo, XslVersion xslVer)
	{
		return new XslNodeEx(XslNodeType.ApplyTemplates, mode, select, ctxInfo, xslVer);
	}

	public static XslNodeEx ApplyTemplates(QilName mode)
	{
		return new XslNodeEx(XslNodeType.ApplyTemplates, mode, null, XslVersion.Version10);
	}

	public static NodeCtor Attribute(string nameAvt, string nsAvt, XslVersion xslVer)
	{
		return new NodeCtor(XslNodeType.Attribute, nameAvt, nsAvt, xslVer);
	}

	public static AttributeSet AttributeSet(QilName name)
	{
		return new AttributeSet(name, XslVersion.Version10);
	}

	public static XslNodeEx CallTemplate(QilName name, XsltInput.ContextInfo ctxInfo)
	{
		return new XslNodeEx(XslNodeType.CallTemplate, name, null, ctxInfo, XslVersion.Version10);
	}

	public static XslNode Choose()
	{
		return new XslNode(XslNodeType.Choose);
	}

	public static XslNode Comment()
	{
		return new XslNode(XslNodeType.Comment);
	}

	public static XslNode Copy()
	{
		return new XslNode(XslNodeType.Copy);
	}

	public static XslNode CopyOf(string select, XslVersion xslVer)
	{
		return new XslNode(XslNodeType.CopyOf, null, select, xslVer);
	}

	public static NodeCtor Element(string nameAvt, string nsAvt, XslVersion xslVer)
	{
		return new NodeCtor(XslNodeType.Element, nameAvt, nsAvt, xslVer);
	}

	public static XslNode Error(string message)
	{
		return new XslNode(XslNodeType.Error, null, message, XslVersion.Version10);
	}

	public static XslNodeEx ForEach(string select, XsltInput.ContextInfo ctxInfo, XslVersion xslVer)
	{
		return new XslNodeEx(XslNodeType.ForEach, null, select, ctxInfo, xslVer);
	}

	public static XslNode If(string test, XslVersion xslVer)
	{
		return new XslNode(XslNodeType.If, null, test, xslVer);
	}

	public static Key Key(QilName name, string match, string use, XslVersion xslVer)
	{
		return new Key(name, match, use, xslVer);
	}

	public static XslNode List()
	{
		return new XslNode(XslNodeType.List);
	}

	public static XslNode LiteralAttribute(QilName name, string value, XslVersion xslVer)
	{
		return new XslNode(XslNodeType.LiteralAttribute, name, value, xslVer);
	}

	public static XslNode LiteralElement(QilName name)
	{
		return new XslNode(XslNodeType.LiteralElement, name, null, XslVersion.Version10);
	}

	public static XslNode Message(bool term)
	{
		return new XslNode(XslNodeType.Message, null, term, XslVersion.Version10);
	}

	public static XslNode Nop()
	{
		return new XslNode(XslNodeType.Nop);
	}

	public static Number Number(NumberLevel level, string count, string from, string value, string format, string lang, string letterValue, string groupingSeparator, string groupingSize, XslVersion xslVer)
	{
		return new Number(level, count, from, value, format, lang, letterValue, groupingSeparator, groupingSize, xslVer);
	}

	public static XslNode Otherwise()
	{
		return new XslNode(XslNodeType.Otherwise);
	}

	public static XslNode PI(string name, XslVersion xslVer)
	{
		return new XslNode(XslNodeType.PI, null, name, xslVer);
	}

	public static Sort Sort(string select, string lang, string dataType, string order, string caseOrder, XslVersion xslVer)
	{
		return new Sort(select, lang, dataType, order, caseOrder, xslVer);
	}

	public static Template Template(QilName name, string match, QilName mode, double priority, XslVersion xslVer)
	{
		return new Template(name, match, mode, priority, xslVer);
	}

	public static XslNode Text(string data)
	{
		return new Text(data, SerializationHints.None, XslVersion.Version10);
	}

	public static XslNode Text(string data, SerializationHints hints)
	{
		return new Text(data, hints, XslVersion.Version10);
	}

	public static XslNode UseAttributeSet(QilName name)
	{
		return new XslNode(XslNodeType.UseAttributeSet, name, null, XslVersion.Version10);
	}

	public static VarPar VarPar(XslNodeType nt, QilName name, string select, XslVersion xslVer)
	{
		return new VarPar(nt, name, select, xslVer);
	}

	public static VarPar WithParam(QilName name)
	{
		return VarPar(XslNodeType.WithParam, name, null, XslVersion.Version10);
	}

	public static QilName QName(string local, string uri, string prefix)
	{
		return s_f.LiteralQName(local, uri, prefix);
	}

	public static QilName QName(string local)
	{
		return s_f.LiteralQName(local);
	}
}
