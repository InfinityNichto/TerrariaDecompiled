using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class LogicalExpr : ValueQuery
{
	private delegate bool cmpXslt(Operator.Op op, object val1, object val2);

	private struct NodeSet
	{
		private readonly Query _opnd;

		private XPathNavigator _current;

		public string Value => _current.Value;

		public NodeSet(object opnd)
		{
			_opnd = (Query)opnd;
			_current = null;
		}

		public bool MoveNext()
		{
			_current = _opnd.Advance();
			return _current != null;
		}

		public void Reset()
		{
			_opnd.Reset();
		}
	}

	private readonly Operator.Op _op;

	private readonly Query _opnd1;

	private readonly Query _opnd2;

	private static readonly cmpXslt[][] s_CompXsltE = new cmpXslt[5][]
	{
		new cmpXslt[5] { cmpNumberNumber, null, null, null, null },
		new cmpXslt[5] { cmpStringNumber, cmpStringStringE, null, null, null },
		new cmpXslt[5] { cmpBoolNumberE, cmpBoolStringE, cmpBoolBoolE, null, null },
		new cmpXslt[5] { cmpQueryNumber, cmpQueryStringE, cmpQueryBoolE, cmpQueryQueryE, null },
		new cmpXslt[5] { cmpRtfNumber, cmpRtfStringE, cmpRtfBoolE, cmpRtfQueryE, cmpRtfRtfE }
	};

	private static readonly cmpXslt[][] s_CompXsltO = new cmpXslt[5][]
	{
		new cmpXslt[5] { cmpNumberNumber, null, null, null, null },
		new cmpXslt[5] { cmpStringNumber, cmpStringStringO, null, null, null },
		new cmpXslt[5] { cmpBoolNumberO, cmpBoolStringO, cmpBoolBoolO, null, null },
		new cmpXslt[5] { cmpQueryNumber, cmpQueryStringO, cmpQueryBoolO, cmpQueryQueryO, null },
		new cmpXslt[5] { cmpRtfNumber, cmpRtfStringO, cmpRtfBoolO, cmpRtfQueryO, cmpRtfRtfO }
	};

	public override XPathResultType StaticType => XPathResultType.Boolean;

	public LogicalExpr(Operator.Op op, Query opnd1, Query opnd2)
	{
		_op = op;
		_opnd1 = opnd1;
		_opnd2 = opnd2;
	}

	private LogicalExpr(LogicalExpr other)
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
		Operator.Op op = _op;
		object obj = _opnd1.Evaluate(nodeIterator);
		object obj2 = _opnd2.Evaluate(nodeIterator);
		int num = (int)GetXPathType(obj);
		int num2 = (int)GetXPathType(obj2);
		if (num < num2)
		{
			op = Operator.InvertOperator(op);
			object obj3 = obj;
			obj = obj2;
			obj2 = obj3;
			int num3 = num;
			num = num2;
			num2 = num3;
		}
		cmpXslt cmpXslt = ((op != Operator.Op.EQ && op != Operator.Op.NE) ? s_CompXsltO[num][num2] : s_CompXsltE[num][num2]);
		return cmpXslt(op, obj, obj2);
	}

	private static bool cmpQueryQueryE(Operator.Op op, object val1, object val2)
	{
		bool flag = op == Operator.Op.EQ;
		NodeSet nodeSet = new NodeSet(val1);
		NodeSet nodeSet2 = new NodeSet(val2);
		while (true)
		{
			if (!nodeSet.MoveNext())
			{
				return false;
			}
			if (!nodeSet2.MoveNext())
			{
				break;
			}
			string value = nodeSet.Value;
			do
			{
				if (value == nodeSet2.Value == flag)
				{
					return true;
				}
			}
			while (nodeSet2.MoveNext());
			nodeSet2.Reset();
		}
		return false;
	}

	private static bool cmpQueryQueryO(Operator.Op op, object val1, object val2)
	{
		NodeSet nodeSet = new NodeSet(val1);
		NodeSet nodeSet2 = new NodeSet(val2);
		while (true)
		{
			if (!nodeSet.MoveNext())
			{
				return false;
			}
			if (!nodeSet2.MoveNext())
			{
				break;
			}
			double n = NumberFunctions.Number(nodeSet.Value);
			do
			{
				if (cmpNumberNumber(op, n, NumberFunctions.Number(nodeSet2.Value)))
				{
					return true;
				}
			}
			while (nodeSet2.MoveNext());
			nodeSet2.Reset();
		}
		return false;
	}

	private static bool cmpQueryNumber(Operator.Op op, object val1, object val2)
	{
		NodeSet nodeSet = new NodeSet(val1);
		double n = (double)val2;
		while (nodeSet.MoveNext())
		{
			if (cmpNumberNumber(op, NumberFunctions.Number(nodeSet.Value), n))
			{
				return true;
			}
		}
		return false;
	}

	private static bool cmpQueryStringE(Operator.Op op, object val1, object val2)
	{
		NodeSet nodeSet = new NodeSet(val1);
		string n = (string)val2;
		while (nodeSet.MoveNext())
		{
			if (cmpStringStringE(op, nodeSet.Value, n))
			{
				return true;
			}
		}
		return false;
	}

	private static bool cmpQueryStringO(Operator.Op op, object val1, object val2)
	{
		NodeSet nodeSet = new NodeSet(val1);
		double n = NumberFunctions.Number((string)val2);
		while (nodeSet.MoveNext())
		{
			if (cmpNumberNumberO(op, NumberFunctions.Number(nodeSet.Value), n))
			{
				return true;
			}
		}
		return false;
	}

	private static bool cmpRtfQueryE(Operator.Op op, object val1, object val2)
	{
		string n = Rtf(val1);
		NodeSet nodeSet = new NodeSet(val2);
		while (nodeSet.MoveNext())
		{
			if (cmpStringStringE(op, n, nodeSet.Value))
			{
				return true;
			}
		}
		return false;
	}

	private static bool cmpRtfQueryO(Operator.Op op, object val1, object val2)
	{
		double n = NumberFunctions.Number(Rtf(val1));
		NodeSet nodeSet = new NodeSet(val2);
		while (nodeSet.MoveNext())
		{
			if (cmpNumberNumberO(op, n, NumberFunctions.Number(nodeSet.Value)))
			{
				return true;
			}
		}
		return false;
	}

	private static bool cmpQueryBoolE(Operator.Op op, object val1, object val2)
	{
		bool n = new NodeSet(val1).MoveNext();
		bool n2 = (bool)val2;
		return cmpBoolBoolE(op, n, n2);
	}

	private static bool cmpQueryBoolO(Operator.Op op, object val1, object val2)
	{
		double n = (new NodeSet(val1).MoveNext() ? 1.0 : 0.0);
		double n2 = NumberFunctions.Number((bool)val2);
		return cmpNumberNumberO(op, n, n2);
	}

	private static bool cmpBoolBoolE(Operator.Op op, bool n1, bool n2)
	{
		return op == Operator.Op.EQ == (n1 == n2);
	}

	private static bool cmpBoolBoolE(Operator.Op op, object val1, object val2)
	{
		bool n = (bool)val1;
		bool n2 = (bool)val2;
		return cmpBoolBoolE(op, n, n2);
	}

	private static bool cmpBoolBoolO(Operator.Op op, object val1, object val2)
	{
		double n = NumberFunctions.Number((bool)val1);
		double n2 = NumberFunctions.Number((bool)val2);
		return cmpNumberNumberO(op, n, n2);
	}

	private static bool cmpBoolNumberE(Operator.Op op, object val1, object val2)
	{
		bool n = (bool)val1;
		bool n2 = BooleanFunctions.toBoolean((double)val2);
		return cmpBoolBoolE(op, n, n2);
	}

	private static bool cmpBoolNumberO(Operator.Op op, object val1, object val2)
	{
		double n = NumberFunctions.Number((bool)val1);
		double n2 = (double)val2;
		return cmpNumberNumberO(op, n, n2);
	}

	private static bool cmpBoolStringE(Operator.Op op, object val1, object val2)
	{
		bool n = (bool)val1;
		bool n2 = BooleanFunctions.toBoolean((string)val2);
		return cmpBoolBoolE(op, n, n2);
	}

	private static bool cmpRtfBoolE(Operator.Op op, object val1, object val2)
	{
		bool n = BooleanFunctions.toBoolean(Rtf(val1));
		bool n2 = (bool)val2;
		return cmpBoolBoolE(op, n, n2);
	}

	private static bool cmpBoolStringO(Operator.Op op, object val1, object val2)
	{
		return cmpNumberNumberO(op, NumberFunctions.Number((bool)val1), NumberFunctions.Number((string)val2));
	}

	private static bool cmpRtfBoolO(Operator.Op op, object val1, object val2)
	{
		return cmpNumberNumberO(op, NumberFunctions.Number(Rtf(val1)), NumberFunctions.Number((bool)val2));
	}

	private static bool cmpNumberNumber(Operator.Op op, double n1, double n2)
	{
		return op switch
		{
			Operator.Op.LT => n1 < n2, 
			Operator.Op.GT => n1 > n2, 
			Operator.Op.LE => n1 <= n2, 
			Operator.Op.GE => n1 >= n2, 
			Operator.Op.EQ => n1 == n2, 
			Operator.Op.NE => n1 != n2, 
			_ => false, 
		};
	}

	private static bool cmpNumberNumberO(Operator.Op op, double n1, double n2)
	{
		return op switch
		{
			Operator.Op.LT => n1 < n2, 
			Operator.Op.GT => n1 > n2, 
			Operator.Op.LE => n1 <= n2, 
			Operator.Op.GE => n1 >= n2, 
			_ => false, 
		};
	}

	private static bool cmpNumberNumber(Operator.Op op, object val1, object val2)
	{
		double n = (double)val1;
		double n2 = (double)val2;
		return cmpNumberNumber(op, n, n2);
	}

	private static bool cmpStringNumber(Operator.Op op, object val1, object val2)
	{
		double n = (double)val2;
		double n2 = NumberFunctions.Number((string)val1);
		return cmpNumberNumber(op, n2, n);
	}

	private static bool cmpRtfNumber(Operator.Op op, object val1, object val2)
	{
		double n = (double)val2;
		double n2 = NumberFunctions.Number(Rtf(val1));
		return cmpNumberNumber(op, n2, n);
	}

	private static bool cmpStringStringE(Operator.Op op, string n1, string n2)
	{
		return op == Operator.Op.EQ == (n1 == n2);
	}

	private static bool cmpStringStringE(Operator.Op op, object val1, object val2)
	{
		string n = (string)val1;
		string n2 = (string)val2;
		return cmpStringStringE(op, n, n2);
	}

	private static bool cmpRtfStringE(Operator.Op op, object val1, object val2)
	{
		string n = Rtf(val1);
		string n2 = (string)val2;
		return cmpStringStringE(op, n, n2);
	}

	private static bool cmpRtfRtfE(Operator.Op op, object val1, object val2)
	{
		string n = Rtf(val1);
		string n2 = Rtf(val2);
		return cmpStringStringE(op, n, n2);
	}

	private static bool cmpStringStringO(Operator.Op op, object val1, object val2)
	{
		double n = NumberFunctions.Number((string)val1);
		double n2 = NumberFunctions.Number((string)val2);
		return cmpNumberNumberO(op, n, n2);
	}

	private static bool cmpRtfStringO(Operator.Op op, object val1, object val2)
	{
		double n = NumberFunctions.Number(Rtf(val1));
		double n2 = NumberFunctions.Number((string)val2);
		return cmpNumberNumberO(op, n, n2);
	}

	private static bool cmpRtfRtfO(Operator.Op op, object val1, object val2)
	{
		double n = NumberFunctions.Number(Rtf(val1));
		double n2 = NumberFunctions.Number(Rtf(val2));
		return cmpNumberNumberO(op, n, n2);
	}

	public override XPathNodeIterator Clone()
	{
		return new LogicalExpr(this);
	}

	private static string Rtf(object o)
	{
		return ((XPathNavigator)o).Value;
	}
}
