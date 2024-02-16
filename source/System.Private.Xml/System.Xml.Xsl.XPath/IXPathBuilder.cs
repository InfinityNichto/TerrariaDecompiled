using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.XPath;

namespace System.Xml.Xsl.XPath;

internal interface IXPathBuilder<Node>
{
	void StartBuild();

	[return: NotNullIfNotNull("result")]
	Node EndBuild(Node result);

	Node String(string value);

	Node Number(double value);

	Node Operator(XPathOperator op, Node left, Node right);

	Node Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string prefix, string name);

	Node JoinStep(Node left, Node right);

	Node Predicate(Node node, Node condition, bool reverseStep);

	Node Variable(string prefix, string name);

	Node Function(string prefix, string name, IList<Node> args);
}
