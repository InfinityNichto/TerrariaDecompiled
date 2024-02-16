using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class Variable : AstNode
{
	private readonly string _localname;

	private readonly string _prefix;

	public override AstType Type => AstType.Variable;

	public override XPathResultType ReturnType => XPathResultType.Any;

	public string Localname => _localname;

	public string Prefix => _prefix;

	public Variable(string name, string prefix)
	{
		_localname = name;
		_prefix = prefix;
	}
}
