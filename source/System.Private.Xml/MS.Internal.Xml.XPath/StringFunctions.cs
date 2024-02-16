using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class StringFunctions : ValueQuery
{
	private readonly Function.FunctionType _funcType;

	private readonly IList<Query> _argList;

	private static readonly CompareInfo s_compareInfo = CultureInfo.InvariantCulture.CompareInfo;

	public override XPathResultType StaticType
	{
		get
		{
			if (_funcType == Function.FunctionType.FuncStringLength)
			{
				return XPathResultType.Number;
			}
			if (_funcType == Function.FunctionType.FuncStartsWith || _funcType == Function.FunctionType.FuncContains)
			{
				return XPathResultType.Boolean;
			}
			return XPathResultType.String;
		}
	}

	public StringFunctions(Function.FunctionType funcType, IList<Query> argList)
	{
		_funcType = funcType;
		_argList = argList;
	}

	private StringFunctions(StringFunctions other)
		: base(other)
	{
		_funcType = other._funcType;
		Query[] array = new Query[other._argList.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Query.Clone(other._argList[i]);
		}
		_argList = array;
	}

	public override void SetXsltContext(XsltContext context)
	{
		for (int i = 0; i < _argList.Count; i++)
		{
			_argList[i].SetXsltContext(context);
		}
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		return _funcType switch
		{
			Function.FunctionType.FuncString => toString(nodeIterator), 
			Function.FunctionType.FuncConcat => Concat(nodeIterator), 
			Function.FunctionType.FuncStartsWith => StartsWith(nodeIterator), 
			Function.FunctionType.FuncContains => Contains(nodeIterator), 
			Function.FunctionType.FuncSubstringBefore => SubstringBefore(nodeIterator), 
			Function.FunctionType.FuncSubstringAfter => SubstringAfter(nodeIterator), 
			Function.FunctionType.FuncSubstring => Substring(nodeIterator), 
			Function.FunctionType.FuncStringLength => StringLength(nodeIterator), 
			Function.FunctionType.FuncNormalize => Normalize(nodeIterator), 
			Function.FunctionType.FuncTranslate => Translate(nodeIterator), 
			_ => string.Empty, 
		};
	}

	internal static string toString(double num)
	{
		return num.ToString("R", NumberFormatInfo.InvariantInfo);
	}

	internal static string toString(bool b)
	{
		if (!b)
		{
			return "false";
		}
		return "true";
	}

	private string toString(XPathNodeIterator nodeIterator)
	{
		if (_argList.Count > 0)
		{
			object obj = _argList[0].Evaluate(nodeIterator);
			switch (GetXPathType(obj))
			{
			case XPathResultType.NodeSet:
			{
				XPathNavigator xPathNavigator = _argList[0].Advance();
				if (xPathNavigator == null)
				{
					return string.Empty;
				}
				return xPathNavigator.Value;
			}
			case XPathResultType.String:
				return (string)obj;
			case XPathResultType.Boolean:
				if (!(bool)obj)
				{
					return "false";
				}
				return "true";
			case (XPathResultType)4:
				return ((XPathNavigator)obj).Value;
			default:
				return toString((double)obj);
			}
		}
		return nodeIterator.Current.Value;
	}

	private string Concat(XPathNodeIterator nodeIterator)
	{
		int num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		while (num < _argList.Count)
		{
			stringBuilder.Append(_argList[num++].Evaluate(nodeIterator).ToString());
		}
		return stringBuilder.ToString();
	}

	private bool StartsWith(XPathNodeIterator nodeIterator)
	{
		string text = _argList[0].Evaluate(nodeIterator).ToString();
		string text2 = _argList[1].Evaluate(nodeIterator).ToString();
		if (text.Length >= text2.Length)
		{
			return string.CompareOrdinal(text, 0, text2, 0, text2.Length) == 0;
		}
		return false;
	}

	private bool Contains(XPathNodeIterator nodeIterator)
	{
		string source = _argList[0].Evaluate(nodeIterator).ToString();
		string value = _argList[1].Evaluate(nodeIterator).ToString();
		return s_compareInfo.IndexOf(source, value, CompareOptions.Ordinal) >= 0;
	}

	private string SubstringBefore(XPathNodeIterator nodeIterator)
	{
		string text = _argList[0].Evaluate(nodeIterator).ToString();
		string text2 = _argList[1].Evaluate(nodeIterator).ToString();
		if (text2.Length == 0)
		{
			return text2;
		}
		int num = s_compareInfo.IndexOf(text, text2, CompareOptions.Ordinal);
		if (num >= 1)
		{
			return text.Substring(0, num);
		}
		return string.Empty;
	}

	private string SubstringAfter(XPathNodeIterator nodeIterator)
	{
		string text = _argList[0].Evaluate(nodeIterator).ToString();
		string text2 = _argList[1].Evaluate(nodeIterator).ToString();
		if (text2.Length == 0)
		{
			return text;
		}
		int num = s_compareInfo.IndexOf(text, text2, CompareOptions.Ordinal);
		if (num >= 0)
		{
			return text.Substring(num + text2.Length);
		}
		return string.Empty;
	}

	private string Substring(XPathNodeIterator nodeIterator)
	{
		string text = _argList[0].Evaluate(nodeIterator).ToString();
		double num = XmlConvert.XPathRound(XmlConvert.ToXPathDouble(_argList[1].Evaluate(nodeIterator))) - 1.0;
		if (double.IsNaN(num) || (double)text.Length <= num)
		{
			return string.Empty;
		}
		if (_argList.Count == 3)
		{
			double num2 = XmlConvert.XPathRound(XmlConvert.ToXPathDouble(_argList[2].Evaluate(nodeIterator)));
			if (double.IsNaN(num2))
			{
				return string.Empty;
			}
			if (num < 0.0 || num2 < 0.0)
			{
				num2 = num + num2;
				if (!(num2 > 0.0))
				{
					return string.Empty;
				}
				num = 0.0;
			}
			double num3 = (double)text.Length - num;
			if (num2 > num3)
			{
				num2 = num3;
			}
			return text.Substring((int)num, (int)num2);
		}
		if (num < 0.0)
		{
			num = 0.0;
		}
		return text.Substring((int)num);
	}

	private double StringLength(XPathNodeIterator nodeIterator)
	{
		if (_argList.Count > 0)
		{
			return _argList[0].Evaluate(nodeIterator).ToString().Length;
		}
		return nodeIterator.Current.Value.Length;
	}

	private string Normalize(XPathNodeIterator nodeIterator)
	{
		string text = ((_argList.Count <= 0) ? nodeIterator.Current.Value : _argList[0].Evaluate(nodeIterator).ToString());
		int num = -1;
		char[] array = text.ToCharArray();
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			if (!XmlCharType.IsWhiteSpace(array[i]))
			{
				flag = true;
				num++;
				array[num] = array[i];
			}
			else if (flag)
			{
				flag = false;
				num++;
				array[num] = ' ';
			}
		}
		if (num > -1 && array[num] == ' ')
		{
			num--;
		}
		return new string(array, 0, num + 1);
	}

	private string Translate(XPathNodeIterator nodeIterator)
	{
		string text = _argList[0].Evaluate(nodeIterator).ToString();
		string text2 = _argList[1].Evaluate(nodeIterator).ToString();
		string text3 = _argList[2].Evaluate(nodeIterator).ToString();
		int num = -1;
		char[] array = text.ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			int num2 = text2.IndexOf(array[i]);
			if (num2 != -1)
			{
				if (num2 < text3.Length)
				{
					num++;
					array[num] = text3[num2];
				}
			}
			else
			{
				num++;
				array[num] = array[i];
			}
		}
		return new string(array, 0, num + 1);
	}

	public override XPathNodeIterator Clone()
	{
		return new StringFunctions(this);
	}
}
