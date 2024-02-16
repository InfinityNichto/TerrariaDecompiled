using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class BooleanExpr : ValueQuery
{
	private readonly Query _opnd1;

	private readonly Query _opnd2;

	private readonly bool _isOr;

	public override XPathResultType StaticType => XPathResultType.Boolean;

	public BooleanExpr(Operator.Op op, Query opnd1, Query opnd2)
	{
		if (opnd1.StaticType != XPathResultType.Boolean)
		{
			opnd1 = new BooleanFunctions(Function.FunctionType.FuncBoolean, opnd1);
		}
		if (opnd2.StaticType != XPathResultType.Boolean)
		{
			opnd2 = new BooleanFunctions(Function.FunctionType.FuncBoolean, opnd2);
		}
		_opnd1 = opnd1;
		_opnd2 = opnd2;
		_isOr = op == Operator.Op.OR;
	}

	private BooleanExpr(BooleanExpr other)
		: base(other)
	{
		_opnd1 = Query.Clone(other._opnd1);
		_opnd2 = Query.Clone(other._opnd2);
		_isOr = other._isOr;
	}

	public override void SetXsltContext(XsltContext context)
	{
		_opnd1.SetXsltContext(context);
		_opnd2.SetXsltContext(context);
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		object obj = _opnd1.Evaluate(nodeIterator);
		if ((bool)obj == _isOr)
		{
			return obj;
		}
		return _opnd2.Evaluate(nodeIterator);
	}

	public override XPathNodeIterator Clone()
	{
		return new BooleanExpr(this);
	}
}
