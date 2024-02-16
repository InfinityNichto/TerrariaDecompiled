using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class Operand : AstNode
{
	private readonly XPathResultType _type;

	private readonly object _val;

	public override AstType Type => AstType.ConstantOperand;

	public override XPathResultType ReturnType => _type;

	public object OperandValue => _val;

	public Operand(string val)
	{
		_type = XPathResultType.String;
		_val = val;
	}

	public Operand(double val)
	{
		_type = XPathResultType.Number;
		_val = val;
	}
}
