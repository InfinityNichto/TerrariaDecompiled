using System.Xml.XPath;

namespace System.Xml.Xsl;

public interface IXsltContextFunction
{
	int Minargs { get; }

	int Maxargs { get; }

	XPathResultType ReturnType { get; }

	XPathResultType[] ArgTypes { get; }

	object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext);
}
