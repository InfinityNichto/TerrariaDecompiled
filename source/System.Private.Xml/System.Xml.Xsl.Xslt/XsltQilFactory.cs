using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt;

internal sealed class XsltQilFactory : XPathQilFactory
{
	public XsltQilFactory(QilFactory f, bool debug)
		: base(f, debug)
	{
	}

	public QilNode DefaultValueMarker()
	{
		return QName("default-value", "urn:schemas-microsoft-com:xslt-debug");
	}

	public QilNode InvokeIsSameNodeSort(QilNode n1, QilNode n2)
	{
		return XsltInvokeEarlyBound(QName("is-same-node-sort"), XsltMethods.IsSameNodeSort, XmlQueryTypeFactory.BooleanX, new QilNode[2] { n1, n2 });
	}

	public QilNode InvokeSystemProperty(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("system-property"), XsltMethods.SystemProperty, XmlQueryTypeFactory.Choice(XmlQueryTypeFactory.DoubleX, XmlQueryTypeFactory.StringX), new QilNode[1] { n });
	}

	public QilNode InvokeElementAvailable(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("element-available"), XsltMethods.ElementAvailable, XmlQueryTypeFactory.BooleanX, new QilNode[1] { n });
	}

	public QilNode InvokeCheckScriptNamespace(string nsUri)
	{
		return XsltInvokeEarlyBound(QName("register-script-namespace"), XsltMethods.CheckScriptNamespace, XmlQueryTypeFactory.IntX, new QilNode[1] { String(nsUri) });
	}

	public QilNode InvokeFunctionAvailable(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("function-available"), XsltMethods.FunctionAvailable, XmlQueryTypeFactory.BooleanX, new QilNode[1] { n });
	}

	public QilNode InvokeBaseUri(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("base-uri"), XsltMethods.BaseUri, XmlQueryTypeFactory.StringX, new QilNode[1] { n });
	}

	public QilNode InvokeOnCurrentNodeChanged(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("on-current-node-changed"), XsltMethods.OnCurrentNodeChanged, XmlQueryTypeFactory.IntX, new QilNode[1] { n });
	}

	public QilNode InvokeLangToLcid(QilNode n, bool fwdCompat)
	{
		return XsltInvokeEarlyBound(QName("lang-to-lcid"), XsltMethods.LangToLcid, XmlQueryTypeFactory.IntX, new QilNode[2]
		{
			n,
			Boolean(fwdCompat)
		});
	}

	public QilNode InvokeNumberFormat(QilNode value, QilNode format, QilNode lang, QilNode letterValue, QilNode groupingSeparator, QilNode groupingSize)
	{
		return XsltInvokeEarlyBound(QName("number-format"), XsltMethods.NumberFormat, XmlQueryTypeFactory.StringX, new QilNode[6] { value, format, lang, letterValue, groupingSeparator, groupingSize });
	}

	public QilNode InvokeRegisterDecimalFormat(DecimalFormatDecl format)
	{
		return XsltInvokeEarlyBound(QName("register-decimal-format"), XsltMethods.RegisterDecimalFormat, XmlQueryTypeFactory.IntX, new QilNode[4]
		{
			QName(format.Name.Name, format.Name.Namespace),
			String(format.InfinitySymbol),
			String(format.NanSymbol),
			String(new string(format.Characters))
		});
	}

	public QilNode InvokeRegisterDecimalFormatter(QilNode formatPicture, DecimalFormatDecl format)
	{
		return XsltInvokeEarlyBound(QName("register-decimal-formatter"), XsltMethods.RegisterDecimalFormatter, XmlQueryTypeFactory.DoubleX, new QilNode[4]
		{
			formatPicture,
			String(format.InfinitySymbol),
			String(format.NanSymbol),
			String(new string(format.Characters))
		});
	}

	public QilNode InvokeFormatNumberStatic(QilNode value, QilNode decimalFormatIndex)
	{
		return XsltInvokeEarlyBound(QName("format-number-static"), XsltMethods.FormatNumberStatic, XmlQueryTypeFactory.StringX, new QilNode[2] { value, decimalFormatIndex });
	}

	public QilNode InvokeFormatNumberDynamic(QilNode value, QilNode formatPicture, QilNode decimalFormatName, QilNode errorMessageName)
	{
		return XsltInvokeEarlyBound(QName("format-number-dynamic"), XsltMethods.FormatNumberDynamic, XmlQueryTypeFactory.StringX, new QilNode[4] { value, formatPicture, decimalFormatName, errorMessageName });
	}

	public QilNode InvokeOuterXml(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("outer-xml"), XsltMethods.OuterXml, XmlQueryTypeFactory.StringX, new QilNode[1] { n });
	}

	public QilNode InvokeMsFormatDateTime(QilNode datetime, QilNode format, QilNode lang, QilNode isDate)
	{
		return XsltInvokeEarlyBound(QName("ms:format-date-time"), XsltMethods.MSFormatDateTime, XmlQueryTypeFactory.StringX, new QilNode[4] { datetime, format, lang, isDate });
	}

	public QilNode InvokeMsStringCompare(QilNode x, QilNode y, QilNode lang, QilNode options)
	{
		return XsltInvokeEarlyBound(QName("ms:string-compare"), XsltMethods.MSStringCompare, XmlQueryTypeFactory.DoubleX, new QilNode[4] { x, y, lang, options });
	}

	public QilNode InvokeMsUtc(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("ms:utc"), XsltMethods.MSUtc, XmlQueryTypeFactory.StringX, new QilNode[1] { n });
	}

	public QilNode InvokeMsNumber(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("ms:number"), XsltMethods.MSNumber, XmlQueryTypeFactory.DoubleX, new QilNode[1] { n });
	}

	public QilNode InvokeMsLocalName(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("ms:local-name"), XsltMethods.MSLocalName, XmlQueryTypeFactory.StringX, new QilNode[1] { n });
	}

	public QilNode InvokeMsNamespaceUri(QilNode n, QilNode currentNode)
	{
		return XsltInvokeEarlyBound(QName("ms:namespace-uri"), XsltMethods.MSNamespaceUri, XmlQueryTypeFactory.StringX, new QilNode[2] { n, currentNode });
	}

	public QilNode InvokeEXslObjectType(QilNode n)
	{
		return XsltInvokeEarlyBound(QName("exsl:object-type"), XsltMethods.EXslObjectType, XmlQueryTypeFactory.StringX, new QilNode[1] { n });
	}
}
