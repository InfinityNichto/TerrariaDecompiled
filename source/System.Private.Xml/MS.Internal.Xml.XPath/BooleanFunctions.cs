using System;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class BooleanFunctions : ValueQuery
{
	private readonly Query _arg;

	private readonly Function.FunctionType _funcType;

	public override XPathResultType StaticType => XPathResultType.Boolean;

	public BooleanFunctions(Function.FunctionType funcType, Query arg)
	{
		_arg = arg;
		_funcType = funcType;
	}

	private BooleanFunctions(BooleanFunctions other)
		: base(other)
	{
		_arg = Query.Clone(other._arg);
		_funcType = other._funcType;
	}

	public override void SetXsltContext(XsltContext context)
	{
		if (_arg != null)
		{
			_arg.SetXsltContext(context);
		}
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		return _funcType switch
		{
			Function.FunctionType.FuncBoolean => toBoolean(nodeIterator), 
			Function.FunctionType.FuncNot => Not(nodeIterator), 
			Function.FunctionType.FuncTrue => true, 
			Function.FunctionType.FuncFalse => false, 
			Function.FunctionType.FuncLang => Lang(nodeIterator), 
			_ => false, 
		};
	}

	internal static bool toBoolean(double number)
	{
		if (number != 0.0)
		{
			return !double.IsNaN(number);
		}
		return false;
	}

	internal static bool toBoolean(string str)
	{
		return str.Length > 0;
	}

	internal bool toBoolean(XPathNodeIterator nodeIterator)
	{
		object obj = _arg.Evaluate(nodeIterator);
		if (obj is XPathNodeIterator)
		{
			return _arg.Advance() != null;
		}
		if (obj is string str)
		{
			return toBoolean(str);
		}
		if (obj is double)
		{
			return toBoolean((double)obj);
		}
		if (obj is bool)
		{
			return (bool)obj;
		}
		return true;
	}

	private bool Not(XPathNodeIterator nodeIterator)
	{
		return !(bool)_arg.Evaluate(nodeIterator);
	}

	private bool Lang(XPathNodeIterator nodeIterator)
	{
		string text = _arg.Evaluate(nodeIterator).ToString();
		string xmlLang = nodeIterator.Current.XmlLang;
		if (xmlLang.StartsWith(text, StringComparison.OrdinalIgnoreCase))
		{
			if (xmlLang.Length != text.Length)
			{
				return xmlLang[text.Length] == '-';
			}
			return true;
		}
		return false;
	}

	public override XPathNodeIterator Clone()
	{
		return new BooleanFunctions(this);
	}
}
