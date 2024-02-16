using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace System;

internal static class DateTimeParse
{
	internal delegate bool MatchNumberDelegate(ref __DTString str, int digitLen, out int result);

	internal enum DTT
	{
		End,
		NumEnd,
		NumAmpm,
		NumSpace,
		NumDatesep,
		NumTimesep,
		MonthEnd,
		MonthSpace,
		MonthDatesep,
		NumDatesuff,
		NumTimesuff,
		DayOfWeek,
		YearSpace,
		YearDateSep,
		YearEnd,
		TimeZone,
		Era,
		NumUTCTimeMark,
		Unk,
		NumLocalTimeMark,
		Max
	}

	internal enum TM
	{
		NotSet = -1,
		AM,
		PM
	}

	internal enum DS
	{
		BEGIN,
		N,
		NN,
		D_Nd,
		D_NN,
		D_NNd,
		D_M,
		D_MN,
		D_NM,
		D_MNd,
		D_NDS,
		D_Y,
		D_YN,
		D_YNd,
		D_YM,
		D_YMd,
		D_S,
		T_S,
		T_Nt,
		T_NNt,
		ERROR,
		DX_NN,
		DX_NNN,
		DX_MN,
		DX_NM,
		DX_MNN,
		DX_DS,
		DX_DSN,
		DX_NDS,
		DX_NNDS,
		DX_YNN,
		DX_YMN,
		DX_YN,
		DX_YM,
		TX_N,
		TX_NN,
		TX_NNN,
		TX_TS,
		DX_NNY
	}

	private static readonly MatchNumberDelegate s_hebrewNumberParser = MatchHebrewDigits;

	private static readonly DS[][] s_dateParsingStates = new DS[20][]
	{
		new DS[18]
		{
			DS.BEGIN,
			DS.ERROR,
			DS.TX_N,
			DS.N,
			DS.D_Nd,
			DS.T_Nt,
			DS.ERROR,
			DS.D_M,
			DS.D_M,
			DS.D_S,
			DS.T_S,
			DS.BEGIN,
			DS.D_Y,
			DS.D_Y,
			DS.ERROR,
			DS.BEGIN,
			DS.BEGIN,
			DS.ERROR
		},
		new DS[18]
		{
			DS.ERROR,
			DS.DX_NN,
			DS.ERROR,
			DS.NN,
			DS.D_NNd,
			DS.ERROR,
			DS.DX_NM,
			DS.D_NM,
			DS.D_MNd,
			DS.D_NDS,
			DS.ERROR,
			DS.N,
			DS.D_YN,
			DS.D_YNd,
			DS.DX_YN,
			DS.N,
			DS.N,
			DS.ERROR
		},
		new DS[18]
		{
			DS.DX_NN,
			DS.DX_NNN,
			DS.TX_N,
			DS.DX_NNN,
			DS.ERROR,
			DS.T_Nt,
			DS.DX_MNN,
			DS.DX_MNN,
			DS.ERROR,
			DS.ERROR,
			DS.T_S,
			DS.NN,
			DS.DX_NNY,
			DS.ERROR,
			DS.DX_NNY,
			DS.NN,
			DS.NN,
			DS.ERROR
		},
		new DS[18]
		{
			DS.ERROR,
			DS.DX_NN,
			DS.ERROR,
			DS.D_NN,
			DS.D_NNd,
			DS.ERROR,
			DS.DX_NM,
			DS.D_MN,
			DS.D_MNd,
			DS.ERROR,
			DS.ERROR,
			DS.D_Nd,
			DS.D_YN,
			DS.D_YNd,
			DS.DX_YN,
			DS.ERROR,
			DS.D_Nd,
			DS.ERROR
		},
		new DS[18]
		{
			DS.DX_NN,
			DS.DX_NNN,
			DS.TX_N,
			DS.DX_NNN,
			DS.ERROR,
			DS.T_Nt,
			DS.DX_MNN,
			DS.DX_MNN,
			DS.ERROR,
			DS.DX_DS,
			DS.T_S,
			DS.D_NN,
			DS.DX_NNY,
			DS.ERROR,
			DS.DX_NNY,
			DS.ERROR,
			DS.D_NN,
			DS.ERROR
		},
		new DS[18]
		{
			DS.ERROR,
			DS.DX_NNN,
			DS.DX_NNN,
			DS.DX_NNN,
			DS.ERROR,
			DS.ERROR,
			DS.DX_MNN,
			DS.DX_MNN,
			DS.ERROR,
			DS.DX_DS,
			DS.ERROR,
			DS.D_NNd,
			DS.DX_NNY,
			DS.ERROR,
			DS.DX_NNY,
			DS.ERROR,
			DS.D_NNd,
			DS.ERROR
		},
		new DS[18]
		{
			DS.ERROR,
			DS.DX_MN,
			DS.ERROR,
			DS.D_MN,
			DS.D_MNd,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_M,
			DS.D_YM,
			DS.D_YMd,
			DS.DX_YM,
			DS.ERROR,
			DS.D_M,
			DS.ERROR
		},
		new DS[18]
		{
			DS.DX_MN,
			DS.DX_MNN,
			DS.DX_MNN,
			DS.DX_MNN,
			DS.ERROR,
			DS.T_Nt,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.DX_DS,
			DS.T_S,
			DS.D_MN,
			DS.DX_YMN,
			DS.ERROR,
			DS.DX_YMN,
			DS.ERROR,
			DS.D_MN,
			DS.ERROR
		},
		new DS[18]
		{
			DS.DX_NM,
			DS.DX_MNN,
			DS.DX_MNN,
			DS.DX_MNN,
			DS.ERROR,
			DS.T_Nt,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.DX_DS,
			DS.T_S,
			DS.D_NM,
			DS.DX_YMN,
			DS.ERROR,
			DS.DX_YMN,
			DS.ERROR,
			DS.D_NM,
			DS.ERROR
		},
		new DS[18]
		{
			DS.ERROR,
			DS.DX_MNN,
			DS.ERROR,
			DS.DX_MNN,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_MNd,
			DS.DX_YMN,
			DS.ERROR,
			DS.DX_YMN,
			DS.ERROR,
			DS.D_MNd,
			DS.ERROR
		},
		new DS[18]
		{
			DS.DX_NDS,
			DS.DX_NNDS,
			DS.DX_NNDS,
			DS.DX_NNDS,
			DS.ERROR,
			DS.T_Nt,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_NDS,
			DS.T_S,
			DS.D_NDS,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_NDS,
			DS.ERROR
		},
		new DS[18]
		{
			DS.ERROR,
			DS.DX_YN,
			DS.ERROR,
			DS.D_YN,
			DS.D_YNd,
			DS.ERROR,
			DS.DX_YM,
			DS.D_YM,
			DS.D_YMd,
			DS.D_YM,
			DS.ERROR,
			DS.D_Y,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_Y,
			DS.ERROR
		},
		new DS[18]
		{
			DS.DX_YN,
			DS.DX_YNN,
			DS.DX_YNN,
			DS.DX_YNN,
			DS.ERROR,
			DS.ERROR,
			DS.DX_YMN,
			DS.DX_YMN,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_YN,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_YN,
			DS.ERROR
		},
		new DS[18]
		{
			DS.ERROR,
			DS.DX_YNN,
			DS.DX_YNN,
			DS.DX_YNN,
			DS.ERROR,
			DS.ERROR,
			DS.DX_YMN,
			DS.DX_YMN,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_YN,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_YN,
			DS.ERROR
		},
		new DS[18]
		{
			DS.DX_YM,
			DS.DX_YMN,
			DS.DX_YMN,
			DS.DX_YMN,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_YM,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_YM,
			DS.ERROR
		},
		new DS[18]
		{
			DS.ERROR,
			DS.DX_YMN,
			DS.DX_YMN,
			DS.DX_YMN,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_YM,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_YM,
			DS.ERROR
		},
		new DS[18]
		{
			DS.DX_DS,
			DS.DX_DSN,
			DS.TX_N,
			DS.T_Nt,
			DS.ERROR,
			DS.T_Nt,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_S,
			DS.T_S,
			DS.D_S,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_S,
			DS.ERROR
		},
		new DS[18]
		{
			DS.TX_TS,
			DS.TX_TS,
			DS.TX_TS,
			DS.T_Nt,
			DS.D_Nd,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.D_S,
			DS.T_S,
			DS.T_S,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.T_S,
			DS.T_S,
			DS.ERROR
		},
		new DS[18]
		{
			DS.ERROR,
			DS.TX_NN,
			DS.TX_NN,
			DS.TX_NN,
			DS.ERROR,
			DS.T_NNt,
			DS.DX_NM,
			DS.D_NM,
			DS.ERROR,
			DS.ERROR,
			DS.T_S,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.T_Nt,
			DS.T_Nt,
			DS.TX_NN
		},
		new DS[18]
		{
			DS.ERROR,
			DS.TX_NNN,
			DS.TX_NNN,
			DS.TX_NNN,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.T_S,
			DS.T_NNt,
			DS.ERROR,
			DS.ERROR,
			DS.ERROR,
			DS.T_NNt,
			DS.T_NNt,
			DS.TX_NNN
		}
	};

