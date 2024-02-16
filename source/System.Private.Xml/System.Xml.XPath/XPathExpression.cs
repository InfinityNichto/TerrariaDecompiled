using System.Collections;
using MS.Internal.Xml.XPath;

namespace System.Xml.XPath;

public abstract class XPathExpression
{
	public abstract string Expression { get; }

	public abstract XPathResultType ReturnType { get; }

	internal XPathExpression()
	{
	}

	public abstract void AddSort(object expr, IComparer comparer);

	public abstract void AddSort(object expr, XmlSortOrder order, XmlCaseOrder caseOrder, string lang, XmlDataType dataType);

	public abstract XPathExpression Clone();

	public abstract void SetContext(XmlNamespaceManager nsManager);

	public abstract void SetContext(IXmlNamespaceResolver? nsResolver);

	public static XPathExpression Compile(string xpath)
	{
		return Compile(xpath, null);
	}

	public static XPathExpression Compile(string xpath, IXmlNamespaceResolver? nsResolver)
	{
		bool needContext;
		Query query = new QueryBuilder().Build(xpath, out needContext);
		CompiledXpathExpr compiledXpathExpr = new CompiledXpathExpr(query, xpath, needContext);
		if (nsResolver != null)
		{
			compiledXpathExpr.SetContext(nsResolver);
		}
		return compiledXpathExpr;
	}
}
