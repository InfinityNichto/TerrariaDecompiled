using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization;

internal static class FormatProvider
{
	private static class Number
	{
		internal struct NumberBuffer
		{
			public int precision;

			public int scale;

			public bool sign;

			public unsafe char* overrideDigits;

			public unsafe char* digits => overrideDigits;
		}

		private static readonly string[] s_posCurrencyFormats = new string[4] { "$#", "#$", "$ #", "# $" };

		private static readonly string[] s_negCurrencyFormats = new string[17]
		{
			"($#)", "-$#", "$-#", "$#-", "(#$)", "-#$", "#-$", "#$-", "-# $", "-$ #",
			"# $-", "$ #-", "$ -#", "#- $", "($ #)", "(# $)", "$- #"
		};

		private static readonly string[] s_posPercentFormats = new string[4] { "# %", "#%", "%#", "% #" };

		private static readonly string[] s_negPercentFormats = new string[12]
		{
			"-# %", "-#%", "-%#", "%-#", "%#-", "#-%", "#%-", "-% #", "# %-", "% #-",
			"% -#", "#- %"
		};

		private static readonly string[] s_negNumberFormats = new string[5] { "(#)", "-#", "- #", "#-", "# -" };

		private static bool IsWhite(char ch)
		{
			if (ch != ' ')
			{
				if (ch >= '\t')
				{
					return ch <= '\r';
				}
				return false;
			}
			return true;
		}

		private unsafe static char* MatchChars(char* p, char* pEnd, string str)
		{
			fixed (char* str2 = str)
			{
				return MatchChars(p, pEnd, str2);
			}
		}

