using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class XsltFunctions
{
	private static readonly CompareInfo s_compareInfo = CultureInfo.InvariantCulture.CompareInfo;

	public static bool StartsWith(string s1, string s2)
	{
		if (s1.Length >= s2.Length)
		{
			return string.CompareOrdinal(s1, 0, s2, 0, s2.Length) == 0;
		}
		return false;
	}

	public static bool Contains(string s1, string s2)
	{
		return s_compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal) >= 0;
	}

	public static string SubstringBefore(string s1, string s2)
	{
		if (s2.Length == 0)
		{
			return s2;
		}
		int num = s_compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal);
		if (num >= 1)
		{
			return s1.Substring(0, num);
		}
		return string.Empty;
	}

	public static string SubstringAfter(string s1, string s2)
	{
		if (s2.Length == 0)
		{
			return s1;
		}
		int num = s_compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal);
		if (num >= 0)
		{
			return s1.Substring(num + s2.Length);
		}
		return string.Empty;
	}

	public static string Substring(string value, double startIndex)
	{
		startIndex = Round(startIndex);
		if (startIndex <= 0.0)
		{
			return value;
		}
		if (startIndex <= (double)value.Length)
		{
			return value.Substring((int)startIndex - 1);
		}
		return string.Empty;
	}

	public static string Substring(string value, double startIndex, double length)
	{
		startIndex = Round(startIndex) - 1.0;
		if (startIndex >= (double)value.Length)
		{
			return string.Empty;
		}
		double num = startIndex + Round(length);
		startIndex = ((startIndex <= 0.0) ? 0.0 : startIndex);
		if (startIndex < num)
		{
			if (num > (double)value.Length)
			{
				num = value.Length;
			}
			return value.Substring((int)startIndex, (int)(num - startIndex));
		}
		return string.Empty;
	}

	public static string NormalizeSpace(string value)
	{
		StringBuilder stringBuilder = null;
		int num = 0;
		int num2 = 0;
		int i;
		for (i = 0; i < value.Length; i++)
		{
			if (!XmlCharType.IsWhiteSpace(value[i]))
			{
				continue;
			}
			if (i == num)
			{
				num++;
			}
			else if (value[i] != ' ' || num2 == i)
			{
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(value.Length);
				}
				else
				{
					stringBuilder.Append(' ');
				}
				if (num2 == i)
				{
					stringBuilder.Append(value, num, i - num - 1);
				}
				else
				{
					stringBuilder.Append(value, num, i - num);
				}
				num = i + 1;
			}
			else
			{
				num2 = i + 1;
			}
		}
		if (stringBuilder == null)
		{
			if (num == i)
			{
				return string.Empty;
			}
			if (num == 0 && num2 != i)
			{
				return value;
			}
			stringBuilder = new StringBuilder(value.Length);
		}
		else if (i != num)
		{
			stringBuilder.Append(' ');
		}
		if (num2 == i)
		{
			stringBuilder.Append(value, num, i - num - 1);
		}
		else
		{
			stringBuilder.Append(value, num, i - num);
		}
		return stringBuilder.ToString();
	}

	public static string Translate(string arg, string mapString, string transString)
	{
		if (mapString.Length == 0)
		{
			return arg;
		}
		StringBuilder stringBuilder = new StringBuilder(arg.Length);
		for (int i = 0; i < arg.Length; i++)
		{
			int num = mapString.IndexOf(arg[i]);
			if (num < 0)
			{
				stringBuilder.Append(arg[i]);
			}
			else if (num < transString.Length)
			{
				stringBuilder.Append(transString[num]);
			}
		}
		return stringBuilder.ToString();
	}

	public static bool Lang(string value, XPathNavigator context)
	{
		string xmlLang = context.XmlLang;
		if (!xmlLang.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (xmlLang.Length != value.Length)
		{
			return xmlLang[value.Length] == '-';
		}
		return true;
	}

	public static double Round(double value)
	{
		double num = Math.Round(value);
		if (value - num != 0.5)
		{
			return num;
		}
		return num + 1.0;
	}

	public static XPathItem SystemProperty(XmlQualifiedName name)
	{
		if (name.Namespace == "http://www.w3.org/1999/XSL/Transform")
		{
			switch (name.Name)
			{
			case "version":
				return new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Double), 1.0);
			case "vendor":
				return new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), "Microsoft");
			case "vendor-url":
				return new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), "http://www.microsoft.com");
			}
		}
		else if (name.Namespace == "urn:schemas-microsoft-com:xslt" && name.Name == "version")
		{
			return new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), typeof(XsltLibrary).Assembly.ImageRuntimeVersion);
		}
		return new XmlAtomicValue(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String), string.Empty);
	}

	public static string BaseUri(XPathNavigator navigator)
	{
		return navigator.BaseURI;
	}

	public static string OuterXml(XPathNavigator navigator)
	{
		if (!(navigator is RtfNavigator rtfNavigator))
		{
			return navigator.OuterXml;
		}
		StringBuilder stringBuilder = new StringBuilder();
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		xmlWriterSettings.OmitXmlDeclaration = true;
		xmlWriterSettings.ConformanceLevel = ConformanceLevel.Fragment;
		xmlWriterSettings.CheckCharacters = false;
		XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, xmlWriterSettings);
		rtfNavigator.CopyToWriter(xmlWriter);
		xmlWriter.Close();
		return stringBuilder.ToString();
	}

	public static string EXslObjectType(IList<XPathItem> value)
	{
		if (value.Count != 1)
		{
			return "node-set";
		}
		XPathItem xPathItem = value[0];
		if (xPathItem is RtfNavigator)
		{
			return "RTF";
		}
		if (xPathItem.IsNode)
		{
			return "node-set";
		}
		object typedValue = xPathItem.TypedValue;
		if (typedValue is string)
		{
			return "string";
		}
		if (typedValue is double)
		{
			return "number";
		}
		if (typedValue is bool)
		{
			return "boolean";
		}
		return "external";
	}

	public static double MSNumber(IList<XPathItem> value)
	{
		if (value.Count == 0)
		{
			return double.NaN;
		}
		XPathItem xPathItem = value[0];
		string value2;
		if (xPathItem.IsNode)
		{
			value2 = xPathItem.Value;
		}
		else
		{
			Type valueType = xPathItem.ValueType;
			if (!(valueType == XsltConvert.StringType))
			{
				if (valueType == XsltConvert.DoubleType)
				{
					return xPathItem.ValueAsDouble;
				}
				if (!xPathItem.ValueAsBoolean)
				{
					return 0.0;
				}
				return 1.0;
			}
			value2 = xPathItem.Value;
		}
		if (XmlConvert.TryToDouble(value2, out var result) != null)
		{
			return double.NaN;
		}
		return result;
	}

	public static string MSFormatDateTime(string dateTime, string format, string lang, bool isDate)
	{
		try
		{
			string name = GetCultureInfo(lang).Name;
			if (!XsdDateTime.TryParse(dateTime, XsdDateTimeFlags.AllXsd | XsdDateTimeFlags.XdrDateTime | XsdDateTimeFlags.XdrTimeNoTz, out var result))
			{
				return string.Empty;
			}
			DateTime dateTime2 = result.ToZulu();
			if (format.Length == 0)
			{
				format = null;
			}
			return dateTime2.ToString(format, new CultureInfo(name));
		}
		catch (ArgumentException)
		{
			return string.Empty;
		}
	}

	public static double MSStringCompare(string s1, string s2, string lang, string options)
	{
		CultureInfo cultureInfo = GetCultureInfo(lang);
		CompareOptions compareOptions = CompareOptions.None;
		bool flag = false;
		for (int i = 0; i < options.Length; i++)
		{
			switch (options[i])
			{
			case 'i':
				compareOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;
				break;
			case 'u':
				flag = true;
				break;
			default:
				flag = true;
				compareOptions = CompareOptions.IgnoreCase;
				break;
			}
		}
		if (flag)
		{
			if (compareOptions != 0)
			{
				throw new XslTransformException(System.SR.Xslt_InvalidCompareOption, options);
			}
			compareOptions = CompareOptions.IgnoreCase;
		}
		int num = cultureInfo.CompareInfo.Compare(s1, s2, compareOptions);
		if (flag && num == 0)
		{
			num = -cultureInfo.CompareInfo.Compare(s1, s2, CompareOptions.None);
		}
		return num;
	}

	public static string MSUtc(string dateTime)
	{
		XsdDateTime result;
		DateTime dt;
		try
		{
			if (!XsdDateTime.TryParse(dateTime, XsdDateTimeFlags.AllXsd | XsdDateTimeFlags.XdrDateTime | XsdDateTimeFlags.XdrTimeNoTz, out result))
			{
				return string.Empty;
			}
			dt = result.ToZulu();
		}
		catch (ArgumentException)
		{
			return string.Empty;
		}
		char[] array = "----------T00:00:00.000".ToCharArray();
		switch (result.TypeCode)
		{
		case XmlTypeCode.DateTime:
			PrintDate(array, dt);
			PrintTime(array, dt);
			break;
		case XmlTypeCode.Time:
			PrintTime(array, dt);
			break;
		case XmlTypeCode.Date:
			PrintDate(array, dt);
			break;
		case XmlTypeCode.GYearMonth:
			PrintYear(array, dt.Year);
			ShortToCharArray(array, 5, dt.Month);
			break;
		case XmlTypeCode.GYear:
			PrintYear(array, dt.Year);
			break;
		case XmlTypeCode.GMonthDay:
			ShortToCharArray(array, 5, dt.Month);
			ShortToCharArray(array, 8, dt.Day);
			break;
		case XmlTypeCode.GDay:
			ShortToCharArray(array, 8, dt.Day);
			break;
		case XmlTypeCode.GMonth:
			ShortToCharArray(array, 5, dt.Month);
			break;
		}
		return new string(array);
	}

	public static string MSLocalName(string name)
	{
		int colonOffset;
		int num = ValidateNames.ParseQName(name, 0, out colonOffset);
		if (num != name.Length)
		{
			return string.Empty;
		}
		if (colonOffset == 0)
		{
			return name;
		}
		return name.Substring(colonOffset + 1);
	}

	public static string MSNamespaceUri(string name, XPathNavigator currentNode)
	{
		int colonOffset;
		int num = ValidateNames.ParseQName(name, 0, out colonOffset);
		if (num != name.Length)
		{
			return string.Empty;
		}
		string text = name.Substring(0, colonOffset);
		if (text == "xmlns")
		{
			return string.Empty;
		}
		string text2 = currentNode.LookupNamespace(text);
		if (text2 != null)
		{
			return text2;
		}
		if (text == "xml")
		{
			return "http://www.w3.org/XML/1998/namespace";
		}
		return string.Empty;
	}

	private static CultureInfo GetCultureInfo(string lang)
	{
		if (lang.Length == 0)
		{
			return CultureInfo.CurrentCulture;
		}
		try
		{
			return new CultureInfo(lang);
		}
		catch (ArgumentException)
		{
			throw new XslTransformException(System.SR.Xslt_InvalidLanguage, lang);
		}
	}

	private static void PrintDate(char[] text, DateTime dt)
	{
		PrintYear(text, dt.Year);
		ShortToCharArray(text, 5, dt.Month);
		ShortToCharArray(text, 8, dt.Day);
	}

	private static void PrintTime(char[] text, DateTime dt)
	{
		ShortToCharArray(text, 11, dt.Hour);
		ShortToCharArray(text, 14, dt.Minute);
		ShortToCharArray(text, 17, dt.Second);
		PrintMsec(text, dt.Millisecond);
	}

	private static void PrintYear(char[] text, int value)
	{
		text[0] = (char)(value / 1000 % 10 + 48);
		text[1] = (char)(value / 100 % 10 + 48);
		text[2] = (char)(value / 10 % 10 + 48);
		text[3] = (char)(value / 1 % 10 + 48);
	}

	private static void PrintMsec(char[] text, int value)
	{
		if (value != 0)
		{
			text[20] = (char)(value / 100 % 10 + 48);
			text[21] = (char)(value / 10 % 10 + 48);
			text[22] = (char)(value / 1 % 10 + 48);
		}
	}

	private static void ShortToCharArray(char[] text, int start, int value)
	{
		text[start] = (char)(value / 10 + 48);
		text[start + 1] = (char)(value % 10 + 48);
	}
}
