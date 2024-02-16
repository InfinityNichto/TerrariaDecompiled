using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class Root : AstNode
{
	public override AstType Type => AstType.Root;

	public override XPathResultType ReturnType => XPathResultType.NodeSet;
}