		private unsafe static char* MatchChars(char* p, char* pEnd, char* str)
		{
			if (*str == '\0')
			{
				return null;
			}
			while (true)
			{
				char c = ((p < pEnd) ? (*p) : '\0');
				if (c != *str && (*str != '\u00a0' || c != ' '))
				{
					break;
				}
				p++;
				str++;
				if (*str == '\0')
				{
					return p;
				}
			}
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AllowHyphenDuringParsing(NumberFormatInfo info)
		{
			string negativeSign = info.NegativeSign;
			bool flag = negativeSign.Length == 1;
			bool flag2 = flag;
			if (flag2)
			{
				bool flag3;
				switch (negativeSign[0])
				{
				case '‒':
				case '⁻':
				case '₋':
				case '−':
				case '➖':
				case '﹣':
				case '－':
					flag3 = true;
					break;
				default:
					flag3 = false;
					break;
				}
				flag2 = flag3;
			}
			return flag2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe static char* MatchNegativeSignChars(char* p, char* pEnd, string negativeSign, bool allowHyphenDuringParsing)
		{
			char* ptr = MatchChars(p, pEnd, negativeSign);
			if (ptr == null && allowHyphenDuringParsing && p < pEnd && *p == '-')
			{
				ptr = p + 1;
			}
			return ptr;
		}

		private unsafe static bool ParseNumber(ref char* str, char* strEnd, NumberStyles options, ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, bool parseDecimal)
		{
			number.scale = 0;
			number.sign = false;
			string text = null;
			bool allowHyphenDuringParsing = AllowHyphenDuringParsing(numfmt);
			bool flag = false;
			string str2;
			string str3;
			if ((options & NumberStyles.AllowCurrencySymbol) != 0)
			{
				text = numfmt.CurrencySymbol;
				str2 = numfmt.CurrencyDecimalSeparator;
				str3 = numfmt.CurrencyGroupSeparator;
				flag = true;
			}
			else
			{
				str2 = numfmt.NumberDecimalSeparator;
				str3 = numfmt.NumberGroupSeparator;
			}
			int num = 0;
			bool flag2 = sb != null;
			int num2 = (flag2 ? int.MaxValue : 32);
			char* ptr = str;
			char c = ((ptr < strEnd) ? (*ptr) : '\0');
			char* digits = number.digits;
			while (true)
			{
				if (!IsWhite(c) || (options & NumberStyles.AllowLeadingWhite) == 0 || (((uint)num & (true ? 1u : 0u)) != 0 && (num & 0x20) == 0 && numfmt.NumberNegativePattern != 2))
				{
					char* ptr2;
					if ((options & NumberStyles.AllowLeadingSign) != 0 && (num & 1) == 0 && ((ptr2 = MatchChars(ptr, strEnd, numfmt.PositiveSign)) != null || ((ptr2 = MatchNegativeSignChars(ptr, strEnd, numfmt.NegativeSign, allowHyphenDuringParsing)) != null && (number.sign = true))))
					{
						num |= 1;
						ptr = ptr2 - 1;
					}
					else if (c == '(' && (options & NumberStyles.AllowParentheses) != 0 && (num & 1) == 0)
					{
						num |= 3;
						number.sign = true;
					}
					else
					{
						if (text == null || (ptr2 = MatchChars(ptr, strEnd, text)) == null)
						{
							break;
						}
						num |= 0x20;
						text = null;
						ptr = ptr2 - 1;
					}
				}
				c = ((++ptr < strEnd) ? (*ptr) : '\0');
			}
			int num3 = 0;
			int num4 = 0;
			while (true)
			{
				char* ptr2;
				if ((c >= '0' && c <= '9') || ((options & NumberStyles.AllowHexSpecifier) != 0 && ((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))))
				{
					num |= 4;
					if (c != '0' || ((uint)num & 8u) != 0 || (flag2 && (options & NumberStyles.AllowHexSpecifier) != 0))
					{
						if (num3 < num2)
						{
							if (flag2)
							{
								sb.Append(c);
							}
							else
							{
								digits[num3++] = c;
							}
							if (c != '0' || parseDecimal)
							{
								num4 = num3;
							}
						}
						if ((num & 0x10) == 0)
						{
							number.scale++;
						}
						num |= 8;
					}
					else if (((uint)num & 0x10u) != 0)
					{
						number.scale--;
					}
				}
				else if ((options & NumberStyles.AllowDecimalPoint) != 0 && (num & 0x10) == 0 && ((ptr2 = MatchChars(ptr, strEnd, str2)) != null || (flag && (num & 0x20) == 0 && (ptr2 = MatchChars(ptr, strEnd, numfmt.NumberDecimalSeparator)) != null)))
				{
					num |= 0x10;
					ptr = ptr2 - 1;
				}
				else
				{
					if ((options & NumberStyles.AllowThousands) == 0 || (num & 4) == 0 || ((uint)num & 0x10u) != 0 || ((ptr2 = MatchChars(ptr, strEnd, str3)) == null && (!flag || ((uint)num & 0x20u) != 0 || (ptr2 = MatchChars(ptr, strEnd, numfmt.NumberGroupSeparator)) == null)))
					{
						break;
					}
					ptr = ptr2 - 1;
				}
				c = ((++ptr < strEnd) ? (*ptr) : '\0');
			}
			bool flag3 = false;
			number.precision = num4;
			if (flag2)
			{
				sb.Append('\0');
			}
			else
			{
				digits[num4] = '\0';
			}
			if (((uint)num & 4u) != 0)
			{
				if ((c == 'E' || c == 'e') && (options & NumberStyles.AllowExponent) != 0)
				{
					char* ptr3 = ptr;
					c = ((++ptr < strEnd) ? (*ptr) : '\0');
					char* ptr2;
					if ((ptr2 = MatchChars(ptr, strEnd, numfmt.PositiveSign)) != null)
					{
						c = (((ptr = ptr2) < strEnd) ? (*ptr) : '\0');
					}
					else if ((ptr2 = MatchNegativeSignChars(ptr, strEnd, numfmt.NegativeSign, allowHyphenDuringParsing)) != null)
					{
						c = (((ptr = ptr2) < strEnd) ? (*ptr) : '\0');
						flag3 = true;
					}
					if (c >= '0' && c <= '9')
					{
						int num5 = 0;
						do
						{
							num5 = num5 * 10 + (c - 48);
							c = ((++ptr < strEnd) ? (*ptr) : '\0');
							if (num5 > 1000)
							{
								num5 = 9999;
								while (c >= '0' && c <= '9')
								{
									c = ((++ptr < strEnd) ? (*ptr) : '\0');
								}
							}
						}
						while (c >= '0' && c <= '9');
						if (flag3)
						{
							num5 = -num5;
						}
						number.scale += num5;
					}
					else
					{
						ptr = ptr3;
						c = ((ptr < strEnd) ? (*ptr) : '\0');
					}
				}
				while (true)
				{
					if (!IsWhite(c) || (options & NumberStyles.AllowTrailingWhite) == 0)
					{
						char* ptr2;
						if ((options & NumberStyles.AllowTrailingSign) != 0 && (num & 1) == 0 && ((ptr2 = MatchChars(ptr, strEnd, numfmt.PositiveSign)) != null || ((ptr2 = MatchNegativeSignChars(ptr, strEnd, numfmt.NegativeSign, allowHyphenDuringParsing)) != null && (number.sign = true))))
						{
							num |= 1;
							ptr = ptr2 - 1;
						}
						else if (c == ')' && ((uint)num & 2u) != 0)
						{
							num &= -3;
						}
						else
						{
							if (text == null || (ptr2 = MatchChars(ptr, strEnd, text)) == null)
							{
								break;
							}
							text = null;
							ptr = ptr2 - 1;
						}
					}
					c = ((++ptr < strEnd) ? (*ptr) : '\0');
				}
				if ((num & 2) == 0)
				{
					if ((num & 8) == 0)
					{
						if (!parseDecimal)
						{
							number.scale = 0;
						}
						if ((num & 0x10) == 0)
						{
							number.sign = false;
						}
					}
					str = ptr;
					return true;
				}
			}
			str = ptr;
			return false;
		}

		private static bool TrailingZeros(ReadOnlySpan<char> s, int index)
		{
			for (int i = index; i < s.Length; i++)
			{
				if (s[i] != 0)
				{
					return false;
				}
			}
			return true;
		}

		internal unsafe static bool TryStringToNumber(ReadOnlySpan<char> str, NumberStyles options, ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, bool parseDecimal)
		{
			fixed (char* ptr = &MemoryMarshal.GetReference(str))
			{
				char* str2 = ptr;
				if (!ParseNumber(ref str2, str2 + str.Length, options, ref number, sb, numfmt, parseDecimal) || (str2 - ptr < str.Length && !TrailingZeros(str, (int)(str2 - ptr))))
				{
					return false;
				}
			}
			return true;
		}

		internal unsafe static void Int32ToDecChars(char* buffer, ref int index, uint value, int digits)
		{
			while (--digits >= 0 || value != 0)
			{
				buffer[--index] = (char)(value % 10 + 48);
				value /= 10;
			}
		}

		internal static char ParseFormatSpecifier(ReadOnlySpan<char> format, out int digits)
		{
			char c = '\0';
			if (format.Length > 0)
			{
				c = format[0];
				if ((uint)(c - 65) <= 25u || (uint)(c - 97) <= 25u)
				{
					if (format.Length == 1)
					{
						digits = -1;
						return c;
					}
					if (format.Length == 2)
					{
						int num = format[1] - 48;
						if ((uint)num < 10u)
						{
							digits = num;
							return c;
						}
					}
					else if (format.Length == 3)
					{
						int num2 = format[1] - 48;
						int num3 = format[2] - 48;
						if ((uint)num2 < 10u && (uint)num3 < 10u)
						{
							digits = num2 * 10 + num3;
							return c;
						}
					}
					int num4 = 0;
					int num5 = 1;
					while (num5 < format.Length && (uint)(format[num5] - 48) < 10u)
					{
						int num6 = num4 * 10 + format[num5++] - 48;
						if (num6 < num4)
						{
							throw new FormatException(System.SR.Argument_BadFormatSpecifier);
						}
						num4 = num6;
					}
					if (num5 == format.Length || format[num5] == '\0')
					{
						digits = num4;
						return c;
					}
				}
			}
			digits = -1;
			if (format.Length != 0 && c != 0)
			{
				return '\0';
			}
			return 'G';
		}

		internal unsafe static void NumberToString(ref System.Text.ValueStringBuilder sb, ref NumberBuffer number, char format, int nMaxDigits, NumberFormatInfo info, bool isDecimal)
		{
			int num = -1;
			switch (format)
			{
			case 'C':
			case 'c':
				num = ((nMaxDigits >= 0) ? nMaxDigits : info.CurrencyDecimalDigits);
				if (nMaxDigits < 0)
				{
					nMaxDigits = info.CurrencyDecimalDigits;
				}
				RoundNumber(ref number, number.scale + nMaxDigits);
				FormatCurrency(ref sb, ref number, num, nMaxDigits, info);
				break;
			case 'F':
			case 'f':
				if (nMaxDigits < 0)
				{
					nMaxDigits = (num = info.NumberDecimalDigits);
				}
				else
				{
					num = nMaxDigits;
				}
				RoundNumber(ref number, number.scale + nMaxDigits);
				if (number.sign)
				{
					sb.Append(info.NegativeSign);
				}
				FormatFixed(ref sb, ref number, num, nMaxDigits, info, null, info.NumberDecimalSeparator, null);
				break;
			case 'N':
			case 'n':
				if (nMaxDigits < 0)
				{
					nMaxDigits = (num = info.NumberDecimalDigits);
				}
				else
				{
					num = nMaxDigits;
				}
				RoundNumber(ref number, number.scale + nMaxDigits);
				FormatNumber(ref sb, ref number, num, nMaxDigits, info);
				break;
			case 'E':
			case 'e':
				if (nMaxDigits < 0)
				{
					nMaxDigits = (num = 6);
				}
				else
				{
					num = nMaxDigits;
				}
				nMaxDigits++;
				RoundNumber(ref number, nMaxDigits);
				if (number.sign)
				{
					sb.Append(info.NegativeSign);
				}
				FormatScientific(ref sb, ref number, num, nMaxDigits, info, format);
				break;
			case 'G':
			case 'g':
			{
				bool flag = true;
				if (nMaxDigits < 1)
				{
					if (isDecimal && nMaxDigits == -1)
					{
						nMaxDigits = (num = 29);
						flag = false;
					}
					else
					{
						nMaxDigits = (num = number.precision);
					}
				}
				else
				{
					num = nMaxDigits;
				}
				if (flag)
				{
					RoundNumber(ref number, nMaxDigits);
				}
				else if (isDecimal && *number.digits == '\0')
				{
					number.sign = false;
				}
				if (number.sign)
				{
					sb.Append(info.NegativeSign);
				}
				FormatGeneral(ref sb, ref number, num, nMaxDigits, info, (char)(format - 2), !flag);
				break;
			}
			case 'P':
			case 'p':
				if (nMaxDigits < 0)
				{
					nMaxDigits = (num = info.PercentDecimalDigits);
				}
				else
				{
					num = nMaxDigits;
				}
				number.scale += 2;
				RoundNumber(ref number, number.scale + nMaxDigits);
				FormatPercent(ref sb, ref number, num, nMaxDigits, info);
				break;
			default:
				throw new FormatException(System.SR.Argument_BadFormatSpecifier);
			}
		}

		private static void FormatCurrency(ref System.Text.ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
		{
			string text = (number.sign ? s_negCurrencyFormats[info.CurrencyNegativePattern] : s_posCurrencyFormats[info.CurrencyPositivePattern]);
			string text2 = text;
			foreach (char c in text2)
			{
				switch (c)
				{
				case '#':
					FormatFixed(ref sb, ref number, nMinDigits, nMaxDigits, info, info.CurrencyGroupSizes, info.CurrencyDecimalSeparator, info.CurrencyGroupSeparator);
					break;
				case '-':
					sb.Append(info.NegativeSign);
					break;
				case '$':
					sb.Append(info.CurrencySymbol);
					break;
				default:
					sb.Append(c);
					break;
				}
			}
		}

		private unsafe static void FormatFixed(ref System.Text.ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, int[] groupDigits, string sDecimal, string sGroup)
		{
			int scale = number.scale;
			char* ptr = number.digits;
			int length = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr).Length;
			if (scale > 0)
			{
				if (groupDigits != null)
				{
					int num = 0;
					int num2 = groupDigits[num];
					int num3 = groupDigits.Length;
					int num4 = scale;
					int length2 = sGroup.Length;
					int num5 = 0;
					if (num3 != 0)
					{
						while (scale > num2 && groupDigits[num] != 0)
						{
							num4 += length2;
							if (num < num3 - 1)
							{
								num++;
							}
							num2 += groupDigits[num];
							if (num2 < 0 || num4 < 0)
							{
								throw new ArgumentOutOfRangeException();
							}
						}
						num5 = ((num2 != 0) ? groupDigits[0] : 0);
					}
					char* ptr2 = stackalloc char[num4];
					num = 0;
					int num6 = 0;
					int num7 = ((scale < length) ? scale : length);
					char* ptr3 = ptr2 + num4 - 1;
					for (int num8 = scale - 1; num8 >= 0; num8--)
					{
						*(ptr3--) = ((num8 < num7) ? ptr[num8] : '0');
						if (num5 > 0)
						{
							num6++;
							if (num6 == num5 && num8 != 0)
							{
								for (int num9 = length2 - 1; num9 >= 0; num9--)
								{
									*(ptr3--) = sGroup[num9];
								}
								if (num < num3 - 1)
								{
									num++;
									num5 = groupDigits[num];
								}
								num6 = 0;
							}
						}
					}
					sb.Append(ptr2, num4);
					ptr += num7;
				}
				else
				{
					int num10 = Math.Min(length, scale);
					sb.Append(ptr, num10);
					ptr += num10;
					if (scale > length)
					{
						sb.Append('0', scale - length);
					}
				}
			}
			else
			{
				sb.Append('0');
			}
			if (nMaxDigits > 0)
			{
				sb.Append(sDecimal);
				if (scale < 0 && nMaxDigits > 0)
				{
					int num11 = Math.Min(-scale, nMaxDigits);
					sb.Append('0', num11);
					scale += num11;
					nMaxDigits -= num11;
				}
				while (nMaxDigits > 0)
				{
					sb.Append((*ptr != 0) ? (*(ptr++)) : '0');
					nMaxDigits--;
				}
			}
		}

		private static void FormatNumber(ref System.Text.ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
		{
			string text = (number.sign ? s_negNumberFormats[info.NumberNegativePattern] : "#");
			string text2 = text;
			foreach (char c in text2)
			{
				switch (c)
				{
				case '#':
					FormatFixed(ref sb, ref number, nMinDigits, nMaxDigits, info, info.NumberGroupSizes, info.NumberDecimalSeparator, info.NumberGroupSeparator);
					break;
				case '-':
					sb.Append(info.NegativeSign);
					break;
				default:
					sb.Append(c);
					break;
				}
			}
		}

		private unsafe static void FormatScientific(ref System.Text.ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, char expChar)
		{
			char* digits = number.digits;
			sb.Append((*digits != 0) ? (*(digits++)) : '0');
			if (nMaxDigits != 1)
			{
				sb.Append(info.NumberDecimalSeparator);
			}
			while (--nMaxDigits > 0)
			{
				sb.Append((*digits != 0) ? (*(digits++)) : '0');
			}
			int value = ((*number.digits != 0) ? (number.scale - 1) : 0);
			FormatExponent(ref sb, info, value, expChar, 3, positiveSign: true);
		}

		private unsafe static void FormatExponent(ref System.Text.ValueStringBuilder sb, NumberFormatInfo info, int value, char expChar, int minDigits, bool positiveSign)
		{
			sb.Append(expChar);
			if (value < 0)
			{
				sb.Append(info.NegativeSign);
				value = -value;
			}
			else if (positiveSign)
			{
				sb.Append(info.PositiveSign);
			}
			char* ptr = stackalloc char[11];
			int index = 10;
			Int32ToDecChars(ptr, ref index, (uint)value, minDigits);
			int num = 10 - index;
			while (--num >= 0)
			{
				sb.Append(ptr[index++]);
			}
		}

		private unsafe static void FormatGeneral(ref System.Text.ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, char expChar, bool bSuppressScientific)
		{
			int i = number.scale;
			bool flag = false;
			if (!bSuppressScientific && (i > nMaxDigits || i < -3))
			{
				i = 1;
				flag = true;
			}
			char* digits = number.digits;
			if (i > 0)
			{
				do
				{
					sb.Append((*digits != 0) ? (*(digits++)) : '0');
				}
				while (--i > 0);
			}
			else
			{
				sb.Append('0');
			}
			if (*digits != 0 || i < 0)
			{
				sb.Append(info.NumberDecimalSeparator);
				for (; i < 0; i++)
				{
					sb.Append('0');
				}
				while (*digits != 0)
				{
					sb.Append(*(digits++));
				}
			}
			if (flag)
			{
				FormatExponent(ref sb, info, number.scale - 1, expChar, 2, positiveSign: true);
			}
		}

		private static void FormatPercent(ref System.Text.ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
		{
			string text = (number.sign ? s_negPercentFormats[info.PercentNegativePattern] : s_posPercentFormats[info.PercentPositivePattern]);
			string text2 = text;
			foreach (char c in text2)
			{
				switch (c)
				{
				case '#':
					FormatFixed(ref sb, ref number, nMinDigits, nMaxDigits, info, info.PercentGroupSizes, info.PercentDecimalSeparator, info.PercentGroupSeparator);
					break;
				case '-':
					sb.Append(info.NegativeSign);
					break;
				case '%':
					sb.Append(info.PercentSymbol);
					break;
				default:
					sb.Append(c);
					break;
				}
			}
		}

		private unsafe static void RoundNumber(ref NumberBuffer number, int pos)
		{
			char* digits = number.digits;
			int i;
			for (i = 0; i < pos && digits[i] != 0; i++)
			{
			}
			if (i == pos && digits[i] >= '5')
			{
				while (i > 0 && digits[i - 1] == '9')
				{
					i--;
				}
				if (i > 0)
				{
					char* num = digits + (i - 1);
					*num = (char)(*num + 1);
				}
				else
				{
					number.scale++;
					*digits = '1';
					i = 1;
				}
			}
			else
			{
				while (i > 0 && digits[i - 1] == '0')
				{
					i--;
				}
			}
			if (i == 0)
			{
				number.scale = 0;
				number.sign = false;
			}
			digits[i] = '\0';
		}

		private unsafe static int FindSection(ReadOnlySpan<char> format, int section)
		{
			if (section == 0)
			{
				return 0;
			}
			fixed (char* ptr = &MemoryMarshal.GetReference(format))
			{
				int num = 0;
				while (true)
				{
					if (num >= format.Length)
					{
						return 0;
					}
					char c;
					char c2 = (c = ptr[num++]);
					if ((uint)c2 <= 34u)
					{
						if (c2 == '\0')
						{
							break;
						}
						if (c2 != '"')
						{
							continue;
						}
					}
					else if (c2 != '\'')
					{
						switch (c2)
						{
						default:
							continue;
						case '\\':
							if (num < format.Length && ptr[num] != 0)
							{
								num++;
							}
							continue;
						case ';':
							break;
						}
						if (--section == 0)
						{
							if (num >= format.Length || ptr[num] == '\0' || ptr[num] == ';')
							{
								break;
							}
							return num;
						}
						continue;
					}
					while (num < format.Length && ptr[num] != 0 && ptr[num++] != c)
					{
					}
				}
				return 0;
			}
		}

		internal unsafe static void NumberToStringFormat(ref System.Text.ValueStringBuilder sb, ref NumberBuffer number, ReadOnlySpan<char> format, NumberFormatInfo info)
		{
			int num = 0;
			char* digits = number.digits;
			int num2 = FindSection(format, (*digits == '\0') ? 2 : (number.sign ? 1 : 0));
			int num3;
			int num4;
			bool flag;
			bool flag2;
			int num5;
			int num6;
			int num9;
			while (true)
			{
				num3 = 0;
				num4 = -1;
				num5 = int.MaxValue;
				num6 = 0;
				flag = false;
				int num7 = -1;
				flag2 = false;
				int num8 = 0;
				num9 = num2;
				fixed (char* ptr = &MemoryMarshal.GetReference(format))
				{
					char c;
					while (num9 < format.Length && (c = ptr[num9++]) != 0)
					{
						switch (c)
						{
						case ';':
							break;
						case '#':
							num3++;
							continue;
						case '0':
							if (num5 == int.MaxValue)
							{
								num5 = num3;
							}
							num3++;
							num6 = num3;
							continue;
						case '.':
							if (num4 < 0)
							{
								num4 = num3;
							}
							continue;
						case ',':
							if (num3 <= 0 || num4 >= 0)
							{
								continue;
							}
							if (num7 >= 0)
							{
								if (num7 == num3)
								{
									num++;
									continue;
								}
								flag2 = true;
							}
							num7 = num3;
							num = 1;
							continue;
						case '%':
							num8 += 2;
							continue;
						case '‰':
							num8 += 3;
							continue;
						case '"':
						case '\'':
							while (num9 < format.Length && ptr[num9] != 0 && ptr[num9++] != c)
							{
							}
							continue;
						case '\\':
							if (num9 < format.Length && ptr[num9] != 0)
							{
								num9++;
							}
							continue;
						case 'E':
						case 'e':
							if ((num9 < format.Length && ptr[num9] == '0') || (num9 + 1 < format.Length && (ptr[num9] == '+' || ptr[num9] == '-') && ptr[num9 + 1] == '0'))
							{
								while (++num9 < format.Length && ptr[num9] == '0')
								{
								}
								flag = true;
							}
							continue;
						default:
							continue;
						}
						break;
					}
				}
				if (num4 < 0)
				{
					num4 = num3;
				}
				if (num7 >= 0)
				{
					if (num7 == num4)
					{
						num8 -= num * 3;
					}
					else
					{
						flag2 = true;
					}
				}
				if (*digits != 0)
				{
					number.scale += num8;
					int pos = (flag ? num3 : (number.scale + num3 - num4));
					RoundNumber(ref number, pos);
					if (*digits != 0)
					{
						break;
					}
					num9 = FindSection(format, 2);
					if (num9 == num2)
					{
						break;
					}
					num2 = num9;
					continue;
				}
				number.sign = false;
				number.scale = 0;
				break;
			}
			num5 = ((num5 < num4) ? (num4 - num5) : 0);
			num6 = ((num6 > num4) ? (num4 - num6) : 0);
			int num10;
			int num11;
			if (flag)
			{
				num10 = num4;
				num11 = 0;
			}
			else
			{
				num10 = ((number.scale > num4) ? number.scale : num4);
				num11 = number.scale - num4;
			}
			num9 = num2;
			Span<int> span = stackalloc int[4];
			int num12 = -1;
			if (flag2 && info.NumberGroupSeparator.Length > 0)
			{
				int[] numberGroupSizes = info.NumberGroupSizes;
				int num13 = 0;
				int i = 0;
				int num14 = numberGroupSizes.Length;
				if (num14 != 0)
				{
					i = numberGroupSizes[num13];
				}
				int num15 = i;
				int num16 = num10 + ((num11 < 0) ? num11 : 0);
				for (int num17 = ((num5 > num16) ? num5 : num16); num17 > i; i += num15)
				{
					if (num15 == 0)
					{
						break;
					}
					num12++;
					if (num12 >= span.Length)
					{
						int[] array = new int[span.Length * 2];
						span.CopyTo(array);
						span = array;
					}
					span[num12] = i;
					if (num13 < num14 - 1)
					{
						num13++;
						num15 = numberGroupSizes[num13];
					}
				}
			}
			if (number.sign && num2 == 0)
			{
				sb.Append(info.NegativeSign);
			}
			bool flag3 = false;
			fixed (char* ptr3 = &MemoryMarshal.GetReference(format))
			{
				char* ptr2 = digits;
				char c;
				while (num9 < format.Length && (c = ptr3[num9++]) != 0 && c != ';')
				{
					if (num11 > 0 && (c == '#' || c == '.' || c == '0'))
					{
						while (num11 > 0)
						{
							sb.Append((*ptr2 != 0) ? (*(ptr2++)) : '0');
							if (flag2 && num10 > 1 && num12 >= 0 && num10 == span[num12] + 1)
							{
								sb.Append(info.NumberGroupSeparator);
								num12--;
							}
							num10--;
							num11--;
						}
					}
					switch (c)
					{
					case '#':
					case '0':
						if (num11 < 0)
						{
							num11++;
							c = ((num10 <= num5) ? '0' : '\0');
						}
						else
						{
							c = ((*ptr2 != 0) ? (*(ptr2++)) : ((num10 > num6) ? '0' : '\0'));
						}
						if (c != 0)
						{
							sb.Append(c);
							if (flag2 && num10 > 1 && num12 >= 0 && num10 == span[num12] + 1)
							{
								sb.Append(info.NumberGroupSeparator);
								num12--;
							}
						}
						num10--;
						break;
					case '.':
						if (!(num10 != 0 || flag3) && (num6 < 0 || (num4 < num3 && *ptr2 != 0)))
						{
							sb.Append(info.NumberDecimalSeparator);
							flag3 = true;
						}
						break;
					case '‰':
						sb.Append(info.PerMilleSymbol);
						break;
					case '%':
						sb.Append(info.PercentSymbol);
						break;
					case '"':
					case '\'':
						while (num9 < format.Length && ptr3[num9] != 0 && ptr3[num9] != c)
						{
							sb.Append(ptr3[num9++]);
						}
						if (num9 < format.Length && ptr3[num9] != 0)
						{
							num9++;
						}
						break;
					case '\\':
						if (num9 < format.Length && ptr3[num9] != 0)
						{
							sb.Append(ptr3[num9++]);
						}
						break;
					case 'E':
					case 'e':
					{
						bool positiveSign = false;
						int num18 = 0;
						if (flag)
						{
							if (num9 < format.Length && ptr3[num9] == '0')
							{
								num18++;
							}
							else if (num9 + 1 < format.Length && ptr3[num9] == '+' && ptr3[num9 + 1] == '0')
							{
								positiveSign = true;
							}
							else if (num9 + 1 >= format.Length || ptr3[num9] != '-' || ptr3[num9 + 1] != '0')
							{
								sb.Append(c);
								break;
							}
							while (++num9 < format.Length && ptr3[num9] == '0')
							{
								num18++;
							}
							if (num18 > 10)
							{
								num18 = 10;
							}
							int value = ((*digits != 0) ? (number.scale - num4) : 0);
							FormatExponent(ref sb, info, value, c, num18, positiveSign);
							flag = false;
							break;
						}
						sb.Append(c);
						if (num9 < format.Length)
						{
							if (ptr3[num9] == '+' || ptr3[num9] == '-')
							{
								sb.Append(ptr3[num9++]);
							}
							while (num9 < format.Length && ptr3[num9] == '0')
							{
								sb.Append(ptr3[num9++]);
							}
						}
						break;
					}
					default:
						sb.Append(c);
						break;
					case ',':
						break;
					}
				}
			}
		}
	}

	internal unsafe static void FormatBigInteger(ref System.Text.ValueStringBuilder sb, int precision, int scale, bool sign, ReadOnlySpan<char> format, NumberFormatInfo numberFormatInfo, char[] digits, int startIndex)
	{
		fixed (char* ptr = digits)
		{
			Number.NumberBuffer number = default(Number.NumberBuffer);
			number.overrideDigits = ptr + startIndex;
			number.precision = precision;
			number.scale = scale;
			number.sign = sign;
			int digits2;
			char c = Number.ParseFormatSpecifier(format, out digits2);
			if (c != 0)
			{
				Number.NumberToString(ref sb, ref number, c, digits2, numberFormatInfo, isDecimal: false);
			}
			else
			{
				Number.NumberToStringFormat(ref sb, ref number, format, numberFormatInfo);
			}
		}
	}

	internal unsafe static bool TryStringToBigInteger(ReadOnlySpan<char> s, NumberStyles styles, NumberFormatInfo numberFormatInfo, StringBuilder receiver, out int precision, out int scale, out bool sign)
	{
		Number.NumberBuffer number = default(Number.NumberBuffer);
		number.overrideDigits = (char*)1;
		if (!Number.TryStringToNumber(s, styles, ref number, receiver, numberFormatInfo, parseDecimal: false))
		{
			precision = 0;
			scale = 0;
			sign = false;
			return false;
		}
		precision = number.precision;
		scale = number.scale;
		sign = number.sign;
		return true;
	}
}
