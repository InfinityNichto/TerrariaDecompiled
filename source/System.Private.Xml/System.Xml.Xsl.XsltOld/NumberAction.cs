using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Xml.XPath;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.XsltOld;

internal class NumberAction : ContainerAction
{
	internal sealed class FormatInfo
	{
		public bool isSeparator;

		public NumberingSequence numSequence;

		public int length;

		public string formatString;

		public FormatInfo(bool isSeparator, string formatString)
		{
			this.isSeparator = isSeparator;
			this.formatString = formatString;
		}

		public FormatInfo()
		{
		}
	}

	private sealed class NumberingFormat : NumberFormatterBase
	{
		private NumberingSequence _seq;

		private int _cMinLen;

		private string _separator;

		private int _sizeGroup;

		internal NumberingFormat()
		{
		}

		internal void setNumberingType(NumberingSequence seq)
		{
			_seq = seq;
		}

		internal void setMinLen(int cMinLen)
		{
			_cMinLen = cMinLen;
		}

		internal void setGroupingSeparator(string separator)
		{
			_separator = separator;
		}

		internal void setGroupingSize(int sizeGroup)
		{
			if (0 <= sizeGroup && sizeGroup <= 9)
			{
				_sizeGroup = sizeGroup;
			}
		}

		internal string FormatItem(object value)
		{
			double num;
			if (value is int)
			{
				num = (int)value;
			}
			else
			{
				num = XmlConvert.ToXPathDouble(value);
				if (!(0.5 <= num) || double.IsPositiveInfinity(num))
				{
					return XmlConvert.ToXPathString(value);
				}
				num = XmlConvert.XPathRound(num);
			}
			switch (_seq)
			{
			case NumberingSequence.FirstAlpha:
			case NumberingSequence.LCLetter:
				if (num <= 2147483647.0)
				{
					StringBuilder stringBuilder2 = new StringBuilder();
					NumberFormatterBase.ConvertToAlphabetic(stringBuilder2, num, (_seq == NumberingSequence.FirstAlpha) ? 'A' : 'a', 26);
					return stringBuilder2.ToString();
				}
				break;
			case NumberingSequence.FirstSpecial:
			case NumberingSequence.LCRoman:
				if (num <= 32767.0)
				{
					StringBuilder stringBuilder = new StringBuilder();
					NumberFormatterBase.ConvertToRoman(stringBuilder, num, _seq == NumberingSequence.FirstSpecial);
					return stringBuilder.ToString();
				}
				break;
			}
			return ConvertToArabic(num, _cMinLen, _sizeGroup, _separator);
		}

		private static string ConvertToArabic(double val, int minLength, int groupSize, string groupSeparator)
		{
			string text;
			if (groupSize != 0 && groupSeparator != null)
			{
				NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
				numberFormatInfo.NumberGroupSizes = new int[1] { groupSize };
				numberFormatInfo.NumberGroupSeparator = groupSeparator;
				if (Math.Floor(val) == val)
				{
					numberFormatInfo.NumberDecimalDigits = 0;
				}
				text = val.ToString("N", numberFormatInfo);
			}
			else
			{
				text = val.ToString(CultureInfo.InvariantCulture);
			}
			return text.PadLeft(minLength, '0');
		}
	}

	private static readonly FormatInfo s_defaultFormat = new FormatInfo(isSeparator: false, "0");

	private static readonly FormatInfo s_defaultSeparator = new FormatInfo(isSeparator: true, ".");

	private string _level;

	private string _countPattern;

	private int _countKey = -1;

	private string _from;

	private int _fromKey = -1;

	private string _value;

	private int _valueKey = -1;

	private Avt _formatAvt;

	private Avt _langAvt;

	private Avt _letterAvt;

	private Avt _groupingSepAvt;

	private Avt _groupingSizeAvt;

	private List<FormatInfo> _formatTokens;

	private string _lang;

	private string _letter;

	private string _groupingSep;

	private string _groupingSize;