	internal static DateTime ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, DateTimeStyles style)
	{
		DateTimeResult result = default(DateTimeResult);
		result.Init(s);
		if (TryParseExact(s, format, dtfi, style, ref result))
		{
			return result.parsedDate;
		}
		throw GetDateTimeParseException(ref result);
	}

	internal static DateTime ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, DateTimeStyles style, out TimeSpan offset)
	{
		DateTimeResult result = default(DateTimeResult);
		result.Init(s);
		result.flags |= ParseFlags.CaptureOffset;
		if (TryParseExact(s, format, dtfi, style, ref result))
		{
			offset = result.timeZoneOffset;
			return result.parsedDate;
		}
		throw GetDateTimeParseException(ref result);
	}

	internal static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result)
	{
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		if (TryParseExact(s, format, dtfi, style, ref result2))
		{
			result = result2.parsedDate;
			return true;
		}
		result = DateTime.MinValue;
		return false;
	}

	internal static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result, out TimeSpan offset)
	{
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		result2.flags |= ParseFlags.CaptureOffset;
		if (TryParseExact(s, format, dtfi, style, ref result2))
		{
			result = result2.parsedDate;
			offset = result2.timeZoneOffset;
			return true;
		}
		result = DateTime.MinValue;
		offset = TimeSpan.Zero;
		return false;
	}

	internal static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, DateTimeStyles style, ref DateTimeResult result)
	{
		if (s.Length == 0)
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDateTime");
			return false;
		}
		if (format.Length == 0)
		{
			result.SetBadFormatSpecifierFailure();
			return false;
		}
		return DoStrictParse(s, format, style, dtfi, ref result);
	}

	internal static DateTime ParseExactMultiple(ReadOnlySpan<char> s, string[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style)
	{
		DateTimeResult result = default(DateTimeResult);
		result.Init(s);
		if (TryParseExactMultiple(s, formats, dtfi, style, ref result))
		{
			return result.parsedDate;
		}
		throw GetDateTimeParseException(ref result);
	}

	internal static DateTime ParseExactMultiple(ReadOnlySpan<char> s, string[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, out TimeSpan offset)
	{
		DateTimeResult result = default(DateTimeResult);
		result.Init(s);
		result.flags |= ParseFlags.CaptureOffset;
		if (TryParseExactMultiple(s, formats, dtfi, style, ref result))
		{
			offset = result.timeZoneOffset;
			return result.parsedDate;
		}
		throw GetDateTimeParseException(ref result);
	}

	internal static bool TryParseExactMultiple(ReadOnlySpan<char> s, string[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result, out TimeSpan offset)
	{
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		result2.flags |= ParseFlags.CaptureOffset;
		if (TryParseExactMultiple(s, formats, dtfi, style, ref result2))
		{
			result = result2.parsedDate;
			offset = result2.timeZoneOffset;
			return true;
		}
		result = DateTime.MinValue;
		offset = TimeSpan.Zero;
		return false;
	}

	internal static bool TryParseExactMultiple(ReadOnlySpan<char> s, string[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result)
	{
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		if (TryParseExactMultiple(s, formats, dtfi, style, ref result2))
		{
			result = result2.parsedDate;
			return true;
		}
		result = DateTime.MinValue;
		return false;
	}

	internal static bool TryParseExactMultiple(ReadOnlySpan<char> s, string[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, ref DateTimeResult result)
	{
		if (formats == null)
		{
			result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "formats");
			return false;
		}
		if (s.Length == 0)
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDateTime");
			return false;
		}
		if (formats.Length == 0)
		{
			result.SetFailure(ParseFailureKind.Format, "Format_NoFormatSpecifier");
			return false;
		}
		foreach (string text in formats)
		{
			if (string.IsNullOrEmpty(text))
			{
				result.SetBadFormatSpecifierFailure();
				return false;
			}
			DateTimeResult result2 = default(DateTimeResult);
			result2.Init(s);
			result2.flags = result.flags;
			if (TryParseExact(s, text, dtfi, style, ref result2))
			{
				result.parsedDate = result2.parsedDate;
				result.timeZoneOffset = result2.timeZoneOffset;
				return true;
			}
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static bool MatchWord(ref __DTString str, string target)
	{
		if (target.Length > str.Value.Length - str.Index)
		{
			return false;
		}
		if (str.CompareInfo.Compare(str.Value.Slice(str.Index, target.Length), target, CompareOptions.IgnoreCase) != 0)
		{
			return false;
		}
		int num = str.Index + target.Length;
		if (num < str.Value.Length)
		{
			char c = str.Value[num];
			if (char.IsLetter(c))
			{
				return false;
			}
		}
		str.Index = num;
		if (str.Index < str.Length)
		{
			str.m_current = str.Value[str.Index];
		}
		return true;
	}

	private static bool GetTimeZoneName(ref __DTString str)
	{
		if (MatchWord(ref str, "GMT"))
		{
			return true;
		}
		if (MatchWord(ref str, "Z"))
		{
			return true;
		}
		return false;
	}

	internal static bool IsDigit(char ch)
	{
		return (uint)(ch - 48) <= 9u;
	}

	private static bool ParseFraction(ref __DTString str, out double result)
	{
		result = 0.0;
		double num = 0.1;
		int num2 = 0;
		char current;
		while (str.GetNext() && IsDigit(current = str.m_current))
		{
			result += (double)(current - 48) * num;
			num *= 0.1;
			num2++;
		}
		return num2 > 0;
	}

	private static bool ParseTimeZone(ref __DTString str, ref TimeSpan result)
	{
		int num = 0;
		DTSubString subString = str.GetSubString();
		if (subString.length != 1)
		{
			return false;
		}
		char c = subString[0];
		if (c != '+' && c != '-')
		{
			return false;
		}
		str.ConsumeSubString(subString);
		subString = str.GetSubString();
		if (subString.type != DTSubStringType.Number)
		{
			return false;
		}
		int value = subString.value;
		int hours;
		switch (subString.length)
		{
		case 1:
		case 2:
			hours = value;
			str.ConsumeSubString(subString);
			subString = str.GetSubString();
			if (subString.length == 1 && subString[0] == ':')
			{
				str.ConsumeSubString(subString);
				subString = str.GetSubString();
				if (subString.type != DTSubStringType.Number || subString.length < 1 || subString.length > 2)
				{
					return false;
				}
				num = subString.value;
				str.ConsumeSubString(subString);
			}
			break;
		case 3:
		case 4:
			hours = value / 100;
			num = value % 100;
			str.ConsumeSubString(subString);
			break;
		default:
			return false;
		}
		if (num < 0 || num >= 60)
		{
			return false;
		}
		result = new TimeSpan(hours, num, 0);
		if (c == '-')
		{
			result = result.Negate();
		}
		return true;
	}

	private static bool HandleTimeZone(ref __DTString str, ref DateTimeResult result)
	{
		if (str.Index < str.Length - 1)
		{
			char c = str.Value[str.Index];
			int num = 0;
			while (char.IsWhiteSpace(c) && str.Index + num < str.Length - 1)
			{
				num++;
				c = str.Value[str.Index + num];
			}
			if (c == '+' || c == '-')
			{
				str.Index += num;
				if ((result.flags & ParseFlags.TimeZoneUsed) != 0)
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				result.flags |= ParseFlags.TimeZoneUsed;
				if (!ParseTimeZone(ref str, ref result.timeZoneOffset))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
			}
		}
		return true;
	}

	private static bool Lex(DS dps, ref __DTString str, ref DateTimeToken dtok, ref DateTimeRawInfo raw, ref DateTimeResult result, ref DateTimeFormatInfo dtfi, DateTimeStyles styles)
	{
		dtok.dtt = DTT.Unk;
		str.GetRegularToken(out var tokenType, out var tokenValue, dtfi);
		int indexBeforeSeparator;
		char charBeforeSeparator;
		switch (tokenType)
		{
		case TokenType.NumberToken:
		case TokenType.YearNumberToken:
		{
			if (raw.numCount == 3 || tokenValue == -1)
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (dps == DS.T_NNt && str.Index < str.Length - 1)
			{
				char c = str.Value[str.Index];
				if (c == '.')
				{
					ParseFraction(ref str, out raw.fraction);
				}
			}
			if ((dps == DS.T_NNt || dps == DS.T_Nt) && str.Index < str.Length - 1 && !HandleTimeZone(ref str, ref result))
			{
				return false;
			}
			dtok.num = tokenValue;
			TokenType separatorToken;
			if (tokenType == TokenType.YearNumberToken)
			{
				if (raw.year == -1)
				{
					raw.year = tokenValue;
					switch (separatorToken = str.GetSeparatorToken(dtfi, out indexBeforeSeparator, out charBeforeSeparator))
					{
					case TokenType.SEP_End:
						dtok.dtt = DTT.YearEnd;
						break;
					case TokenType.SEP_Am:
					case TokenType.SEP_Pm:
						if (raw.timeMark == TM.NotSet)
						{
							raw.timeMark = ((separatorToken != TokenType.SEP_Am) ? TM.PM : TM.AM);
							dtok.dtt = DTT.YearSpace;
						}
						else
						{
							result.SetBadDateTimeFailure();
						}
						break;
					case TokenType.SEP_Space:
						dtok.dtt = DTT.YearSpace;
						break;
					case TokenType.SEP_Date:
						dtok.dtt = DTT.YearDateSep;
						break;
					case TokenType.SEP_Time:
						if (!raw.hasSameDateAndTimeSeparators)
						{
							result.SetBadDateTimeFailure();
							return false;
						}
						dtok.dtt = DTT.YearDateSep;
						break;
					case TokenType.SEP_DateOrOffset:
						if (s_dateParsingStates[(int)dps][13] == DS.ERROR && s_dateParsingStates[(int)dps][12] > DS.ERROR)
						{
							str.Index = indexBeforeSeparator;
							str.m_current = charBeforeSeparator;
							dtok.dtt = DTT.YearSpace;
						}
						else
						{
							dtok.dtt = DTT.YearDateSep;
						}
						break;
					case TokenType.SEP_YearSuff:
					case TokenType.SEP_MonthSuff:
					case TokenType.SEP_DaySuff:
						dtok.dtt = DTT.NumDatesuff;
						dtok.suffix = separatorToken;
						break;
					case TokenType.SEP_HourSuff:
					case TokenType.SEP_MinuteSuff:
					case TokenType.SEP_SecondSuff:
						dtok.dtt = DTT.NumTimesuff;
						dtok.suffix = separatorToken;
						break;
					default:
						result.SetBadDateTimeFailure();
						return false;
					}
					return true;
				}
				result.SetBadDateTimeFailure();
				return false;
			}
			switch (separatorToken = str.GetSeparatorToken(dtfi, out indexBeforeSeparator, out charBeforeSeparator))
			{
			case TokenType.SEP_End:
				dtok.dtt = DTT.NumEnd;
				raw.AddNumber(dtok.num);
				break;
			case TokenType.SEP_Am:
			case TokenType.SEP_Pm:
				if (raw.timeMark == TM.NotSet)
				{
					raw.timeMark = ((separatorToken != TokenType.SEP_Am) ? TM.PM : TM.AM);
					dtok.dtt = DTT.NumAmpm;
					if (dps == DS.D_NN && !ProcessTerminalState(DS.DX_NN, ref result, ref styles, ref raw, dtfi))
					{
						return false;
					}
					raw.AddNumber(dtok.num);
					if ((dps == DS.T_NNt || dps == DS.T_Nt) && !HandleTimeZone(ref str, ref result))
					{
						return false;
					}
				}
				else
				{
					result.SetBadDateTimeFailure();
				}
				break;
			case TokenType.SEP_Space:
				dtok.dtt = DTT.NumSpace;
				raw.AddNumber(dtok.num);
				break;
			case TokenType.SEP_Date:
				dtok.dtt = DTT.NumDatesep;
				raw.AddNumber(dtok.num);
				break;
			case TokenType.SEP_DateOrOffset:
				if (s_dateParsingStates[(int)dps][4] == DS.ERROR && s_dateParsingStates[(int)dps][3] > DS.ERROR)
				{
					str.Index = indexBeforeSeparator;
					str.m_current = charBeforeSeparator;
					dtok.dtt = DTT.NumSpace;
				}
				else
				{
					dtok.dtt = DTT.NumDatesep;
				}
				raw.AddNumber(dtok.num);
				break;
			case TokenType.SEP_Time:
				if (raw.hasSameDateAndTimeSeparators && (dps == DS.D_Y || dps == DS.D_YN || dps == DS.D_YNd || dps == DS.D_YM || dps == DS.D_YMd))
				{
					dtok.dtt = DTT.NumDatesep;
					raw.AddNumber(dtok.num);
				}
				else
				{
					dtok.dtt = DTT.NumTimesep;
					raw.AddNumber(dtok.num);
				}
				break;
			case TokenType.SEP_YearSuff:
				try
				{
					dtok.num = dtfi.Calendar.ToFourDigitYear(tokenValue);
				}
				catch (ArgumentOutOfRangeException)
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				dtok.dtt = DTT.NumDatesuff;
				dtok.suffix = separatorToken;
				break;
			case TokenType.SEP_MonthSuff:
			case TokenType.SEP_DaySuff:
				dtok.dtt = DTT.NumDatesuff;
				dtok.suffix = separatorToken;
				break;
			case TokenType.SEP_HourSuff:
			case TokenType.SEP_MinuteSuff:
			case TokenType.SEP_SecondSuff:
				dtok.dtt = DTT.NumTimesuff;
				dtok.suffix = separatorToken;
				break;
			case TokenType.SEP_LocalTimeMark:
				dtok.dtt = DTT.NumLocalTimeMark;
				raw.AddNumber(dtok.num);
				break;
			default:
				result.SetBadDateTimeFailure();
				return false;
			}
			break;
		}
		case TokenType.HebrewNumber:
		{
			TokenType separatorToken;
			if (tokenValue >= 100)
			{
				if (raw.year == -1)
				{
					raw.year = tokenValue;
					TokenType tokenType2 = (separatorToken = str.GetSeparatorToken(dtfi, out indexBeforeSeparator, out charBeforeSeparator));
					if (tokenType2 != TokenType.SEP_End)
					{
						if (tokenType2 != TokenType.SEP_Space)
						{
							if (tokenType2 != TokenType.SEP_DateOrOffset || s_dateParsingStates[(int)dps][12] <= DS.ERROR)
							{
								result.SetBadDateTimeFailure();
								return false;
							}
							str.Index = indexBeforeSeparator;
							str.m_current = charBeforeSeparator;
							dtok.dtt = DTT.YearSpace;
						}
						else
						{
							dtok.dtt = DTT.YearSpace;
						}
					}
					else
					{
						dtok.dtt = DTT.YearEnd;
					}
					break;
				}
				result.SetBadDateTimeFailure();
				return false;
			}
			dtok.num = tokenValue;
			raw.AddNumber(dtok.num);
			switch (separatorToken = str.GetSeparatorToken(dtfi, out indexBeforeSeparator, out charBeforeSeparator))
			{
			case TokenType.SEP_End:
				dtok.dtt = DTT.NumEnd;
				break;
			case TokenType.SEP_Space:
			case TokenType.SEP_Date:
				dtok.dtt = DTT.NumDatesep;
				break;
			case TokenType.SEP_DateOrOffset:
				if (s_dateParsingStates[(int)dps][4] == DS.ERROR && s_dateParsingStates[(int)dps][3] > DS.ERROR)
				{
					str.Index = indexBeforeSeparator;
					str.m_current = charBeforeSeparator;
					dtok.dtt = DTT.NumSpace;
				}
				else
				{
					dtok.dtt = DTT.NumDatesep;
				}
				break;
			default:
				result.SetBadDateTimeFailure();
				return false;
			}
			break;
		}
		case TokenType.DayOfWeekToken:
			if (raw.dayOfWeek == -1)
			{
				raw.dayOfWeek = tokenValue;
				dtok.dtt = DTT.DayOfWeek;
				break;
			}
			result.SetBadDateTimeFailure();
			return false;
		case TokenType.MonthToken:
			if (raw.month == -1)
			{
				TokenType separatorToken;
				switch (separatorToken = str.GetSeparatorToken(dtfi, out indexBeforeSeparator, out charBeforeSeparator))
				{
				case TokenType.SEP_End:
					dtok.dtt = DTT.MonthEnd;
					break;
				case TokenType.SEP_Space:
					dtok.dtt = DTT.MonthSpace;
					break;
				case TokenType.SEP_Date:
					dtok.dtt = DTT.MonthDatesep;
					break;
				case TokenType.SEP_Time:
					if (!raw.hasSameDateAndTimeSeparators)
					{
						result.SetBadDateTimeFailure();
						return false;
					}
					dtok.dtt = DTT.MonthDatesep;
					break;
				case TokenType.SEP_DateOrOffset:
					if (s_dateParsingStates[(int)dps][8] == DS.ERROR && s_dateParsingStates[(int)dps][7] > DS.ERROR)
					{
						str.Index = indexBeforeSeparator;
						str.m_current = charBeforeSeparator;
						dtok.dtt = DTT.MonthSpace;
					}
					else
					{
						dtok.dtt = DTT.MonthDatesep;
					}
					break;
				default:
					result.SetBadDateTimeFailure();
					return false;
				}
				raw.month = tokenValue;
				break;
			}
			result.SetBadDateTimeFailure();
			return false;
		case TokenType.EraToken:
			if (result.era != -1)
			{
				result.era = tokenValue;
				dtok.dtt = DTT.Era;
				break;
			}
			result.SetBadDateTimeFailure();
			return false;
		case TokenType.JapaneseEraToken:
			if (GlobalizationMode.Invariant)
			{
				return false;
			}
			result.calendar = JapaneseCalendar.GetDefaultInstance();
			dtfi = DateTimeFormatInfo.GetJapaneseCalendarDTFI();
			if (result.era != -1)
			{
				result.era = tokenValue;
				dtok.dtt = DTT.Era;
				break;
			}
			result.SetBadDateTimeFailure();
			return false;
		case TokenType.TEraToken:
			if (GlobalizationMode.Invariant)
			{
				return false;
			}
			result.calendar = TaiwanCalendar.GetDefaultInstance();
			dtfi = DateTimeFormatInfo.GetTaiwanCalendarDTFI();
			if (result.era != -1)
			{
				result.era = tokenValue;
				dtok.dtt = DTT.Era;
				break;
			}
			result.SetBadDateTimeFailure();
			return false;
		case TokenType.TimeZoneToken:
			if ((result.flags & ParseFlags.TimeZoneUsed) != 0)
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			dtok.dtt = DTT.TimeZone;
			result.flags |= ParseFlags.TimeZoneUsed;
			result.timeZoneOffset = new TimeSpan(0L);
			result.flags |= ParseFlags.TimeZoneUtc;
			break;
		case TokenType.EndOfString:
			dtok.dtt = DTT.End;
			break;
		case TokenType.Am:
		case TokenType.Pm:
			if (raw.timeMark == TM.NotSet)
			{
				raw.timeMark = (TM)tokenValue;
				break;
			}
			result.SetBadDateTimeFailure();
			return false;
		case TokenType.UnknownToken:
			if (char.IsLetter(str.m_current))
			{
				result.SetFailure(ParseFailureKind.FormatWithOriginalDateTimeAndParameter, "Format_UnknownDateTimeWord", str.Index);
				return false;
			}
			if ((str.m_current == '-' || str.m_current == '+') && (result.flags & ParseFlags.TimeZoneUsed) == 0)
			{
				int index = str.Index;
				if (ParseTimeZone(ref str, ref result.timeZoneOffset))
				{
					result.flags |= ParseFlags.TimeZoneUsed;
					return true;
				}
				str.Index = index;
			}
			if (VerifyValidPunctuation(ref str))
			{
				return true;
			}
			result.SetBadDateTimeFailure();
			return false;
		}
		return true;
	}

	private static bool VerifyValidPunctuation(ref __DTString str)
	{
		switch (str.Value[str.Index])
		{
		case '#':
		{
			bool flag = false;
			bool flag2 = false;
			for (int j = 0; j < str.Length; j++)
			{
				char c = str.Value[j];
				switch (c)
				{
				case '#':
					if (flag)
					{
						if (flag2)
						{
							return false;
						}
						flag2 = true;
					}
					else
					{
						flag = true;
					}
					break;
				case '\0':
					if (!flag2)
					{
						return false;
					}
					break;
				default:
					if (!char.IsWhiteSpace(c) && (!flag || flag2))
					{
						return false;
					}
					break;
				}
			}
			if (!flag2)
			{
				return false;
			}
			str.GetNext();
			return true;
		}
		case '\0':
		{
			for (int i = str.Index; i < str.Length; i++)
			{
				if (str.Value[i] != 0)
				{
					return false;
				}
			}
			str.Index = str.Length;
			return true;
		}
		default:
			return false;
		}
	}

	private static bool GetYearMonthDayOrder(string datePattern, out int order)
	{
		int num = -1;
		int num2 = -1;
		int num3 = -1;
		int num4 = 0;
		bool flag = false;
		for (int i = 0; i < datePattern.Length && num4 < 3; i++)
		{
			char c = datePattern[i];
			switch (c)
			{
			case '%':
			case '\\':
				i++;
				continue;
			case '"':
			case '\'':
				flag = !flag;
				break;
			}
			if (flag)
			{
				continue;
			}
			switch (c)
			{
			case 'y':
				num = num4++;
				for (; i + 1 < datePattern.Length && datePattern[i + 1] == 'y'; i++)
				{
				}
				break;
			case 'M':
				num2 = num4++;
				for (; i + 1 < datePattern.Length && datePattern[i + 1] == 'M'; i++)
				{
				}
				break;
			case 'd':
			{
				int num5 = 1;
				for (; i + 1 < datePattern.Length && datePattern[i + 1] == 'd'; i++)
				{
					num5++;
				}
				if (num5 <= 2)
				{
					num3 = num4++;
				}
				break;
			}
			}
		}
		if (num == 0 && num2 == 1 && num3 == 2)
		{
			order = 0;
			return true;
		}
		if (num2 == 0 && num3 == 1 && num == 2)
		{
			order = 1;
			return true;
		}
		if (num3 == 0 && num2 == 1 && num == 2)
		{
			order = 2;
			return true;
		}
		if (num == 0 && num3 == 1 && num2 == 2)
		{
			order = 3;
			return true;
		}
		order = -1;
		return false;
	}

	private static bool GetYearMonthOrder(string pattern, out int order)
	{
		int num = -1;
		int num2 = -1;
		int num3 = 0;
		bool flag = false;
		for (int i = 0; i < pattern.Length && num3 < 2; i++)
		{
			char c = pattern[i];
			switch (c)
			{
			case '%':
			case '\\':
				i++;
				continue;
			case '"':
			case '\'':
				flag = !flag;
				break;
			}
			if (flag)
			{
				continue;
			}
			switch (c)
			{
			case 'y':
				num = num3++;
				for (; i + 1 < pattern.Length && pattern[i + 1] == 'y'; i++)
				{
				}
				break;
			case 'M':
				num2 = num3++;
				for (; i + 1 < pattern.Length && pattern[i + 1] == 'M'; i++)
				{
				}
				break;
			}
		}
		if (num == 0 && num2 == 1)
		{
			order = 4;
			return true;
		}
		if (num2 == 0 && num == 1)
		{
			order = 5;
			return true;
		}
		order = -1;
		return false;
	}

	private static bool GetMonthDayOrder(string pattern, out int order)
	{
		int num = -1;
		int num2 = -1;
		int num3 = 0;
		bool flag = false;
		for (int i = 0; i < pattern.Length && num3 < 2; i++)
		{
			char c = pattern[i];
			switch (c)
			{
			case '%':
			case '\\':
				i++;
				continue;
			case '"':
			case '\'':
				flag = !flag;
				break;
			}
			if (flag)
			{
				continue;
			}
			switch (c)
			{
			case 'd':
			{
				int num4 = 1;
				for (; i + 1 < pattern.Length && pattern[i + 1] == 'd'; i++)
				{
					num4++;
				}
				if (num4 <= 2)
				{
					num2 = num3++;
				}
				break;
			}
			case 'M':
				num = num3++;
				for (; i + 1 < pattern.Length && pattern[i + 1] == 'M'; i++)
				{
				}
				break;
			}
		}
		if (num == 0 && num2 == 1)
		{
			order = 6;
			return true;
		}
		if (num2 == 0 && num == 1)
		{
			order = 7;
			return true;
		}
		order = -1;
		return false;
	}

	private static bool TryAdjustYear(ref DateTimeResult result, int year, out int adjustedYear)
	{
		if (year < 100)
		{
			try
			{
				year = result.calendar.ToFourDigitYear(year);
			}
			catch (ArgumentOutOfRangeException)
			{
				adjustedYear = -1;
				return false;
			}
		}
		adjustedYear = year;
		return true;
	}

	private static bool SetDateYMD(ref DateTimeResult result, int year, int month, int day)
	{
		if (result.calendar.IsValidDay(year, month, day, result.era))
		{
			result.SetDate(year, month, day);
			return true;
		}
		return false;
	}

	private static bool SetDateMDY(ref DateTimeResult result, int month, int day, int year)
	{
		return SetDateYMD(ref result, year, month, day);
	}

	private static bool SetDateDMY(ref DateTimeResult result, int day, int month, int year)
	{
		return SetDateYMD(ref result, year, month, day);
	}

	private static bool SetDateYDM(ref DateTimeResult result, int year, int day, int month)
	{
		return SetDateYMD(ref result, year, month, day);
	}

	private static void GetDefaultYear(ref DateTimeResult result, ref DateTimeStyles styles)
	{
		result.Year = result.calendar.GetYear(GetDateTimeNow(ref result, ref styles));
		result.flags |= ParseFlags.YearDefault;
	}

	private static bool GetDayOfNN(ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		if ((result.flags & ParseFlags.HaveDate) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int number = raw.GetNumber(0);
		int number2 = raw.GetNumber(1);
		GetDefaultYear(ref result, ref styles);
		if (!GetMonthDayOrder(dtfi.MonthDayPattern, out var order))
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
			return false;
		}
		if (order == 6)
		{
			if (SetDateYMD(ref result, result.Year, number, number2))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
		}
		else if (SetDateYMD(ref result, result.Year, number2, number))
		{
			result.flags |= ParseFlags.HaveDate;
			return true;
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static bool GetDayOfNNN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		if ((result.flags & ParseFlags.HaveDate) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int number = raw.GetNumber(0);
		int number2 = raw.GetNumber(1);
		int number3 = raw.GetNumber(2);
		if (!GetYearMonthDayOrder(dtfi.ShortDatePattern, out var order))
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
			return false;
		}
		int adjustedYear;
		switch (order)
		{
		case 0:
			if (TryAdjustYear(ref result, number, out adjustedYear) && SetDateYMD(ref result, adjustedYear, number2, number3))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			break;
		case 1:
			if (TryAdjustYear(ref result, number3, out adjustedYear) && SetDateMDY(ref result, number, number2, adjustedYear))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			break;
		case 2:
			if (TryAdjustYear(ref result, number3, out adjustedYear) && SetDateDMY(ref result, number, number2, adjustedYear))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			break;
		case 3:
			if (TryAdjustYear(ref result, number, out adjustedYear) && SetDateYDM(ref result, adjustedYear, number2, number3))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			break;
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static bool GetDayOfMN(ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		if ((result.flags & ParseFlags.HaveDate) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (!GetMonthDayOrder(dtfi.MonthDayPattern, out var order))
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
			return false;
		}
		if (order == 7)
		{
			if (!GetYearMonthOrder(dtfi.YearMonthPattern, out var order2))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.YearMonthPattern);
				return false;
			}
			if (order2 == 5)
			{
				if (!TryAdjustYear(ref result, raw.GetNumber(0), out var adjustedYear) || !SetDateYMD(ref result, adjustedYear, raw.month, 1))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				return true;
			}
		}
		GetDefaultYear(ref result, ref styles);
		if (!SetDateYMD(ref result, result.Year, raw.month, raw.GetNumber(0)))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		return true;
	}

	private static bool GetHebrewDayOfNM(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		if (!GetMonthDayOrder(dtfi.MonthDayPattern, out var order))
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
			return false;
		}
		result.Month = raw.month;
		if ((order == 7 || order == 6) && result.calendar.IsValidDay(result.Year, result.Month, raw.GetNumber(0), result.era))
		{
			result.Day = raw.GetNumber(0);
			return true;
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static bool GetDayOfNM(ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		if ((result.flags & ParseFlags.HaveDate) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (!GetMonthDayOrder(dtfi.MonthDayPattern, out var order))
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
			return false;
		}
		if (order == 6)
		{
			if (!GetYearMonthOrder(dtfi.YearMonthPattern, out var order2))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.YearMonthPattern);
				return false;
			}
			if (order2 == 4)
			{
				if (!TryAdjustYear(ref result, raw.GetNumber(0), out var adjustedYear) || !SetDateYMD(ref result, adjustedYear, raw.month, 1))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				return true;
			}
		}
		GetDefaultYear(ref result, ref styles);
		if (!SetDateYMD(ref result, result.Year, raw.month, raw.GetNumber(0)))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		return true;
	}

	private static bool GetDayOfMNN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		if ((result.flags & ParseFlags.HaveDate) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int number = raw.GetNumber(0);
		int number2 = raw.GetNumber(1);
		if (!GetYearMonthDayOrder(dtfi.ShortDatePattern, out var order))
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
			return false;
		}
		int adjustedYear;
		switch (order)
		{
		case 1:
			if (TryAdjustYear(ref result, number2, out adjustedYear) && result.calendar.IsValidDay(adjustedYear, raw.month, number, result.era))
			{
				result.SetDate(adjustedYear, raw.month, number);
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			if (TryAdjustYear(ref result, number, out adjustedYear) && result.calendar.IsValidDay(adjustedYear, raw.month, number2, result.era))
			{
				result.SetDate(adjustedYear, raw.month, number2);
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			break;
		case 0:
			if (TryAdjustYear(ref result, number, out adjustedYear) && result.calendar.IsValidDay(adjustedYear, raw.month, number2, result.era))
			{
				result.SetDate(adjustedYear, raw.month, number2);
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			if (TryAdjustYear(ref result, number2, out adjustedYear) && result.calendar.IsValidDay(adjustedYear, raw.month, number, result.era))
			{
				result.SetDate(adjustedYear, raw.month, number);
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			break;
		case 2:
			if (TryAdjustYear(ref result, number2, out adjustedYear) && result.calendar.IsValidDay(adjustedYear, raw.month, number, result.era))
			{
				result.SetDate(adjustedYear, raw.month, number);
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			if (TryAdjustYear(ref result, number, out adjustedYear) && result.calendar.IsValidDay(adjustedYear, raw.month, number2, result.era))
			{
				result.SetDate(adjustedYear, raw.month, number2);
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			break;
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static bool GetDayOfYNN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		if ((result.flags & ParseFlags.HaveDate) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int number = raw.GetNumber(0);
		int number2 = raw.GetNumber(1);
		string shortDatePattern = dtfi.ShortDatePattern;
		if (GetYearMonthDayOrder(shortDatePattern, out var order) && order == 3)
		{
			if (SetDateYMD(ref result, raw.year, number2, number))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
		}
		else if (SetDateYMD(ref result, raw.year, number, number2))
		{
			result.flags |= ParseFlags.HaveDate;
			return true;
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static bool GetDayOfNNY(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		if ((result.flags & ParseFlags.HaveDate) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int number = raw.GetNumber(0);
		int number2 = raw.GetNumber(1);
		if (!GetYearMonthDayOrder(dtfi.ShortDatePattern, out var order))
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
			return false;
		}
		if (order == 1 || order == 0)
		{
			if (SetDateYMD(ref result, raw.year, number, number2))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
		}
		else if (SetDateYMD(ref result, raw.year, number2, number))
		{
			result.flags |= ParseFlags.HaveDate;
			return true;
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static bool GetDayOfYMN(ref DateTimeResult result, ref DateTimeRawInfo raw)
	{
		if ((result.flags & ParseFlags.HaveDate) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (SetDateYMD(ref result, raw.year, raw.month, raw.GetNumber(0)))
		{
			result.flags |= ParseFlags.HaveDate;
			return true;
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static bool GetDayOfYN(ref DateTimeResult result, ref DateTimeRawInfo raw)
	{
		if ((result.flags & ParseFlags.HaveDate) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (SetDateYMD(ref result, raw.year, raw.GetNumber(0), 1))
		{
			result.flags |= ParseFlags.HaveDate;
			return true;
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static bool GetDayOfYM(ref DateTimeResult result, ref DateTimeRawInfo raw)
	{
		if ((result.flags & ParseFlags.HaveDate) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (SetDateYMD(ref result, raw.year, raw.month, 1))
		{
			result.flags |= ParseFlags.HaveDate;
			return true;
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static void AdjustTimeMark(DateTimeFormatInfo dtfi, ref DateTimeRawInfo raw)
	{
		if (raw.timeMark == TM.NotSet && dtfi.AMDesignator != null && dtfi.PMDesignator != null)
		{
			if (dtfi.AMDesignator.Length == 0 && dtfi.PMDesignator.Length != 0)
			{
				raw.timeMark = TM.AM;
			}
			if (dtfi.PMDesignator.Length == 0 && dtfi.AMDesignator.Length != 0)
			{
				raw.timeMark = TM.PM;
			}
		}
	}

	private static bool AdjustHour(ref int hour, TM timeMark)
	{
		switch (timeMark)
		{
		case TM.AM:
			if (hour < 0 || hour > 12)
			{
				return false;
			}
			hour = ((hour != 12) ? hour : 0);
			break;
		default:
			if (hour < 0 || hour > 23)
			{
				return false;
			}
			if (hour < 12)
			{
				hour += 12;
			}
			break;
		case TM.NotSet:
			break;
		}
		return true;
	}

	private static bool GetTimeOfN(ref DateTimeResult result, ref DateTimeRawInfo raw)
	{
		if ((result.flags & ParseFlags.HaveTime) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (raw.timeMark == TM.NotSet)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		result.Hour = raw.GetNumber(0);
		result.flags |= ParseFlags.HaveTime;
		return true;
	}

	private static bool GetTimeOfNN(ref DateTimeResult result, ref DateTimeRawInfo raw)
	{
		if ((result.flags & ParseFlags.HaveTime) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		result.Hour = raw.GetNumber(0);
		result.Minute = raw.GetNumber(1);
		result.flags |= ParseFlags.HaveTime;
		return true;
	}

	private static bool GetTimeOfNNN(ref DateTimeResult result, ref DateTimeRawInfo raw)
	{
		if ((result.flags & ParseFlags.HaveTime) != 0)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		result.Hour = raw.GetNumber(0);
		result.Minute = raw.GetNumber(1);
		result.Second = raw.GetNumber(2);
		result.flags |= ParseFlags.HaveTime;
		return true;
	}

	private static bool GetDateOfDSN(ref DateTimeResult result, ref DateTimeRawInfo raw)
	{
		if (raw.numCount != 1 || result.Day != -1)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		result.Day = raw.GetNumber(0);
		return true;
	}

	private static bool GetDateOfNDS(ref DateTimeResult result, ref DateTimeRawInfo raw)
	{
		if (result.Month == -1)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (result.Year != -1)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (!TryAdjustYear(ref result, raw.GetNumber(0), out result.Year))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		result.Day = 1;
		return true;
	}

	private static bool GetDateOfNNDS(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		if ((result.flags & ParseFlags.HaveYear) != 0)
		{
			if ((result.flags & ParseFlags.HaveMonth) == 0 && (result.flags & ParseFlags.HaveDay) == 0 && TryAdjustYear(ref result, raw.year, out result.Year) && SetDateYMD(ref result, result.Year, raw.GetNumber(0), raw.GetNumber(1)))
			{
				return true;
			}
		}
		else if ((result.flags & ParseFlags.HaveMonth) != 0 && (result.flags & ParseFlags.HaveYear) == 0 && (result.flags & ParseFlags.HaveDay) == 0)
		{
			if (!GetYearMonthDayOrder(dtfi.ShortDatePattern, out var order))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
				return false;
			}
			int adjustedYear;
			if (order == 0)
			{
				if (TryAdjustYear(ref result, raw.GetNumber(0), out adjustedYear) && SetDateYMD(ref result, adjustedYear, result.Month, raw.GetNumber(1)))
				{
					return true;
				}
			}
			else if (TryAdjustYear(ref result, raw.GetNumber(1), out adjustedYear) && SetDateYMD(ref result, adjustedYear, result.Month, raw.GetNumber(0)))
			{
				return true;
			}
		}
		result.SetBadDateTimeFailure();
		return false;
	}

	private static bool ProcessDateTimeSuffix(ref DateTimeResult result, ref DateTimeRawInfo raw, ref DateTimeToken dtok)
	{
		switch (dtok.suffix)
		{
		case TokenType.SEP_YearSuff:
			if ((result.flags & ParseFlags.HaveYear) != 0)
			{
				return false;
			}
			result.flags |= ParseFlags.HaveYear;
			result.Year = (raw.year = dtok.num);
			break;
		case TokenType.SEP_MonthSuff:
			if ((result.flags & ParseFlags.HaveMonth) != 0)
			{
				return false;
			}
			result.flags |= ParseFlags.HaveMonth;
			result.Month = (raw.month = dtok.num);
			break;
		case TokenType.SEP_DaySuff:
			if ((result.flags & ParseFlags.HaveDay) != 0)
			{
				return false;
			}
			result.flags |= ParseFlags.HaveDay;
			result.Day = dtok.num;
			break;
		case TokenType.SEP_HourSuff:
			if ((result.flags & ParseFlags.HaveHour) != 0)
			{
				return false;
			}
			result.flags |= ParseFlags.HaveHour;
			result.Hour = dtok.num;
			break;
		case TokenType.SEP_MinuteSuff:
			if ((result.flags & ParseFlags.HaveMinute) != 0)
			{
				return false;
			}
			result.flags |= ParseFlags.HaveMinute;
			result.Minute = dtok.num;
			break;
		case TokenType.SEP_SecondSuff:
			if ((result.flags & ParseFlags.HaveSecond) != 0)
			{
				return false;
			}
			result.flags |= ParseFlags.HaveSecond;
			result.Second = dtok.num;
			break;
		}
		return true;
	}

	internal static bool ProcessHebrewTerminalState(DS dps, ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		switch (dps)
		{
		case DS.DX_MNN:
			raw.year = raw.GetNumber(1);
			if (!dtfi.YearMonthAdjustment(ref raw.year, ref raw.month, parsedMonthName: true))
			{
				result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar");
				return false;
			}
			if (!GetDayOfMNN(ref result, ref raw, dtfi))
			{
				return false;
			}
			break;
		case DS.DX_YMN:
			if (!dtfi.YearMonthAdjustment(ref raw.year, ref raw.month, parsedMonthName: true))
			{
				result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar");
				return false;
			}
			if (!GetDayOfYMN(ref result, ref raw))
			{
				return false;
			}
			break;
		case DS.DX_NNY:
			if (raw.year < 1000)
			{
				raw.year += 5000;
			}
			if (!GetDayOfNNY(ref result, ref raw, dtfi))
			{
				return false;
			}
			if (!dtfi.YearMonthAdjustment(ref result.Year, ref raw.month, parsedMonthName: true))
			{
				result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar");
				return false;
			}
			break;
		case DS.DX_MN:
		case DS.DX_NM:
			GetDefaultYear(ref result, ref styles);
			if (!dtfi.YearMonthAdjustment(ref result.Year, ref raw.month, parsedMonthName: true))
			{
				result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar");
				return false;
			}
			if (!GetHebrewDayOfNM(ref result, ref raw, dtfi))
			{
				return false;
			}
			break;
		case DS.DX_YM:
			if (!dtfi.YearMonthAdjustment(ref raw.year, ref raw.month, parsedMonthName: true))
			{
				result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar");
				return false;
			}
			if (!GetDayOfYM(ref result, ref raw))
			{
				return false;
			}
			break;
		case DS.TX_N:
			if (!GetTimeOfN(ref result, ref raw))
			{
				return false;
			}
			break;
		case DS.TX_NN:
			if (!GetTimeOfNN(ref result, ref raw))
			{
				return false;
			}
			break;
		case DS.TX_NNN:
			if (!GetTimeOfNNN(ref result, ref raw))
			{
				return false;
			}
			break;
		default:
			result.SetBadDateTimeFailure();
			return false;
		}
		if (dps > DS.ERROR)
		{
			raw.numCount = 0;
		}
		return true;
	}

	internal static bool ProcessTerminalState(DS dps, ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
	{
		bool flag = true;
		switch (dps)
		{
		case DS.DX_NN:
			flag = GetDayOfNN(ref result, ref styles, ref raw, dtfi);
			break;
		case DS.DX_NNN:
			flag = GetDayOfNNN(ref result, ref raw, dtfi);
			break;
		case DS.DX_MN:
			flag = GetDayOfMN(ref result, ref styles, ref raw, dtfi);
			break;
		case DS.DX_NM:
			flag = GetDayOfNM(ref result, ref styles, ref raw, dtfi);
			break;
		case DS.DX_MNN:
			flag = GetDayOfMNN(ref result, ref raw, dtfi);
			break;
		case DS.DX_DS:
			flag = true;
			break;
		case DS.DX_YNN:
			flag = GetDayOfYNN(ref result, ref raw, dtfi);
			break;
		case DS.DX_NNY:
			flag = GetDayOfNNY(ref result, ref raw, dtfi);
			break;
		case DS.DX_YMN:
			flag = GetDayOfYMN(ref result, ref raw);
			break;
		case DS.DX_YN:
			flag = GetDayOfYN(ref result, ref raw);
			break;
		case DS.DX_YM:
			flag = GetDayOfYM(ref result, ref raw);
			break;
		case DS.TX_N:
			flag = GetTimeOfN(ref result, ref raw);
			break;
		case DS.TX_NN:
			flag = GetTimeOfNN(ref result, ref raw);
			break;
		case DS.TX_NNN:
			flag = GetTimeOfNNN(ref result, ref raw);
			break;
		case DS.TX_TS:
			flag = true;
			break;
		case DS.DX_DSN:
			flag = GetDateOfDSN(ref result, ref raw);
			break;
		case DS.DX_NDS:
			flag = GetDateOfNDS(ref result, ref raw);
			break;
		case DS.DX_NNDS:
			flag = GetDateOfNNDS(ref result, ref raw, dtfi);
			break;
		}
		if (!flag)
		{
			return false;
		}
		if (dps > DS.ERROR)
		{
			raw.numCount = 0;
		}
		return true;
	}

	internal static DateTime Parse(ReadOnlySpan<char> s, DateTimeFormatInfo dtfi, DateTimeStyles styles)
	{
		DateTimeResult result = default(DateTimeResult);
		result.Init(s);
		if (TryParse(s, dtfi, styles, ref result))
		{
			return result.parsedDate;
		}
		throw GetDateTimeParseException(ref result);
	}

	internal static DateTime Parse(ReadOnlySpan<char> s, DateTimeFormatInfo dtfi, DateTimeStyles styles, out TimeSpan offset)
	{
		DateTimeResult result = default(DateTimeResult);
		result.Init(s);
		result.flags |= ParseFlags.CaptureOffset;
		if (TryParse(s, dtfi, styles, ref result))
		{
			offset = result.timeZoneOffset;
			return result.parsedDate;
		}
		throw GetDateTimeParseException(ref result);
	}

	internal static bool TryParse(ReadOnlySpan<char> s, DateTimeFormatInfo dtfi, DateTimeStyles styles, out DateTime result)
	{
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		if (TryParse(s, dtfi, styles, ref result2))
		{
			result = result2.parsedDate;
			return true;
		}
		result = DateTime.MinValue;
		return false;
	}

	internal static bool TryParse(ReadOnlySpan<char> s, DateTimeFormatInfo dtfi, DateTimeStyles styles, out DateTime result, out TimeSpan offset)
	{
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		result2.flags |= ParseFlags.CaptureOffset;
		if (TryParse(s, dtfi, styles, ref result2))
		{
			result = result2.parsedDate;
			offset = result2.timeZoneOffset;
			return true;
		}
		result = DateTime.MinValue;
		offset = TimeSpan.Zero;
		return false;
	}

	internal unsafe static bool TryParse(ReadOnlySpan<char> s, DateTimeFormatInfo dtfi, DateTimeStyles styles, ref DateTimeResult result)
	{
		if (s.Length == 0)
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDateTime");
			return false;
		}
		DS dS = DS.BEGIN;
		bool flag = false;
		DateTimeToken dtok = default(DateTimeToken);
		dtok.suffix = TokenType.SEP_Unk;
		DateTimeRawInfo raw = default(DateTimeRawInfo);
		int* numberBuffer = stackalloc int[3];
		raw.Init(numberBuffer);
		raw.hasSameDateAndTimeSeparators = dtfi.DateSeparator.Equals(dtfi.TimeSeparator, StringComparison.Ordinal);
		result.calendar = dtfi.Calendar;
		result.era = 0;
		__DTString str = new __DTString(s, dtfi);
		str.GetNext();
		do
		{
			if (!Lex(dS, ref str, ref dtok, ref raw, ref result, ref dtfi, styles))
			{
				return false;
			}
			if (dtok.dtt == DTT.Unk)
			{
				continue;
			}
			if (dtok.suffix != TokenType.SEP_Unk)
			{
				if (!ProcessDateTimeSuffix(ref result, ref raw, ref dtok))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				dtok.suffix = TokenType.SEP_Unk;
			}
			if (dtok.dtt == DTT.NumLocalTimeMark)
			{
				if (dS == DS.D_YNd || dS == DS.D_YN)
				{
					return ParseISO8601(ref raw, ref str, styles, ref result);
				}
				result.SetBadDateTimeFailure();
				return false;
			}
			if (raw.hasSameDateAndTimeSeparators)
			{
				if (dtok.dtt == DTT.YearEnd || dtok.dtt == DTT.YearSpace || dtok.dtt == DTT.YearDateSep)
				{
					if (dS == DS.T_Nt)
					{
						dS = DS.D_Nd;
					}
					if (dS == DS.T_NNt)
					{
						dS = DS.D_NNd;
					}
				}
				bool flag2 = str.AtEnd();
				if (s_dateParsingStates[(int)dS][(int)dtok.dtt] == DS.ERROR || flag2)
				{
					switch (dtok.dtt)
					{
					case DTT.YearDateSep:
						dtok.dtt = (flag2 ? DTT.YearEnd : DTT.YearSpace);
						break;
					case DTT.NumDatesep:
						dtok.dtt = (flag2 ? DTT.NumEnd : DTT.NumSpace);
						break;
					case DTT.NumTimesep:
						dtok.dtt = (flag2 ? DTT.NumEnd : DTT.NumSpace);
						break;
					case DTT.MonthDatesep:
						dtok.dtt = (flag2 ? DTT.MonthEnd : DTT.MonthSpace);
						break;
					}
				}
			}
			dS = s_dateParsingStates[(int)dS][(int)dtok.dtt];
			if (dS == DS.ERROR)
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (dS <= DS.ERROR)
			{
				continue;
			}
			if ((dtfi.FormatFlags & DateTimeFormatFlags.UseHebrewRule) != 0)
			{
				if (!ProcessHebrewTerminalState(dS, ref result, ref styles, ref raw, dtfi))
				{
					return false;
				}
			}
			else if (!ProcessTerminalState(dS, ref result, ref styles, ref raw, dtfi))
			{
				return false;
			}
			flag = true;
			dS = DS.BEGIN;
		}
		while (dtok.dtt != 0 && dtok.dtt != DTT.NumEnd && dtok.dtt != DTT.MonthEnd);
		if (!flag)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		AdjustTimeMark(dtfi, ref raw);
		if (!AdjustHour(ref result.Hour, raw.timeMark))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		bool bTimeOnly = result.Year == -1 && result.Month == -1 && result.Day == -1;
		if (!CheckDefaultDateTime(ref result, ref result.calendar, styles))
		{
			return false;
		}
		if (!result.calendar.TryToDateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, 0, result.era, out var result2))
		{
			result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar");
			return false;
		}
		if (raw.fraction > 0.0 && !result2.TryAddTicks((long)Math.Round(raw.fraction * 10000000.0), out result2))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (raw.dayOfWeek != -1 && raw.dayOfWeek != (int)result.calendar.GetDayOfWeek(result2))
		{
			result.SetFailure(ParseFailureKind.FormatWithOriginalDateTime, "Format_BadDayOfWeek");
			return false;
		}
		result.parsedDate = result2;
		if (!DetermineTimeZoneAdjustments(ref result, styles, bTimeOnly))
		{
			return false;
		}
		return true;
	}

	private static bool DetermineTimeZoneAdjustments(ref DateTimeResult result, DateTimeStyles styles, bool bTimeOnly)
	{
		if ((result.flags & ParseFlags.CaptureOffset) != 0)
		{
			return DateTimeOffsetTimeZonePostProcessing(ref result, styles);
		}
		long ticks = result.timeZoneOffset.Ticks;
		if (ticks < -504000000000L || ticks > 504000000000L)
		{
			result.SetFailure(ParseFailureKind.FormatWithOriginalDateTime, "Format_OffsetOutOfRange");
			return false;
		}
		if ((result.flags & ParseFlags.TimeZoneUsed) == 0)
		{
			if ((styles & DateTimeStyles.AssumeLocal) != 0)
			{
				if ((styles & DateTimeStyles.AdjustToUniversal) == 0)
				{
					result.parsedDate = DateTime.SpecifyKind(result.parsedDate, DateTimeKind.Local);
					return true;
				}
				result.flags |= ParseFlags.TimeZoneUsed;
				result.timeZoneOffset = TimeZoneInfo.GetLocalUtcOffset(result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime);
			}
			else
			{
				if ((styles & DateTimeStyles.AssumeUniversal) == 0)
				{
					return true;
				}
				if ((styles & DateTimeStyles.AdjustToUniversal) != 0)
				{
					result.parsedDate = DateTime.SpecifyKind(result.parsedDate, DateTimeKind.Utc);
					return true;
				}
				result.flags |= ParseFlags.TimeZoneUsed;
				result.timeZoneOffset = TimeSpan.Zero;
			}
		}
		if ((styles & DateTimeStyles.RoundtripKind) != 0 && (result.flags & ParseFlags.TimeZoneUtc) != 0)
		{
			result.parsedDate = DateTime.SpecifyKind(result.parsedDate, DateTimeKind.Utc);
			return true;
		}
		if ((styles & DateTimeStyles.AdjustToUniversal) != 0)
		{
			return AdjustTimeZoneToUniversal(ref result);
		}
		return AdjustTimeZoneToLocal(ref result, bTimeOnly);
	}

	private static bool DateTimeOffsetTimeZonePostProcessing(ref DateTimeResult result, DateTimeStyles styles)
	{
		if ((result.flags & ParseFlags.TimeZoneUsed) == 0)
		{
			if ((styles & DateTimeStyles.AssumeUniversal) != 0)
			{
				result.timeZoneOffset = TimeSpan.Zero;
			}
			else
			{
				result.timeZoneOffset = TimeZoneInfo.GetLocalUtcOffset(result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime);
			}
		}
		long ticks = result.timeZoneOffset.Ticks;
		long num = result.parsedDate.Ticks - ticks;
		if (num < 0 || num > 3155378975999999999L)
		{
			result.SetFailure(ParseFailureKind.FormatWithOriginalDateTime, "Format_UTCOutOfRange");
			return false;
		}
		if (ticks < -504000000000L || ticks > 504000000000L)
		{
			result.SetFailure(ParseFailureKind.FormatWithOriginalDateTime, "Format_OffsetOutOfRange");
			return false;
		}
		if ((styles & DateTimeStyles.AdjustToUniversal) != 0)
		{
			if ((result.flags & ParseFlags.TimeZoneUsed) == 0 && (styles & DateTimeStyles.AssumeUniversal) == 0)
			{
				bool result2 = AdjustTimeZoneToUniversal(ref result);
				result.timeZoneOffset = TimeSpan.Zero;
				return result2;
			}
			result.parsedDate = new DateTime(num, DateTimeKind.Utc);
			result.timeZoneOffset = TimeSpan.Zero;
		}
		return true;
	}

	private static bool AdjustTimeZoneToUniversal(ref DateTimeResult result)
	{
		long ticks = result.parsedDate.Ticks;
		ticks -= result.timeZoneOffset.Ticks;
		if (ticks < 0)
		{
			ticks += 864000000000L;
		}
		if (ticks < 0 || ticks > 3155378975999999999L)
		{
			result.SetFailure(ParseFailureKind.FormatWithOriginalDateTime, "Format_DateOutOfRange");
			return false;
		}
		result.parsedDate = new DateTime(ticks, DateTimeKind.Utc);
		return true;
	}

	private static bool AdjustTimeZoneToLocal(ref DateTimeResult result, bool bTimeOnly)
	{
		long ticks = result.parsedDate.Ticks;
		TimeZoneInfo local = TimeZoneInfo.Local;
		bool isAmbiguousLocalDst = false;
		if (ticks < 864000000000L)
		{
			ticks -= result.timeZoneOffset.Ticks;
			ticks += local.GetUtcOffset(bTimeOnly ? DateTime.Now : result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
			if (ticks < 0)
			{
				ticks += 864000000000L;
			}
		}
		else
		{
			ticks -= result.timeZoneOffset.Ticks;
			if (ticks < 0 || ticks > 3155378975999999999L)
			{
				ticks += local.GetUtcOffset(result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
			}
			else
			{
				DateTime time = new DateTime(ticks, DateTimeKind.Utc);
				ticks += TimeZoneInfo.GetUtcOffsetFromUtc(time, TimeZoneInfo.Local, out var _, out isAmbiguousLocalDst).Ticks;
			}
		}
		if (ticks < 0 || ticks > 3155378975999999999L)
		{
			result.parsedDate = DateTime.MinValue;
			result.SetFailure(ParseFailureKind.FormatWithOriginalDateTime, "Format_DateOutOfRange");
			return false;
		}
		result.parsedDate = new DateTime(ticks, DateTimeKind.Local, isAmbiguousLocalDst);
		return true;
	}

	private static bool ParseISO8601(ref DateTimeRawInfo raw, ref __DTString str, DateTimeStyles styles, ref DateTimeResult result)
	{
		if (raw.year >= 0 && raw.GetNumber(0) >= 0)
		{
			raw.GetNumber(1);
			_ = 0;
		}
		str.Index--;
		int result2 = 0;
		double result3 = 0.0;
		str.SkipWhiteSpaces();
		if (!ParseDigits(ref str, 2, out var result4))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		str.SkipWhiteSpaces();
		if (!str.Match(':'))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		str.SkipWhiteSpaces();
		if (!ParseDigits(ref str, 2, out var result5))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		str.SkipWhiteSpaces();
		if (str.Match(':'))
		{
			str.SkipWhiteSpaces();
			if (!ParseDigits(ref str, 2, out result2))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (str.Match('.'))
			{
				if (!ParseFraction(ref str, out result3))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				str.Index--;
			}
			str.SkipWhiteSpaces();
		}
		if (str.GetNext())
		{
			switch (str.GetChar())
			{
			case '+':
			case '-':
				result.flags |= ParseFlags.TimeZoneUsed;
				if (!ParseTimeZone(ref str, ref result.timeZoneOffset))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				str.Index--;
				break;
			case 'Z':
			case 'z':
				result.flags |= ParseFlags.TimeZoneUsed;
				result.timeZoneOffset = TimeSpan.Zero;
				result.flags |= ParseFlags.TimeZoneUtc;
				break;
			default:
				str.Index--;
				break;
			}
			str.SkipWhiteSpaces();
			if (str.Match('#'))
			{
				if (!VerifyValidPunctuation(ref str))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				str.SkipWhiteSpaces();
			}
			if (str.Match('\0') && !VerifyValidPunctuation(ref str))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (str.GetNext())
			{
				result.SetBadDateTimeFailure();
				return false;
			}
		}
		Calendar defaultInstance = GregorianCalendar.GetDefaultInstance();
		if (!defaultInstance.TryToDateTime(raw.year, raw.GetNumber(0), raw.GetNumber(1), result4, result5, result2, 0, result.era, out var result6))
		{
			result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar");
			return false;
		}
		if (!result6.TryAddTicks((long)Math.Round(result3 * 10000000.0), out result6))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		result.parsedDate = result6;
		return DetermineTimeZoneAdjustments(ref result, styles, bTimeOnly: false);
	}

	internal static bool MatchHebrewDigits(ref __DTString str, int digitLen, out int number)
	{
		number = 0;
		HebrewNumberParsingContext context = new HebrewNumberParsingContext(0);
		HebrewNumberParsingState hebrewNumberParsingState = HebrewNumberParsingState.ContinueParsing;
		while (hebrewNumberParsingState == HebrewNumberParsingState.ContinueParsing && str.GetNext())
		{
			hebrewNumberParsingState = HebrewNumber.ParseByChar(str.GetChar(), ref context);
		}
		if (hebrewNumberParsingState == HebrewNumberParsingState.FoundEndOfHebrewNumber)
		{
			number = context.result;
			return true;
		}
		return false;
	}

	internal static bool ParseDigits(ref __DTString str, int digitLen, out int result)
	{
		if (digitLen == 1)
		{
			return ParseDigits(ref str, 1, 2, out result);
		}
		return ParseDigits(ref str, digitLen, digitLen, out result);
	}

	internal static bool ParseDigits(ref __DTString str, int minDigitLen, int maxDigitLen, out int result)
	{
		int num = 0;
		int index = str.Index;
		int i;
		for (i = 0; i < maxDigitLen; i++)
		{
			if (!str.GetNextDigit())
			{
				str.Index--;
				break;
			}
			num = num * 10 + str.GetDigit();
		}
		result = num;
		if (i < minDigitLen)
		{
			str.Index = index;
			return false;
		}
		return true;
	}

	private static bool ParseFractionExact(ref __DTString str, int maxDigitLen, ref double result)
	{
		if (!str.GetNextDigit())
		{
			str.Index--;
			return false;
		}
		result = str.GetDigit();
		int i;
		for (i = 1; i < maxDigitLen; i++)
		{
			if (!str.GetNextDigit())
			{
				str.Index--;
				break;
			}
			result = result * 10.0 + (double)str.GetDigit();
		}
		result /= TimeSpanParse.Pow10(i);
		return i == maxDigitLen;
	}

	private static bool ParseSign(ref __DTString str, ref bool result)
	{
		if (!str.GetNext())
		{
			return false;
		}
		switch (str.GetChar())
		{
		case '+':
			result = true;
			return true;
		case '-':
			result = false;
			return true;
		default:
			return false;
		}
	}

	private static bool ParseTimeZoneOffset(ref __DTString str, int len, ref TimeSpan result)
	{
		bool result2 = true;
		int result3 = 0;
		int result4;
		if ((uint)(len - 1) <= 1u)
		{
			if (!ParseSign(ref str, ref result2))
			{
				return false;
			}
			if (!ParseDigits(ref str, len, out result4))
			{
				return false;
			}
		}
		else
		{
			if (!ParseSign(ref str, ref result2))
			{
				return false;
			}
			if (!ParseDigits(ref str, 1, out result4))
			{
				return false;
			}
			if (str.Match(":"))
			{
				if (!ParseDigits(ref str, 2, out result3))
				{
					return false;
				}
			}
			else
			{
				str.Index--;
				if (!ParseDigits(ref str, 2, out result3))
				{
					return false;
				}
			}
		}
		if (result3 < 0 || result3 >= 60)
		{
			return false;
		}
		result = new TimeSpan(result4, result3, 0);
		if (!result2)
		{
			result = result.Negate();
		}
		return true;
	}

	private static bool MatchAbbreviatedMonthName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
	{
		int maxMatchStrLen = 0;
		result = -1;
		if (str.GetNext())
		{
			int num = ((dtfi.GetMonthName(13).Length == 0) ? 12 : 13);
			for (int i = 1; i <= num; i++)
			{
				string abbreviatedMonthName = dtfi.GetAbbreviatedMonthName(i);
				int matchLength = abbreviatedMonthName.Length;
				if ((dtfi.HasSpacesInMonthNames ? str.MatchSpecifiedWords(abbreviatedMonthName, checkWordBoundary: false, ref matchLength) : str.MatchSpecifiedWord(abbreviatedMonthName)) && matchLength > maxMatchStrLen)
				{
					maxMatchStrLen = matchLength;
					result = i;
				}
			}
			if ((dtfi.FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0)
			{
				int num2 = str.MatchLongestWords(dtfi.AbbreviatedMonthGenitiveNames, ref maxMatchStrLen);
				if (num2 >= 0)
				{
					result = num2 + 1;
				}
			}
			if ((dtfi.FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != 0)
			{
				int num3 = str.MatchLongestWords(dtfi.InternalGetLeapYearMonthNames(), ref maxMatchStrLen);
				if (num3 >= 0)
				{
					result = num3 + 1;
				}
			}
		}
		if (result > 0)
		{
			str.Index += maxMatchStrLen - 1;
			return true;
		}
		return false;
	}

	private static bool MatchMonthName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
	{
		int maxMatchStrLen = 0;
		result = -1;
		if (str.GetNext())
		{
			int num = ((dtfi.GetMonthName(13).Length == 0) ? 12 : 13);
			for (int i = 1; i <= num; i++)
			{
				string monthName = dtfi.GetMonthName(i);
				int matchLength = monthName.Length;
				if ((dtfi.HasSpacesInMonthNames ? str.MatchSpecifiedWords(monthName, checkWordBoundary: false, ref matchLength) : str.MatchSpecifiedWord(monthName)) && matchLength > maxMatchStrLen)
				{
					maxMatchStrLen = matchLength;
					result = i;
				}
			}
			if ((dtfi.FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0)
			{
				int num2 = str.MatchLongestWords(dtfi.MonthGenitiveNames, ref maxMatchStrLen);
				if (num2 >= 0)
				{
					result = num2 + 1;
				}
			}
			if ((dtfi.FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != 0)
			{
				int num3 = str.MatchLongestWords(dtfi.InternalGetLeapYearMonthNames(), ref maxMatchStrLen);
				if (num3 >= 0)
				{
					result = num3 + 1;
				}
			}
		}
		if (result > 0)
		{
			str.Index += maxMatchStrLen - 1;
			return true;
		}
		return false;
	}

	private static bool MatchAbbreviatedDayName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
	{
		int num = 0;
		result = -1;
		if (str.GetNext())
		{
			for (DayOfWeek dayOfWeek = DayOfWeek.Sunday; dayOfWeek <= DayOfWeek.Saturday; dayOfWeek++)
			{
				string abbreviatedDayName = dtfi.GetAbbreviatedDayName(dayOfWeek);
				int matchLength = abbreviatedDayName.Length;
				if ((dtfi.HasSpacesInDayNames ? str.MatchSpecifiedWords(abbreviatedDayName, checkWordBoundary: false, ref matchLength) : str.MatchSpecifiedWord(abbreviatedDayName)) && matchLength > num)
				{
					num = matchLength;
					result = (int)dayOfWeek;
				}
			}
		}
		if (result >= 0)
		{
			str.Index += num - 1;
			return true;
		}
		return false;
	}

	private static bool MatchDayName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
	{
		int num = 0;
		result = -1;
		if (str.GetNext())
		{
			for (DayOfWeek dayOfWeek = DayOfWeek.Sunday; dayOfWeek <= DayOfWeek.Saturday; dayOfWeek++)
			{
				string dayName = dtfi.GetDayName(dayOfWeek);
				int matchLength = dayName.Length;
				if ((dtfi.HasSpacesInDayNames ? str.MatchSpecifiedWords(dayName, checkWordBoundary: false, ref matchLength) : str.MatchSpecifiedWord(dayName)) && matchLength > num)
				{
					num = matchLength;
					result = (int)dayOfWeek;
				}
			}
		}
		if (result >= 0)
		{
			str.Index += num - 1;
			return true;
		}
		return false;
	}

	private static bool MatchEraName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
	{
		if (str.GetNext())
		{
			int[] eras = dtfi.Calendar.Eras;
			if (eras != null)
			{
				for (int i = 0; i < eras.Length; i++)
				{
					string eraName = dtfi.GetEraName(eras[i]);
					if (str.MatchSpecifiedWord(eraName))
					{
						str.Index += eraName.Length - 1;
						result = eras[i];
						return true;
					}
					eraName = dtfi.GetAbbreviatedEraName(eras[i]);
					if (str.MatchSpecifiedWord(eraName))
					{
						str.Index += eraName.Length - 1;
						result = eras[i];
						return true;
					}
				}
			}
		}
		return false;
	}

	private static bool MatchTimeMark(ref __DTString str, DateTimeFormatInfo dtfi, ref TM result)
	{
		result = TM.NotSet;
		if (dtfi.AMDesignator.Length == 0)
		{
			result = TM.AM;
		}
		if (dtfi.PMDesignator.Length == 0)
		{
			result = TM.PM;
		}
		if (str.GetNext())
		{
			string aMDesignator = dtfi.AMDesignator;
			if (aMDesignator.Length > 0 && str.MatchSpecifiedWord(aMDesignator))
			{
				str.Index += aMDesignator.Length - 1;
				result = TM.AM;
				return true;
			}
			aMDesignator = dtfi.PMDesignator;
			if (aMDesignator.Length > 0 && str.MatchSpecifiedWord(aMDesignator))
			{
				str.Index += aMDesignator.Length - 1;
				result = TM.PM;
				return true;
			}
			str.Index--;
		}
		if (result != TM.NotSet)
		{
			return true;
		}
		return false;
	}

	private static bool MatchAbbreviatedTimeMark(ref __DTString str, DateTimeFormatInfo dtfi, ref TM result)
	{
		if (str.GetNext())
		{
			string aMDesignator = dtfi.AMDesignator;
			if (aMDesignator.Length > 0 && str.GetChar() == aMDesignator[0])
			{
				result = TM.AM;
				return true;
			}
			string pMDesignator = dtfi.PMDesignator;
			if (pMDesignator.Length > 0 && str.GetChar() == pMDesignator[0])
			{
				result = TM.PM;
				return true;
			}
		}
		return false;
	}

	private static bool CheckNewValue(ref int currentValue, int newValue, char patternChar, ref DateTimeResult result)
	{
		if (currentValue == -1)
		{
			currentValue = newValue;
			return true;
		}
		if (newValue != currentValue)
		{
			result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", patternChar);
			return false;
		}
		return true;
	}

	private static DateTime GetDateTimeNow(ref DateTimeResult result, ref DateTimeStyles styles)
	{
		if ((result.flags & ParseFlags.CaptureOffset) != 0)
		{
			if ((result.flags & ParseFlags.TimeZoneUsed) != 0)
			{
				return new DateTime(DateTime.UtcNow.Ticks + result.timeZoneOffset.Ticks, DateTimeKind.Unspecified);
			}
			if ((styles & DateTimeStyles.AssumeUniversal) != 0)
			{
				return DateTime.UtcNow;
			}
		}
		return DateTime.Now;
	}

	private static bool CheckDefaultDateTime(ref DateTimeResult result, ref Calendar cal, DateTimeStyles styles)
	{
		if ((result.flags & ParseFlags.CaptureOffset) != 0 && (result.Month != -1 || result.Day != -1) && (result.Year == -1 || (result.flags & ParseFlags.YearDefault) != 0) && (result.flags & ParseFlags.TimeZoneUsed) != 0)
		{
			result.SetFailure(ParseFailureKind.FormatWithOriginalDateTime, "Format_MissingIncompleteDate");
			return false;
		}
		if (result.Year == -1 || result.Month == -1 || result.Day == -1)
		{
			DateTime dateTimeNow = GetDateTimeNow(ref result, ref styles);
			if (result.Month == -1 && result.Day == -1)
			{
				if (result.Year == -1)
				{
					if ((styles & DateTimeStyles.NoCurrentDateDefault) != 0)
					{
						cal = GregorianCalendar.GetDefaultInstance();
						result.Year = (result.Month = (result.Day = 1));
					}
					else
					{
						result.Year = cal.GetYear(dateTimeNow);
						result.Month = cal.GetMonth(dateTimeNow);
						result.Day = cal.GetDayOfMonth(dateTimeNow);
					}
				}
				else
				{
					result.Month = 1;
					result.Day = 1;
				}
			}
			else
			{
				if (result.Year == -1)
				{
					result.Year = cal.GetYear(dateTimeNow);
				}
				if (result.Month == -1)
				{
					result.Month = 1;
				}
				if (result.Day == -1)
				{
					result.Day = 1;
				}
			}
		}
		if (result.Hour == -1)
		{
			result.Hour = 0;
		}
		if (result.Minute == -1)
		{
			result.Minute = 0;
		}
		if (result.Second == -1)
		{
			result.Second = 0;
		}
		if (result.era == -1)
		{
			result.era = 0;
		}
		return true;
	}

	private static string ExpandPredefinedFormat(ReadOnlySpan<char> format, ref DateTimeFormatInfo dtfi, ref ParsingInfo parseInfo, ref DateTimeResult result)
	{
		switch (format[0])
		{
		case 'O':
		case 'o':
		case 's':
			ConfigureFormatOS(ref dtfi, ref parseInfo);
			break;
		case 'R':
		case 'r':
			ConfigureFormatR(ref dtfi, ref parseInfo, ref result);
			break;
		case 'u':
			parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
			dtfi = DateTimeFormatInfo.InvariantInfo;
			if ((result.flags & ParseFlags.CaptureOffset) != 0)
			{
				result.flags |= ParseFlags.UtcSortPattern;
			}
			break;
		case 'U':
			parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
			result.flags |= ParseFlags.TimeZoneUsed;
			result.timeZoneOffset = new TimeSpan(0L);
			result.flags |= ParseFlags.TimeZoneUtc;
			if (dtfi.Calendar.GetType() != typeof(GregorianCalendar))
			{
				dtfi = (DateTimeFormatInfo)dtfi.Clone();
				dtfi.Calendar = GregorianCalendar.GetDefaultInstance();
			}
			break;
		}
		return DateTimeFormat.GetRealFormat(format, dtfi);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ParseJapaneseEraStart(ref __DTString str, DateTimeFormatInfo dtfi)
	{
		if (LocalAppContextSwitches.EnforceLegacyJapaneseDateParsing || dtfi.Calendar.ID != CalendarId.JAPAN || !str.GetNext())
		{
			return false;
		}
		if (str.m_current != "元"[0])
		{
			str.Index--;
			return false;
		}
		return true;
	}

	private static void ConfigureFormatR(ref DateTimeFormatInfo dtfi, ref ParsingInfo parseInfo, ref DateTimeResult result)
	{
		parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
		dtfi = DateTimeFormatInfo.InvariantInfo;
		if ((result.flags & ParseFlags.CaptureOffset) != 0)
		{
			result.flags |= ParseFlags.Rfc1123Pattern;
		}
	}

	private static void ConfigureFormatOS(ref DateTimeFormatInfo dtfi, ref ParsingInfo parseInfo)
	{
		parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
		dtfi = DateTimeFormatInfo.InvariantInfo;
	}

	private static bool ParseByFormat(ref __DTString str, ref __DTString format, ref ParsingInfo parseInfo, DateTimeFormatInfo dtfi, ref DateTimeResult result)
	{
		int result2 = 0;
		int result3 = 0;
		int result4 = 0;
		int result5 = 0;
		int result6 = 0;
		int result7 = 0;
		int result8 = 0;
		double result9 = 0.0;
		TM result10 = TM.AM;
		char @char = format.GetChar();
		switch (@char)
		{
		case 'y':
		{
			int repeatCount = format.GetRepeatCount();
			bool flag;
			if (ParseJapaneseEraStart(ref str, dtfi))
			{
				result2 = 1;
				flag = true;
			}
			else if (dtfi.HasForceTwoDigitYears)
			{
				flag = ParseDigits(ref str, 1, 4, out result2);
			}
			else
			{
				if (repeatCount <= 2)
				{
					parseInfo.fUseTwoDigitYear = true;
				}
				flag = ParseDigits(ref str, repeatCount, out result2);
			}
			if (!flag && parseInfo.fCustomNumberParser)
			{
				flag = parseInfo.parseNumberDelegate(ref str, repeatCount, out result2);
			}
			if (!flag)
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (!CheckNewValue(ref result.Year, result2, @char, ref result))
			{
				return false;
			}
			break;
		}
		case 'M':
		{
			int repeatCount = format.GetRepeatCount();
			if (repeatCount <= 2)
			{
				if (!ParseDigits(ref str, repeatCount, out result3) && (!parseInfo.fCustomNumberParser || !parseInfo.parseNumberDelegate(ref str, repeatCount, out result3)))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
			}
			else
			{
				if (repeatCount == 3)
				{
					if (!MatchAbbreviatedMonthName(ref str, dtfi, ref result3))
					{
						result.SetBadDateTimeFailure();
						return false;
					}
				}
				else if (!MatchMonthName(ref str, dtfi, ref result3))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				result.flags |= ParseFlags.ParsedMonthName;
			}
			if (!CheckNewValue(ref result.Month, result3, @char, ref result))
			{
				return false;
			}
			break;
		}
		case 'd':
		{
			int repeatCount = format.GetRepeatCount();
			if (repeatCount <= 2)
			{
				if (!ParseDigits(ref str, repeatCount, out result4) && (!parseInfo.fCustomNumberParser || !parseInfo.parseNumberDelegate(ref str, repeatCount, out result4)))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				if (!CheckNewValue(ref result.Day, result4, @char, ref result))
				{
					return false;
				}
				break;
			}
			if (repeatCount == 3)
			{
				if (!MatchAbbreviatedDayName(ref str, dtfi, ref result5))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
			}
			else if (!MatchDayName(ref str, dtfi, ref result5))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (!CheckNewValue(ref parseInfo.dayOfWeek, result5, @char, ref result))
			{
				return false;
			}
			break;
		}
		case 'g':
			format.GetRepeatCount();
			if (!MatchEraName(ref str, dtfi, ref result.era))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			break;
		case 'h':
		{
			parseInfo.fUseHour12 = true;
			int repeatCount = format.GetRepeatCount();
			if (!ParseDigits(ref str, (repeatCount < 2) ? 1 : 2, out result6))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (!CheckNewValue(ref result.Hour, result6, @char, ref result))
			{
				return false;
			}
			break;
		}
		case 'H':
		{
			int repeatCount = format.GetRepeatCount();
			if (!ParseDigits(ref str, (repeatCount < 2) ? 1 : 2, out result6))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (!CheckNewValue(ref result.Hour, result6, @char, ref result))
			{
				return false;
			}
			break;
		}
		case 'm':
		{
			int repeatCount = format.GetRepeatCount();
			if (!ParseDigits(ref str, (repeatCount < 2) ? 1 : 2, out result7))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (!CheckNewValue(ref result.Minute, result7, @char, ref result))
			{
				return false;
			}
			break;
		}
		case 's':
		{
			int repeatCount = format.GetRepeatCount();
			if (!ParseDigits(ref str, (repeatCount < 2) ? 1 : 2, out result8))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (!CheckNewValue(ref result.Second, result8, @char, ref result))
			{
				return false;
			}
			break;
		}
		case 'F':
		case 'f':
		{
			int repeatCount = format.GetRepeatCount();
			if (repeatCount <= 7)
			{
				if (!ParseFractionExact(ref str, repeatCount, ref result9) && @char == 'f')
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				if (result.fraction < 0.0)
				{
					result.fraction = result9;
				}
				else if (result9 != result.fraction)
				{
					result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", @char);
					return false;
				}
				break;
			}
			result.SetBadDateTimeFailure();
			return false;
		}
		case 't':
		{
			int repeatCount = format.GetRepeatCount();
			if (repeatCount == 1)
			{
				if (!MatchAbbreviatedTimeMark(ref str, dtfi, ref result10))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
			}
			else if (!MatchTimeMark(ref str, dtfi, ref result10))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (parseInfo.timeMark == TM.NotSet)
			{
				parseInfo.timeMark = result10;
			}
			else if (parseInfo.timeMark != result10)
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", @char);
				return false;
			}
			break;
		}
		case 'z':
		{
			int repeatCount = format.GetRepeatCount();
			TimeSpan result11 = new TimeSpan(0L);
			if (!ParseTimeZoneOffset(ref str, repeatCount, ref result11))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if ((result.flags & ParseFlags.TimeZoneUsed) != 0 && result11 != result.timeZoneOffset)
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'z');
				return false;
			}
			result.timeZoneOffset = result11;
			result.flags |= ParseFlags.TimeZoneUsed;
			break;
		}
		case 'Z':
			if ((result.flags & ParseFlags.TimeZoneUsed) != 0 && result.timeZoneOffset != TimeSpan.Zero)
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'Z');
				return false;
			}
			result.flags |= ParseFlags.TimeZoneUsed;
			result.timeZoneOffset = new TimeSpan(0L);
			result.flags |= ParseFlags.TimeZoneUtc;
			str.Index++;
			if (!GetTimeZoneName(ref str))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			str.Index--;
			break;
		case 'K':
			if (str.Match('Z'))
			{
				if ((result.flags & ParseFlags.TimeZoneUsed) != 0 && result.timeZoneOffset != TimeSpan.Zero)
				{
					result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'K');
					return false;
				}
				result.flags |= ParseFlags.TimeZoneUsed;
				result.timeZoneOffset = new TimeSpan(0L);
				result.flags |= ParseFlags.TimeZoneUtc;
			}
			else if (str.Match('+') || str.Match('-'))
			{
				str.Index--;
				TimeSpan result12 = new TimeSpan(0L);
				if (!ParseTimeZoneOffset(ref str, 3, ref result12))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				if ((result.flags & ParseFlags.TimeZoneUsed) != 0 && result12 != result.timeZoneOffset)
				{
					result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'K');
					return false;
				}
				result.timeZoneOffset = result12;
				result.flags |= ParseFlags.TimeZoneUsed;
			}
			break;
		case ':':
			if (((dtfi.TimeSeparator.Length > 1 && dtfi.TimeSeparator[0] == ':') || !str.Match(':')) && !str.Match(dtfi.TimeSeparator))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			break;
		case '/':
			if (((dtfi.DateSeparator.Length > 1 && dtfi.DateSeparator[0] == '/') || !str.Match('/')) && !str.Match(dtfi.DateSeparator))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			break;
		case '"':
		case '\'':
		{
			StringBuilder stringBuilder = StringBuilderCache.Acquire();
			if (!TryParseQuoteString(format.Value, format.Index, stringBuilder, out var repeatCount))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadQuote", @char);
				StringBuilderCache.Release(stringBuilder);
				return false;
			}
			format.Index += repeatCount - 1;
			string stringAndRelease = StringBuilderCache.GetStringAndRelease(stringBuilder);
			for (int i = 0; i < stringAndRelease.Length; i++)
			{
				if (stringAndRelease[i] == ' ' && parseInfo.fAllowInnerWhite)
				{
					str.SkipWhiteSpaces();
				}
				else if (!str.Match(stringAndRelease[i]))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
			}
			if ((result.flags & ParseFlags.CaptureOffset) != 0 && (((result.flags & ParseFlags.Rfc1123Pattern) != 0 && stringAndRelease == "GMT") || ((result.flags & ParseFlags.UtcSortPattern) != 0 && stringAndRelease == "Z")))
			{
				result.flags |= ParseFlags.TimeZoneUsed;
				result.timeZoneOffset = TimeSpan.Zero;
			}
			break;
		}
		case '%':
			if (format.Index >= format.Value.Length - 1 || format.Value[format.Index + 1] == '%')
			{
				result.SetBadFormatSpecifierFailure(format.Value);
				return false;
			}
			break;
		case '\\':
			if (format.GetNext())
			{
				if (!str.Match(format.GetChar()))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				break;
			}
			result.SetBadFormatSpecifierFailure(format.Value);
			return false;
		case '.':
			if (!str.Match(@char))
			{
				if (!format.GetNext() || !format.Match('F'))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				format.GetRepeatCount();
			}
			break;
		default:
			if (@char == ' ')
			{
				if (!parseInfo.fAllowInnerWhite && !str.Match(@char))
				{
					if (parseInfo.fAllowTrailingWhite && format.GetNext() && ParseByFormat(ref str, ref format, ref parseInfo, dtfi, ref result))
					{
						return true;
					}
					result.SetBadDateTimeFailure();
					return false;
				}
			}
			else if (format.MatchSpecifiedWord("GMT"))
			{
				format.Index += "GMT".Length - 1;
				result.flags |= ParseFlags.TimeZoneUsed;
				result.timeZoneOffset = TimeSpan.Zero;
				if (!str.Match("GMT"))
				{
					result.SetBadDateTimeFailure();
					return false;
				}
			}
			else if (!str.Match(@char))
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			break;
		}
		return true;
	}

	internal static bool TryParseQuoteString(ReadOnlySpan<char> format, int pos, StringBuilder result, out int returnValue)
	{
		returnValue = 0;
		int length = format.Length;
		int num = pos;
		char c = format[pos++];
		bool flag = false;
		while (pos < length)
		{
			char c2 = format[pos++];
			if (c2 == c)
			{
				flag = true;
				break;
			}
			if (c2 == '\\')
			{
				if (pos >= length)
				{
					return false;
				}
				result.Append(format[pos++]);
			}
			else
			{
				result.Append(c2);
			}
		}
		if (!flag)
		{
			return false;
		}
		returnValue = pos - num;
		return true;
	}

	private static bool DoStrictParse(ReadOnlySpan<char> s, ReadOnlySpan<char> formatParam, DateTimeStyles styles, DateTimeFormatInfo dtfi, ref DateTimeResult result)
	{
		ParsingInfo parseInfo = default(ParsingInfo);
		parseInfo.Init();
		parseInfo.calendar = dtfi.Calendar;
		parseInfo.fAllowInnerWhite = (styles & DateTimeStyles.AllowInnerWhite) != 0;
		parseInfo.fAllowTrailingWhite = (styles & DateTimeStyles.AllowTrailingWhite) != 0;
		if (formatParam.Length == 1)
		{
			char c = formatParam[0];
			if (styles == DateTimeStyles.None)
			{
				switch (c)
				{
				case 'R':
				case 'r':
					ConfigureFormatR(ref dtfi, ref parseInfo, ref result);
					return ParseFormatR(s, ref parseInfo, ref result);
				case 'O':
				case 'o':
					ConfigureFormatOS(ref dtfi, ref parseInfo);
					return ParseFormatO(s, ref result);
				}
			}
			if ((result.flags & ParseFlags.CaptureOffset) != 0 && c == 'U')
			{
				result.SetBadFormatSpecifierFailure(formatParam);
				return false;
			}
			formatParam = ExpandPredefinedFormat(formatParam, ref dtfi, ref parseInfo, ref result);
		}
		result.calendar = parseInfo.calendar;
		if (parseInfo.calendar.ID == CalendarId.HEBREW)
		{
			parseInfo.parseNumberDelegate = s_hebrewNumberParser;
			parseInfo.fCustomNumberParser = true;
		}
		result.Hour = (result.Minute = (result.Second = -1));
		__DTString format = new __DTString(formatParam, dtfi, checkDigitToken: false);
		__DTString str = new __DTString(s, dtfi, checkDigitToken: false);
		if (parseInfo.fAllowTrailingWhite)
		{
			format.TrimTail();
			format.RemoveTrailingInQuoteSpaces();
			str.TrimTail();
		}
		if ((styles & DateTimeStyles.AllowLeadingWhite) != 0)
		{
			format.SkipWhiteSpaces();
			format.RemoveLeadingInQuoteSpaces();
			str.SkipWhiteSpaces();
		}
		while (format.GetNext())
		{
			if (parseInfo.fAllowInnerWhite)
			{
				str.SkipWhiteSpaces();
			}
			if (!ParseByFormat(ref str, ref format, ref parseInfo, dtfi, ref result))
			{
				return false;
			}
		}
		if (str.Index < str.Value.Length - 1)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (parseInfo.fUseTwoDigitYear && (dtfi.FormatFlags & DateTimeFormatFlags.UseHebrewRule) == 0)
		{
			if (result.Year >= 100)
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			try
			{
				result.Year = parseInfo.calendar.ToFourDigitYear(result.Year);
			}
			catch (ArgumentOutOfRangeException)
			{
				result.SetBadDateTimeFailure();
				return false;
			}
		}
		if (parseInfo.fUseHour12)
		{
			if (parseInfo.timeMark == TM.NotSet)
			{
				parseInfo.timeMark = TM.AM;
			}
			if (result.Hour > 12)
			{
				result.SetBadDateTimeFailure();
				return false;
			}
			if (parseInfo.timeMark == TM.AM)
			{
				if (result.Hour == 12)
				{
					result.Hour = 0;
				}
			}
			else
			{
				result.Hour = ((result.Hour == 12) ? 12 : (result.Hour + 12));
			}
		}
		else if ((parseInfo.timeMark == TM.AM && result.Hour >= 12) || (parseInfo.timeMark == TM.PM && result.Hour < 12))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		bool flag = result.Year == -1 && result.Month == -1 && result.Day == -1;
		if (!CheckDefaultDateTime(ref result, ref parseInfo.calendar, styles))
		{
			return false;
		}
		if (!flag && dtfi.HasYearMonthAdjustment && !dtfi.YearMonthAdjustment(ref result.Year, ref result.Month, (result.flags & ParseFlags.ParsedMonthName) != 0))
		{
			result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar");
			return false;
		}
		if (!parseInfo.calendar.TryToDateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, 0, result.era, out result.parsedDate))
		{
			result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar");
			return false;
		}
		if (result.fraction > 0.0 && !result.parsedDate.TryAddTicks((long)Math.Round(result.fraction * 10000000.0), out result.parsedDate))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (parseInfo.dayOfWeek != -1 && parseInfo.dayOfWeek != (int)parseInfo.calendar.GetDayOfWeek(result.parsedDate))
		{
			result.SetFailure(ParseFailureKind.FormatWithOriginalDateTime, "Format_BadDayOfWeek");
			return false;
		}
		return DetermineTimeZoneAdjustments(ref result, styles, flag);
	}

	private static bool ParseFormatR(ReadOnlySpan<char> source, ref ParsingInfo parseInfo, ref DateTimeResult result)
	{
		if (source.Length != 29)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		uint num = source[0];
		uint num2 = source[1];
		uint num3 = source[2];
		uint num4 = source[3];
		if ((num | num2 | num3 | num4) > 127)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		DayOfWeek dayOfWeek;
		switch ((num << 24) | (num2 << 16) | (num3 << 8) | num4 | 0x20202000)
		{
		case 1937075756u:
			dayOfWeek = DayOfWeek.Sunday;
			break;
		case 1836019244u:
			dayOfWeek = DayOfWeek.Monday;
			break;
		case 1953850668u:
			dayOfWeek = DayOfWeek.Tuesday;
			break;
		case 2003133484u:
			dayOfWeek = DayOfWeek.Wednesday;
			break;
		case 1953002796u:
			dayOfWeek = DayOfWeek.Thursday;
			break;
		case 1718774060u:
			dayOfWeek = DayOfWeek.Friday;
			break;
		case 1935766572u:
			dayOfWeek = DayOfWeek.Saturday;
			break;
		default:
			result.SetBadDateTimeFailure();
			return false;
		}
		if (source[4] != ' ')
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		uint num5 = (uint)(source[5] - 48);
		uint num6 = (uint)(source[6] - 48);
		if (num5 > 9 || num6 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int day = (int)(num5 * 10 + num6);
		if (source[7] != ' ')
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		uint num7 = source[8];
		uint num8 = source[9];
		uint num9 = source[10];
		uint num10 = source[11];
		if ((num7 | num8 | num9 | num10) > 127)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int month;
		switch ((num7 << 24) | (num8 << 16) | (num9 << 8) | num10 | 0x20202000)
		{
		case 1784770080u:
			month = 1;
			break;
		case 1717920288u:
			month = 2;
			break;
		case 1835102752u:
			month = 3;
			break;
		case 1634759200u:
			month = 4;
			break;
		case 1835104544u:
			month = 5;
			break;
		case 1786080800u:
			month = 6;
			break;
		case 1786080288u:
			month = 7;
			break;
		case 1635084064u:
			month = 8;
			break;
		case 1936027680u:
			month = 9;
			break;
		case 1868788768u:
			month = 10;
			break;
		case 1852798496u:
			month = 11;
			break;
		case 1684366112u:
			month = 12;
			break;
		default:
			result.SetBadDateTimeFailure();
			return false;
		}
		uint num11 = (uint)(source[12] - 48);
		uint num12 = (uint)(source[13] - 48);
		uint num13 = (uint)(source[14] - 48);
		uint num14 = (uint)(source[15] - 48);
		if (num11 > 9 || num12 > 9 || num13 > 9 || num14 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int year = (int)(num11 * 1000 + num12 * 100 + num13 * 10 + num14);
		if (source[16] != ' ')
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		uint num15 = (uint)(source[17] - 48);
		uint num16 = (uint)(source[18] - 48);
		if (num15 > 9 || num16 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int hour = (int)(num15 * 10 + num16);
		if (source[19] != ':')
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		uint num17 = (uint)(source[20] - 48);
		uint num18 = (uint)(source[21] - 48);
		if (num17 > 9 || num18 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int minute = (int)(num17 * 10 + num18);
		if (source[22] != ':')
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		uint num19 = (uint)(source[23] - 48);
		uint num20 = (uint)(source[24] - 48);
		if (num19 > 9 || num20 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int second = (int)(num19 * 10 + num20);
		if (source[25] != ' ' || source[26] != 'G' || source[27] != 'M' || source[28] != 'T')
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (!parseInfo.calendar.TryToDateTime(year, month, day, hour, minute, second, 0, 0, out result.parsedDate))
		{
			result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar");
			return false;
		}
		if (dayOfWeek != result.parsedDate.DayOfWeek)
		{
			result.SetFailure(ParseFailureKind.FormatWithOriginalDateTime, "Format_BadDayOfWeek");
			return false;
		}
		return true;
	}

	private static bool ParseFormatO(ReadOnlySpan<char> source, ref DateTimeResult result)
	{
		if ((uint)source.Length < 27u || source[4] != '-' || source[7] != '-' || source[10] != 'T' || source[13] != ':' || source[16] != ':' || source[19] != '.')
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		uint num = (uint)(source[0] - 48);
		uint num2 = (uint)(source[1] - 48);
		uint num3 = (uint)(source[2] - 48);
		uint num4 = (uint)(source[3] - 48);
		if (num > 9 || num2 > 9 || num3 > 9 || num4 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int year = (int)(num * 1000 + num2 * 100 + num3 * 10 + num4);
		uint num5 = (uint)(source[5] - 48);
		uint num6 = (uint)(source[6] - 48);
		if (num5 > 9 || num6 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int month = (int)(num5 * 10 + num6);
		uint num7 = (uint)(source[8] - 48);
		uint num8 = (uint)(source[9] - 48);
		if (num7 > 9 || num8 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int day = (int)(num7 * 10 + num8);
		uint num9 = (uint)(source[11] - 48);
		uint num10 = (uint)(source[12] - 48);
		if (num9 > 9 || num10 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int hour = (int)(num9 * 10 + num10);
		uint num11 = (uint)(source[14] - 48);
		uint num12 = (uint)(source[15] - 48);
		if (num11 > 9 || num12 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int minute = (int)(num11 * 10 + num12);
		uint num13 = (uint)(source[17] - 48);
		uint num14 = (uint)(source[18] - 48);
		if (num13 > 9 || num14 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		int second = (int)(num13 * 10 + num14);
		uint num15 = (uint)(source[20] - 48);
		uint num16 = (uint)(source[21] - 48);
		uint num17 = (uint)(source[22] - 48);
		uint num18 = (uint)(source[23] - 48);
		uint num19 = (uint)(source[24] - 48);
		uint num20 = (uint)(source[25] - 48);
		uint num21 = (uint)(source[26] - 48);
		if (num15 > 9 || num16 > 9 || num17 > 9 || num18 > 9 || num19 > 9 || num20 > 9 || num21 > 9)
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		double num22 = (double)(num15 * 1000000 + num16 * 100000 + num17 * 10000 + num18 * 1000 + num19 * 100 + num20 * 10 + num21) / 10000000.0;
		if (!DateTime.TryCreate(year, month, day, hour, minute, second, 0, out var result2))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if (!result2.TryAddTicks((long)Math.Round(num22 * 10000000.0), out result.parsedDate))
		{
			result.SetBadDateTimeFailure();
			return false;
		}
		if ((uint)source.Length > 27u)
		{
			char c = source[27];
			switch (c)
			{
			case 'Z':
				if (source.Length != 28)
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				result.flags |= ParseFlags.TimeZoneUsed | ParseFlags.TimeZoneUtc;
				break;
			case '+':
			case '-':
			{
				int num25;
				int num26;
				if (source.Length == 33)
				{
					uint num23 = (uint)(source[28] - 48);
					uint num24 = (uint)(source[29] - 48);
					if (num23 > 9 || num24 > 9)
					{
						result.SetBadDateTimeFailure();
						return false;
					}
					num25 = (int)(num23 * 10 + num24);
					num26 = 30;
				}
				else
				{
					if (source.Length != 32)
					{
						result.SetBadDateTimeFailure();
						return false;
					}
					num25 = source[28] - 48;
					if ((uint)num25 > 9u)
					{
						result.SetBadDateTimeFailure();
						return false;
					}
					num26 = 29;
				}
				if (source[num26] != ':')
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				uint num27 = (uint)(source[num26 + 1] - 48);
				uint num28 = (uint)(source[num26 + 2] - 48);
				if (num27 > 9 || num28 > 9)
				{
					result.SetBadDateTimeFailure();
					return false;
				}
				int minutes = (int)(num27 * 10 + num28);
				result.flags |= ParseFlags.TimeZoneUsed;
				result.timeZoneOffset = new TimeSpan(num25, minutes, 0);
				if (c == '-')
				{
					result.timeZoneOffset = result.timeZoneOffset.Negate();
				}
				break;
			}
			default:
				result.SetBadDateTimeFailure();
				return false;
			}
		}
		return DetermineTimeZoneAdjustments(ref result, DateTimeStyles.None, bTimeOnly: false);
	}

	private static Exception GetDateTimeParseException(ref DateTimeResult result)
	{
		return result.failure switch
		{
			ParseFailureKind.ArgumentNull => new ArgumentNullException(result.failureArgumentName, SR.GetResourceString(result.failureMessageID)), 
			ParseFailureKind.Format => new FormatException(SR.GetResourceString(result.failureMessageID)), 
			ParseFailureKind.FormatWithParameter => new FormatException(SR.Format(SR.GetResourceString(result.failureMessageID), result.failureMessageFormatArgument)), 
			ParseFailureKind.FormatBadDateTimeCalendar => new FormatException(SR.Format(SR.GetResourceString(result.failureMessageID), new string(result.originalDateTimeString), result.calendar)), 
			ParseFailureKind.FormatWithOriginalDateTime => new FormatException(SR.Format(SR.GetResourceString(result.failureMessageID), new string(result.originalDateTimeString))), 
			ParseFailureKind.FormatWithFormatSpecifier => new FormatException(SR.Format(SR.GetResourceString(result.failureMessageID), new string(result.failedFormatSpecifier))), 
			ParseFailureKind.FormatWithOriginalDateTimeAndParameter => new FormatException(SR.Format(SR.GetResourceString(result.failureMessageID), new string(result.originalDateTimeString), result.failureMessageFormatArgument)), 
			_ => null, 
		};
	}
}
