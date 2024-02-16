using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class NumberFunctions : ValueQuery
{
	private readonly Query _arg;

	private readonly Function.FunctionType _ftype;

	public override XPathResultType StaticType => XPathResultType.Number;

	public NumberFunctions(Function.FunctionType ftype, Query arg)
	{
		_arg = arg;
		_ftype = ftype;
	}

	private NumberFunctions(NumberFunctions other)
		: base(other)
	{
		_arg = Query.Clone(other._arg);
		_ftype = other._ftype;
	}

	public override void SetXsltContext(XsltContext context)
	{
		if (_arg != null)
		{
			_arg.SetXsltContext(context);
		}
	}

	internal static double Number(bool arg)
	{
		if (!arg)
		{
			return 0.0;
		}
		return 1.0;
	}

	internal static double Number(string arg)
	{
		return XmlConvert.ToXPathDouble(arg);
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		return _ftype switch
		{
			Function.FunctionType.FuncNumber => Number(nodeIterator), 
			Function.FunctionType.FuncSum => Sum(nodeIterator), 
			Function.FunctionType.FuncFloor => Floor(nodeIterator), 
			Function.FunctionType.FuncCeiling => Ceiling(nodeIterator), 
			Function.FunctionType.FuncRound => Round(nodeIterator), 
			_ => throw new InvalidOperationException(), 
		};
	}

	private double Number(XPathNodeIterator nodeIterator)
	{
		if (_arg == null)
		{
			return XmlConvert.ToXPathDouble(nodeIterator.Current.Value);
		}
		object obj = _arg.Evaluate(nodeIterator);
		switch (GetXPathType(obj))
		{
		case XPathResultType.NodeSet:
		{
			XPathNavigator xPathNavigator = _arg.Advance();
			if (xPathNavigator != null)
			{
				return Number(xPathNavigator.Value);
			}
			break;
		}
		case XPathResultType.String:
			return Number((string)obj);
		case XPathResultType.Boolean:
			return Number((bool)obj);
		case XPathResultType.Number:
			return (double)obj;
		case (XPathResultType)4:
			return Number(((XPathNavigator)obj).Value);
		}
		return double.NaN;
	}

	private double Sum(XPathNodeIterator nodeIterator)
	{
		double num = 0.0;
		_arg.Evaluate(nodeIterator);
		XPathNavigator xPathNavigator;
		while ((xPathNavigator = _arg.Advance()) != null)
		{
			num += Number(xPathNavigator.Value);
		}
		return num;
	}

	private double Floor(XPathNodeIterator nodeIterator)
	{
		return Math.Floor((double)_arg.Evaluate(nodeIterator));
	}

	private double Ceiling(XPathNodeIterator nodeIterator)
	{
		return Math.Ceiling((double)_arg.Evaluate(nodeIterator));
	}

	private double Round(XPathNodeIterator nodeIterator)
	{
		double value = XmlConvert.ToXPathDouble(_arg.Evaluate(nodeIterator));
		return XmlConvert.XPathRound(value);
	}

	public override XPathNodeIterator Clone()
	{
		return new NumberFunctions(this);
	}
}
