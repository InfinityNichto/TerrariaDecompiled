using System.Collections.Generic;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.XPath;

internal interface IXPathEnvironment : IFocus
{
	XPathQilFactory Factory { get; }

	QilNode ResolveVariable(string prefix, string name);

	QilNode ResolveFunction(string prefix, string name, IList<QilNode> args, IFocus env);

	string ResolvePrefix(string prefix);
}