	private bool _forwardCompatibility;

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Level))
		{
			if (value != "any" && value != "multiple" && value != "single")
			{
				throw XsltException.Create(System.SR.Xslt_InvalidAttrValue, "level", value);
			}
			_level = value;
		}
		else if (Ref.Equal(localName, compiler.Atoms.Count))
		{
			_countPattern = value;
			_countKey = compiler.AddQuery(value, allowVar: true, allowKey: true, isPattern: true);
		}
		else if (Ref.Equal(localName, compiler.Atoms.From))
		{
			_from = value;
			_fromKey = compiler.AddQuery(value, allowVar: true, allowKey: true, isPattern: true);
		}
		else if (Ref.Equal(localName, compiler.Atoms.Value))
		{
			_value = value;
			_valueKey = compiler.AddQuery(value);
		}
		else if (Ref.Equal(localName, compiler.Atoms.Format))
		{
			_formatAvt = Avt.CompileAvt(compiler, value);
		}
		else if (Ref.Equal(localName, compiler.Atoms.Lang))
		{
			_langAvt = Avt.CompileAvt(compiler, value);
		}
		else if (Ref.Equal(localName, compiler.Atoms.LetterValue))
		{
			_letterAvt = Avt.CompileAvt(compiler, value);
		}
		else if (Ref.Equal(localName, compiler.Atoms.GroupingSeparator))
		{
			_groupingSepAvt = Avt.CompileAvt(compiler, value);
		}
		else
		{
			if (!Ref.Equal(localName, compiler.Atoms.GroupingSize))
			{
				return false;
			}
			_groupingSizeAvt = Avt.CompileAvt(compiler, value);
		}
		return true;
	}

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		CheckEmpty(compiler);
		_forwardCompatibility = compiler.ForwardCompatibility;
		_formatTokens = ParseFormat(CompiledAction.PrecalculateAvt(ref _formatAvt));
		_letter = ParseLetter(CompiledAction.PrecalculateAvt(ref _letterAvt));
		_lang = CompiledAction.PrecalculateAvt(ref _langAvt);
		_groupingSep = CompiledAction.PrecalculateAvt(ref _groupingSepAvt);
		if (_groupingSep != null && _groupingSep.Length > 1)
		{
			throw XsltException.Create(System.SR.Xslt_CharAttribute, "grouping-separator");
		}
		_groupingSize = CompiledAction.PrecalculateAvt(ref _groupingSizeAvt);
	}

	private int numberAny(Processor processor, ActionFrame frame)
	{
		int num = 0;
		XPathNavigator xPathNavigator = frame.Node;
		if (xPathNavigator.NodeType == XPathNodeType.Attribute || xPathNavigator.NodeType == XPathNodeType.Namespace)
		{
			xPathNavigator = xPathNavigator.Clone();
			xPathNavigator.MoveToParent();
		}
		XPathNavigator xPathNavigator2 = xPathNavigator.Clone();
		if (_fromKey != -1)
		{
			bool flag = false;
			do
			{
				if (processor.Matches(xPathNavigator2, _fromKey))
				{
					flag = true;
					break;
				}
			}
			while (xPathNavigator2.MoveToParent());
			XPathNodeIterator xPathNodeIterator = xPathNavigator2.SelectDescendants(XPathNodeType.All, matchSelf: true);
			while (xPathNodeIterator.MoveNext())
			{
				if (processor.Matches(xPathNodeIterator.Current, _fromKey))
				{
					flag = true;
					num = 0;
				}
				else if (MatchCountKey(processor, frame.Node, xPathNodeIterator.Current))
				{
					num++;
				}
				if (xPathNodeIterator.Current.IsSamePosition(xPathNavigator))
				{
					break;
				}
			}
			if (!flag)
			{
				num = 0;
			}
		}
		else
		{
			xPathNavigator2.MoveToRoot();
			XPathNodeIterator xPathNodeIterator2 = xPathNavigator2.SelectDescendants(XPathNodeType.All, matchSelf: true);
			while (xPathNodeIterator2.MoveNext())
			{
				if (MatchCountKey(processor, frame.Node, xPathNodeIterator2.Current))
				{
					num++;
				}
				if (xPathNodeIterator2.Current.IsSamePosition(xPathNavigator))
				{
					break;
				}
			}
		}
		return num;
	}

	private bool checkFrom(Processor processor, XPathNavigator nav)
	{
		if (_fromKey == -1)
		{
			return true;
		}
		do
		{
			if (processor.Matches(nav, _fromKey))
			{
				return true;
			}
		}
		while (nav.MoveToParent());
		return false;
	}

	private bool moveToCount(XPathNavigator nav, Processor processor, XPathNavigator contextNode)
	{
		do
		{
			if (_fromKey != -1 && processor.Matches(nav, _fromKey))
			{
				return false;
			}
			if (MatchCountKey(processor, contextNode, nav))
			{
				return true;
			}
		}
		while (nav.MoveToParent());
		return false;
	}

	private int numberCount(XPathNavigator nav, Processor processor, XPathNavigator contextNode)
	{
		XPathNavigator xPathNavigator = nav.Clone();
		int num = 1;
		if (xPathNavigator.MoveToParent())
		{
			xPathNavigator.MoveToFirstChild();
			while (!xPathNavigator.IsSamePosition(nav))
			{
				if (MatchCountKey(processor, contextNode, xPathNavigator))
				{
					num++;
				}
				if (!xPathNavigator.MoveToNext())
				{
					break;
				}
			}
		}
		return num;
	}

	private static object SimplifyValue(object value)
	{
		if (Type.GetTypeCode(value.GetType()) == TypeCode.Object)
		{
			if (value is XPathNodeIterator xPathNodeIterator)
			{
				if (xPathNodeIterator.MoveNext())
				{
					return xPathNodeIterator.Current.Value;
				}
				return string.Empty;
			}
			if (value is XPathNavigator xPathNavigator)
			{
				return xPathNavigator.Value;
			}
		}
		return value;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		ArrayList numberList = processor.NumberList;
		switch (frame.State)
		{
		default:
			return;
		case 0:
			numberList.Clear();
			if (_valueKey != -1)
			{
				numberList.Add(SimplifyValue(processor.Evaluate(frame, _valueKey)));
			}
			else if (_level == "any")
			{
				int num = numberAny(processor, frame);
				if (num != 0)
				{
					numberList.Add(num);
				}
			}
			else
			{
				bool flag = _level == "multiple";
				XPathNavigator node = frame.Node;
				XPathNavigator xPathNavigator = frame.Node.Clone();
				if (xPathNavigator.NodeType == XPathNodeType.Attribute || xPathNavigator.NodeType == XPathNodeType.Namespace)
				{
					xPathNavigator.MoveToParent();
				}
				while (moveToCount(xPathNavigator, processor, node))
				{
					numberList.Insert(0, numberCount(xPathNavigator, processor, node));
					if (!flag || !xPathNavigator.MoveToParent())
					{
						break;
					}
				}
				if (!checkFrom(processor, xPathNavigator))
				{
					numberList.Clear();
				}
			}
			frame.StoredOutput = Format(numberList, (_formatAvt == null) ? _formatTokens : ParseFormat(_formatAvt.Evaluate(processor, frame)), (_langAvt == null) ? _lang : _langAvt.Evaluate(processor, frame), (_letterAvt == null) ? _letter : ParseLetter(_letterAvt.Evaluate(processor, frame)), (_groupingSepAvt == null) ? _groupingSep : _groupingSepAvt.Evaluate(processor, frame), (_groupingSizeAvt == null) ? _groupingSize : _groupingSizeAvt.Evaluate(processor, frame));
			break;
		case 2:
			break;
		}
		if (!processor.TextEvent(frame.StoredOutput))
		{
			frame.State = 2;
		}
		else
		{
			frame.Finished();
		}
	}

	private bool MatchCountKey(Processor processor, XPathNavigator contextNode, XPathNavigator nav)
	{
		if (_countKey != -1)
		{
			return processor.Matches(nav, _countKey);
		}
		if (contextNode.Name == nav.Name && BasicNodeType(contextNode.NodeType) == BasicNodeType(nav.NodeType))
		{
			return true;
		}
		return false;
	}

	private XPathNodeType BasicNodeType(XPathNodeType type)
	{
		if (type == XPathNodeType.SignificantWhitespace || type == XPathNodeType.Whitespace)
		{
			return XPathNodeType.Text;
		}
		return type;
	}

	private static string Format(ArrayList numberlist, List<FormatInfo> tokens, string lang, string letter, string groupingSep, string groupingSize)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		if (tokens != null)
		{
			num = tokens.Count;
		}
		NumberingFormat numberingFormat = new NumberingFormat();
		if (groupingSize != null)
		{
			try
			{
				numberingFormat.setGroupingSize(Convert.ToInt32(groupingSize, CultureInfo.InvariantCulture));
			}
			catch (FormatException)
			{
			}
			catch (OverflowException)
			{
			}
		}
		if (groupingSep != null)
		{
			_ = groupingSep.Length;
			_ = 1;
			numberingFormat.setGroupingSeparator(groupingSep);
		}
		if (0 < num)
		{
			FormatInfo formatInfo = tokens[0];
			FormatInfo formatInfo2 = null;
			if (num % 2 == 1)
			{
				formatInfo2 = tokens[num - 1];
				num--;
			}
			FormatInfo formatInfo3 = ((2 < num) ? tokens[num - 2] : s_defaultSeparator);
			FormatInfo formatInfo4 = ((0 < num) ? tokens[num - 1] : s_defaultFormat);
			if (formatInfo != null)
			{
				stringBuilder.Append(formatInfo.formatString);
			}
			int count = numberlist.Count;
			for (int i = 0; i < count; i++)
			{
				int num2 = i * 2;
				bool flag = num2 < num;
				if (0 < i)
				{
					FormatInfo formatInfo5 = (flag ? tokens[num2] : formatInfo3);
					stringBuilder.Append(formatInfo5.formatString);
				}
				FormatInfo formatInfo6 = (flag ? tokens[num2 + 1] : formatInfo4);
				numberingFormat.setNumberingType(formatInfo6.numSequence);
				numberingFormat.setMinLen(formatInfo6.length);
				stringBuilder.Append(numberingFormat.FormatItem(numberlist[i]));
			}
			if (formatInfo2 != null)
			{
				stringBuilder.Append(formatInfo2.formatString);
			}
		}
		else
		{
			numberingFormat.setNumberingType(NumberingSequence.FirstDecimal);
			for (int j = 0; j < numberlist.Count; j++)
			{
				if (j != 0)
				{
					stringBuilder.Append('.');
				}
				stringBuilder.Append(numberingFormat.FormatItem(numberlist[j]));
			}
		}
		return stringBuilder.ToString();
	}

	private static void mapFormatToken(string wsToken, int startLen, int tokLen, out NumberingSequence seq, out int pminlen)
	{
		char c = wsToken[startLen];
		bool flag = false;
		pminlen = 1;
		seq = NumberingSequence.Nil;
		int num = c;
		if (num <= 2406)
		{
			if (num == 48 || num == 2406)
			{
				goto IL_0042;
			}
		}
		else if (num == 3664 || num == 51067 || num == 65296)
		{
			goto IL_0042;
		}
		goto IL_0071;
		IL_0071:
		if (!flag)
		{
			switch (wsToken[startLen])
			{
			case '1':
				seq = NumberingSequence.FirstDecimal;
				break;
			case 'A':
				seq = NumberingSequence.FirstAlpha;
				break;
			case 'I':
				seq = NumberingSequence.FirstSpecial;
				break;
			case 'a':
				seq = NumberingSequence.LCLetter;
				break;
			case 'i':
				seq = NumberingSequence.LCRoman;
				break;
			case 'А':
				seq = NumberingSequence.UCRus;
				break;
			case 'а':
				seq = NumberingSequence.LCRus;
				break;
			case 'א':
				seq = NumberingSequence.Hebrew;
				break;
			case 'أ':
				seq = NumberingSequence.ArabicScript;
				break;
			case 'अ':
				seq = NumberingSequence.Hindi2;
				break;
			case 'क':
				seq = NumberingSequence.Hindi1;
				break;
			case '१':
				seq = NumberingSequence.Hindi3;
				break;
			case 'ก':
				seq = NumberingSequence.Thai1;
				break;
			case '๑':
				seq = NumberingSequence.Thai2;
				break;
			case 'ア':
				seq = NumberingSequence.DAiueo;
				break;
			case 'イ':
				seq = NumberingSequence.DIroha;
				break;
			case 'ㄱ':
				seq = NumberingSequence.DChosung;
				break;
			case '一':
				seq = NumberingSequence.FEDecimal;
				break;
			case '壱':
				seq = NumberingSequence.DbNum3;
				break;
			case '壹':
				seq = NumberingSequence.ChnCmplx;
				break;
			case '子':
				seq = NumberingSequence.Zodiac2;
				break;
			case '가':
				seq = NumberingSequence.Ganada;
				break;
			case '일':
				seq = NumberingSequence.KorDbNum1;
				break;
			case '하':
				seq = NumberingSequence.KorDbNum3;
				break;
			case '１':
				seq = NumberingSequence.DArabic;
				break;
			case 'ｱ':
				seq = NumberingSequence.Aiueo;
				break;
			case 'ｲ':
				seq = NumberingSequence.Iroha;
				break;
			case '甲':
				if (tokLen > 1 && wsToken[startLen + 1] == '子')
				{
					seq = NumberingSequence.Zodiac3;
					tokLen--;
					startLen++;
				}
				else
				{
					seq = NumberingSequence.Zodiac1;
				}
				break;
			default:
				seq = NumberingSequence.FirstDecimal;
				break;
			}
		}
		if (flag)
		{
			seq = NumberingSequence.FirstDecimal;
			pminlen = 0;
		}
		return;
		IL_0042:
		do
		{
			pminlen++;
		}
		while (--tokLen > 0 && c == wsToken[++startLen]);
		if (wsToken[startLen] != (ushort)(c + 1))
		{
			flag = true;
		}
		goto IL_0071;
	}

	[return: NotNullIfNotNull("formatString")]
	private static List<FormatInfo> ParseFormat(string formatString)
	{
		if (formatString == null || formatString.Length == 0)
		{
			return null;
		}
		int num = 0;
		bool flag = CharUtil.IsAlphaNumeric(formatString[num]);
		List<FormatInfo> list = new List<FormatInfo>();
		int num2 = 0;
		if (flag)
		{
			list.Add(null);
		}
		while (num <= formatString.Length)
		{
			bool flag2 = ((num < formatString.Length) ? CharUtil.IsAlphaNumeric(formatString[num]) : (!flag));
			if (flag != flag2)
			{
				FormatInfo formatInfo = new FormatInfo();
				if (flag)
				{
					mapFormatToken(formatString, num2, num - num2, out formatInfo.numSequence, out formatInfo.length);
				}
				else
				{
					formatInfo.isSeparator = true;
					formatInfo.formatString = formatString.Substring(num2, num - num2);
				}
				num2 = num;
				num++;
				list.Add(formatInfo);
				flag = flag2;
			}
			else
			{
				num++;
			}
		}
		return list;
	}

	private string ParseLetter(string letter)
	{
		switch (letter)
		{
		case null:
		case "traditional":
		case "alphabetic":
			return letter;
		default:
			if (!_forwardCompatibility)
			{
				throw XsltException.Create(System.SR.Xslt_InvalidAttrValue, "letter-value", letter);
			}
			return null;
		}
	}
}
