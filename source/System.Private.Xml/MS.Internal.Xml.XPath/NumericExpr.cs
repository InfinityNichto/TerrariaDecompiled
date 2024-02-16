using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class NumericExpr : ValueQuery
{
	private readonly Operator.Op _op;

	private readonly Query _opnd1;

	private readonly Query _opnd2;

	public override XPathResultType StaticType => XPathResultType.Number;

	public NumericExpr(Operator.Op op, Query opnd1, Query opnd2)
	{
		if (opnd1.StaticType != 0)
		{
			opnd1 = new NumberFunctions(Function.FunctionType.FuncNumber, opnd1);
		}
		if (opnd2.StaticType != 0)
		{
			opnd2 = new NumberFunctions(Function.FunctionType.FuncNumber, opnd2);
		}
		_op = op;
		_opnd1 = opnd1;
		_opnd2 = opnd2;
	}

	private NumericExpr(NumericExpr other)
		: base(other)
	{
		_op = other._op;
		_opnd1 = Query.Clone(other._opnd1);
		_opnd2 = Query.Clone(other._opnd2);
	}

	public override void SetXsltContext(XsltContext context)
	{
		_opnd1.SetXsltContext(context);
		_opnd2.SetXsltContext(context);
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		return GetValue(_op, XmlConvert.ToXPathDouble(_opnd1.Evaluate(nodeIterator)), XmlConvert.ToXPathDouble(_opnd2.Evaluate(nodeIterator)));
	}

	private static double GetValue(Operator.Op op, double n1, double n2)
	{
		return op switch
		{
			Operator.Op.PLUS => n1 + n2, 
			Operator.Op.MINUS => n1 - n2, 
			Operator.Op.MOD => n1 % n2, 
			Operator.Op.DIV => n1 / n2, 
			Operator.Op.MUL => n1 * n2, 
			_ => 0.0, 
		};
	}

	public override XPathNodeIterator Clone()
	{
		return new NumericExpr(this);
	}
}
