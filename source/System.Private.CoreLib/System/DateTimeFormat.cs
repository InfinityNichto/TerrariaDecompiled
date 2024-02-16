using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System;

internal static class DateTimeFormat
{
	internal static char[] allStandardFormats = new char[19]
	{
		'd', 'D', 'f', 'F', 'g', 'G', 'm', 'M', 'o', 'O',
		'r', 'R', 's', 't', 'T', 'u', 'U', 'y', 'Y'
	};

	internal static readonly DateTimeFormatInfo InvariantFormatInfo = CultureInfo.InvariantCulture.DateTimeFormat;

	internal static readonly string[] InvariantAbbreviatedMonthNames = InvariantFormatInfo.AbbreviatedMonthNames;

	internal static readonly string[] InvariantAbbreviatedDayNames = InvariantFormatInfo.AbbreviatedDayNames;

	internal static string[] fixedNumberFormats = new string[7] { "0", "00", "000", "0000", "00000", "000000", "0000000" };

	internal static void FormatDigits(StringBuilder outputBuffer, int value, int len)
	{
		FormatDigits(outputBuffer, value, len, overrideLengthLimit: false);
	}

	internal unsafe static void FormatDigits(StringBuilder outputBuffer, int value, int len, bool overrideLengthLimit)
	{
		if (!overrideLengthLimit && len > 2)
		{
			len = 2;
		}
		char* ptr = stackalloc char[16];
		char* ptr2 = ptr + 16;
		int num = value;
		do
		{
			*(--ptr2) = (char)(num % 10 + 48);
			num /= 10;
		}
		while (num != 0 && ptr2 > ptr);
		int i;
		for (i = (int)(ptr + 16 - ptr2); i < len; i++)
		{
			if (ptr2 <= ptr)
			{
				break;
			}
			*(--ptr2) = '0';
		}
		outputBuffer.Append(ptr2, i);
	}

	private static void HebrewFormatDigits(StringBuilder outputBuffer, int digits)
	{
		HebrewNumber.Append(outputBuffer, digits);
	}

	internal static int ParseRepeatPattern(ReadOnlySpan<char> format, int pos, char patternChar)
	{
		int length = format.Length;
		int i;
		for (i = pos + 1; i < length && format[i] == patternChar; i++)
		{
		}
		return i - pos;
	}

	private static string FormatDayOfWeek(int dayOfWeek, int repeat, DateTimeFormatInfo dtfi)
	{
		if (repeat == 3)
		{
			return dtfi.GetAbbreviatedDayName((DayOfWeek)dayOfWeek);
		}
		return dtfi.GetDayName((DayOfWeek)dayOfWeek);
	}

	private static string FormatMonth(int month, int repeatCount, DateTimeFormatInfo dtfi)
	{
		if (repeatCount == 3)
		{
			return dtfi.GetAbbreviatedMonthName(month);
		}
		return dtfi.GetMonthName(month);
	}

	private static string FormatHebrewMonthName(DateTime time, int month, int repeatCount, DateTimeFormatInfo dtfi)
	{
		if (dtfi.Calendar.IsLeapYear(dtfi.Calendar.GetYear(time)))
		{
			return dtfi.InternalGetMonthName(month, MonthNameStyles.LeapYear, repeatCount == 3);
		}
		if (month >= 7)
		{
			month++;
		}
		if (repeatCount == 3)
		{
			return dtfi.GetAbbreviatedMonthName(month);
		}
		return dtfi.GetMonthName(month);
	}

	internal static int ParseQuoteString(ReadOnlySpan<char> format, int pos, StringBuilder result)
	{
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
					throw new FormatException(SR.Format_InvalidString);
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
			throw new FormatException(SR.Format(SR.Format_BadQuote, c));
		}
		return pos - num;
	}

	internal static int ParseNextChar(ReadOnlySpan<char> format, int pos)
	{
		if (pos >= format.Length - 1)
		{
			return -1;
		}
		return format[pos + 1];
	}

	private static bool IsUseGenitiveForm(ReadOnlySpan<char> format, int index, int tokenLen, char patternToMatch)
	{
		int num = 0;
		int num2 = index - 1;
		while (num2 >= 0 && format[num2] != patternToMatch)
		{
			num2--;
		}
		if (num2 >= 0)
		{
			while (--num2 >= 0 && format[num2] == patternToMatch)
			{
				num++;
			}
			if (num <= 1)
			{
				return true;
			}
		}
		for (num2 = index + tokenLen; num2 < format.Length && format[num2] != patternToMatch; num2++)
		{
		}
		if (num2 < format.Length)
		{
			num = 0;
			while (++num2 < format.Length && format[num2] == patternToMatch)
			{
				num++;
			}
			if (num <= 1)
			{
				return true;
			}
		}
		return false;
	}

	private static StringBuilder FormatCustomized(DateTime dateTime, ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, TimeSpan offset, StringBuilder result)
	{
		Calendar calendar = dtfi.Calendar;
		bool flag = false;
		if (result == null)
		{
			flag = true;
			result = StringBuilderCache.Acquire();
		}
		bool flag2 = calendar.ID == CalendarId.HEBREW;
		bool flag3 = calendar.ID == CalendarId.JAPAN;
		bool timeOnly = true;
		int num;
		for (int i = 0; i < format.Length; i += num)
		{
			char c = format[i];
			switch (c)
			{
			case 'g':
				num = ParseRepeatPattern(format, i, c);
				result.Append(dtfi.GetEraName(calendar.GetEra(dateTime)));
				break;
			case 'h':
			{
				num = ParseRepeatPattern(format, i, c);
				int num3 = dateTime.Hour % 12;
				if (num3 == 0)
				{
					num3 = 12;
				}
				FormatDigits(result, num3, num);
				break;
			}
			case 'H':
				num = ParseRepeatPattern(format, i, c);
				FormatDigits(result, dateTime.Hour, num);
				break;
			case 'm':
				num = ParseRepeatPattern(format, i, c);
				FormatDigits(result, dateTime.Minute, num);
				break;
			case 's':
				num = ParseRepeatPattern(format, i, c);
				FormatDigits(result, dateTime.Second, num);
				break;
			case 'F':
			case 'f':
				num = ParseRepeatPattern(format, i, c);
				if (num <= 7)
				{
					long num4 = dateTime.Ticks % 10000000;
					num4 /= (long)Math.Pow(10.0, 7 - num);
					if (c == 'f')
					{
						result.AppendSpanFormattable((int)num4, fixedNumberFormats[num - 1], CultureInfo.InvariantCulture);
						break;
					}
					int num5 = num;
					while (num5 > 0 && num4 % 10 == 0L)
					{
						num4 /= 10;
						num5--;
					}
					if (num5 > 0)
					{
						result.AppendSpanFormattable((int)num4, fixedNumberFormats[num5 - 1], CultureInfo.InvariantCulture);
					}
					else if (result.Length > 0 && result[result.Length - 1] == '.')
					{
						result.Remove(result.Length - 1, 1);
					}
					break;
				}
				if (flag)
				{
					StringBuilderCache.Release(result);
				}
				throw new FormatException(SR.Format_InvalidString);
			case 't':
				num = ParseRepeatPattern(format, i, c);
				if (num == 1)
				{
					if (dateTime.Hour < 12)
					{
						if (dtfi.AMDesignator.Length >= 1)
						{
							result.Append(dtfi.AMDesignator[0]);
						}
					}
					else if (dtfi.PMDesignator.Length >= 1)
					{
						result.Append(dtfi.PMDesignator[0]);
					}
				}
				else
				{
					result.Append((dateTime.Hour < 12) ? dtfi.AMDesignator : dtfi.PMDesignator);
				}
				break;
			case 'd':
				num = ParseRepeatPattern(format, i, c);
				if (num <= 2)
				{
					int dayOfMonth = calendar.GetDayOfMonth(dateTime);
					if (flag2)
					{
						HebrewFormatDigits(result, dayOfMonth);
					}
					else
					{
						FormatDigits(result, dayOfMonth, num);
					}
				}
				else
				{
					int dayOfWeek = (int)calendar.GetDayOfWeek(dateTime);
					result.Append(FormatDayOfWeek(dayOfWeek, num, dtfi));
				}
				timeOnly = false;
				break;
			case 'M':
			{
				num = ParseRepeatPattern(format, i, c);
				int month = calendar.GetMonth(dateTime);
				if (num <= 2)
				{
					if (flag2)
					{
						HebrewFormatDigits(result, month);
					}
					else
					{
						FormatDigits(result, month, num);
					}
				}
				else if (flag2)
				{
					result.Append(FormatHebrewMonthName(dateTime, month, num, dtfi));
				}
				else if ((dtfi.FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0)
				{
					result.Append(dtfi.InternalGetMonthName(month, IsUseGenitiveForm(format, i, num, 'd') ? MonthNameStyles.Genitive : MonthNameStyles.Regular, num == 3));
				}
				else
				{
					result.Append(FormatMonth(month, num, dtfi));
				}
				timeOnly = false;
				break;
			}
			case 'y':
			{
				int year = calendar.GetYear(dateTime);
				num = ParseRepeatPattern(format, i, c);
				if (flag3 && !LocalAppContextSwitches.FormatJapaneseFirstYearAsANumber && year == 1 && ((i + num < format.Length && format[i + num] == '年') || (i + num < format.Length - 1 && format[i + num] == '\'' && format[i + num + 1] == '年')))
				{
					result.Append("元"[0]);
				}
				else if (dtfi.HasForceTwoDigitYears)
				{
					FormatDigits(result, year, (num <= 2) ? num : 2);
				}
				else if (calendar.ID == CalendarId.HEBREW)
				{
					HebrewFormatDigits(result, year);
				}
				else if (num <= 2)
				{
					FormatDigits(result, year % 100, num);
				}
				else if (num <= 16)
				{
					FormatDigits(result, year, num, overrideLengthLimit: true);
				}
				else
				{
					result.Append(year.ToString("D" + num.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));
				}
				timeOnly = false;
				break;
			}
			case 'z':
				num = ParseRepeatPattern(format, i, c);
				FormatCustomizedTimeZone(dateTime, offset, num, timeOnly, result);
				break;
			case 'K':
				num = 1;
				FormatCustomizedRoundripTimeZone(dateTime, offset, result);
				break;
			case ':':
				result.Append(dtfi.TimeSeparator);
				num = 1;
				break;
			case '/':
				result.Append(dtfi.DateSeparator);
				num = 1;
				break;
			case '"':
			case '\'':
				num = ParseQuoteString(format, i, result);
				break;
			case '%':
			{
				int num2 = ParseNextChar(format, i);
				if (num2 >= 0 && num2 != 37)
				{
					char reference = (char)num2;
					StringBuilder stringBuilder = FormatCustomized(dateTime, MemoryMarshal.CreateReadOnlySpan(ref reference, 1), dtfi, offset, result);
					num = 2;
					break;
				}
				if (flag)
				{
					StringBuilderCache.Release(result);
				}
				throw new FormatException(SR.Format_InvalidString);
			}
			case '\\':
			{
				int num2 = ParseNextChar(format, i);
				if (num2 >= 0)
				{
					result.Append((char)num2);
					num = 2;
					break;
				}
				if (flag)
				{
					StringBuilderCache.Release(result);
				}
				throw new FormatException(SR.Format_InvalidString);
			}
			default:
				result.Append(c);
				num = 1;
				break;
			}
		}
		return result;
	}

	private static void FormatCustomizedTimeZone(DateTime dateTime, TimeSpan offset, int tokenLen, bool timeOnly, StringBuilder result)
	{
		if (offset.Ticks == long.MinValue)
		{
			offset = ((timeOnly && dateTime.Ticks < 864000000000L) ? TimeZoneInfo.GetLocalUtcOffset(DateTime.Now, TimeZoneInfoOptions.NoThrowOnInvalidTime) : ((dateTime.Kind != DateTimeKind.Utc) ? TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime) : default(TimeSpan)));
		}
		if (offset.Ticks >= 0)
		{
			result.Append('+');
		}
		else
		{
			result.Append('-');
			offset = offset.Negate();
		}
		StringBuilder stringBuilder;
		IFormatProvider invariantCulture;
		if (tokenLen <= 1)
		{
			stringBuilder = result;
			StringBuilder stringBuilder2 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder, invariantCulture);
			handler.AppendFormatted(offset.Hours, "0");
			stringBuilder2.Append(provider, ref handler);
			return;
		}
		stringBuilder = result;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder, invariantCulture);
		handler2.AppendFormatted(offset.Hours, "00");
		stringBuilder3.Append(provider2, ref handler2);
		if (tokenLen >= 3)
		{
			stringBuilder = result;
			StringBuilder stringBuilder4 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider3 = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler3 = new StringBuilder.AppendInterpolatedStringHandler(1, 1, stringBuilder, invariantCulture);
			handler3.AppendLiteral(":");
			handler3.AppendFormatted(offset.Minutes, "00");
			stringBuilder4.Append(provider3, ref handler3);
		}
	}

	private static void FormatCustomizedRoundripTimeZone(DateTime dateTime, TimeSpan offset, StringBuilder result)
	{
		if (offset.Ticks == long.MinValue)
		{
			switch (dateTime.Kind)
			{
			case DateTimeKind.Local:
				break;
			case DateTimeKind.Utc:
				result.Append('Z');
				return;
			default:
				return;
			}
			offset = TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
		}
		if (offset.Ticks >= 0)
		{
			result.Append('+');
		}
		else
		{
			result.Append('-');
			offset = offset.Negate();
		}
		Append2DigitNumber(result, offset.Hours);
		result.Append(':');
		Append2DigitNumber(result, offset.Minutes);
	}

	private static void Append2DigitNumber(StringBuilder result, int val)
	{
		result.Append((char)(48 + val / 10));
		result.Append((char)(48 + val % 10));
	}

	internal static string GetRealFormat(ReadOnlySpan<char> format, DateTimeFormatInfo dtfi)
	{
		switch (format[0])
		{
		case 'd':
			return dtfi.ShortDatePattern;
		case 'D':
			return dtfi.LongDatePattern;
		case 'f':
			return dtfi.LongDatePattern + " " + dtfi.ShortTimePattern;
		case 'F':
			return dtfi.FullDateTimePattern;
		case 'g':
			return dtfi.GeneralShortTimePattern;
		case 'G':
			return dtfi.GeneralLongTimePattern;
		case 'M':
		case 'm':
			return dtfi.MonthDayPattern;
		case 'O':
		case 'o':
			return "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK";
		case 'R':
		case 'r':
			return dtfi.RFC1123Pattern;
		case 's':
			return dtfi.SortableDateTimePattern;
		case 't':
			return dtfi.ShortTimePattern;
		case 'T':
			return dtfi.LongTimePattern;
		case 'u':
			return dtfi.UniversalSortableDateTimePattern;
		case 'U':
			return dtfi.FullDateTimePattern;
		case 'Y':
		case 'y':
			return dtfi.YearMonthPattern;
		default:
			throw new FormatException(SR.Format_InvalidString);
		}
	}

	private static string ExpandPredefinedFormat(ReadOnlySpan<char> format, ref DateTime dateTime, ref DateTimeFormatInfo dtfi, TimeSpan offset)
	{
		switch (format[0])
		{
		case 'O':
		case 'o':
			dtfi = DateTimeFormatInfo.InvariantInfo;
			break;
		case 'R':
		case 'r':
		case 'u':
			if (offset.Ticks != long.MinValue)
			{
				dateTime -= offset;
			}
			dtfi = DateTimeFormatInfo.InvariantInfo;
			break;
		case 's':
			dtfi = DateTimeFormatInfo.InvariantInfo;
			break;
		case 'U':
			if (offset.Ticks != long.MinValue)
			{
				throw new FormatException(SR.Format_InvalidString);
			}
			dtfi = (DateTimeFormatInfo)dtfi.Clone();
			if (dtfi.Calendar.GetType() != typeof(GregorianCalendar))
			{
				dtfi.Calendar = GregorianCalendar.GetDefaultInstance();
			}
			dateTime = dateTime.ToUniversalTime();
			break;
		}
		return GetRealFormat(format, dtfi);
	}

	internal static string Format(DateTime dateTime, string format, IFormatProvider provider)
	{
		return Format(dateTime, format, provider, new TimeSpan(long.MinValue));
	}

	internal static string Format(DateTime dateTime, string format, IFormatProvider provider, TimeSpan offset)
	{
		if (format != null && format.Length == 1)
		{
			switch (format[0])
			{
			case 'O':
			case 'o':
			{
				Span<char> destination = stackalloc char[33];
				TryFormatO(dateTime, offset, destination, out var charsWritten2);
				return destination.Slice(0, charsWritten2).ToString();
			}
			case 'R':
			case 'r':
			{
				string text = string.FastAllocateString(29);
				TryFormatR(dateTime, offset, new Span<char>(ref text.GetRawStringData(), text.Length), out var _);
				return text;
			}
			}
		}
		DateTimeFormatInfo instance = DateTimeFormatInfo.GetInstance(provider);
		return StringBuilderCache.GetStringAndRelease(FormatStringBuilder(dateTime, format, instance, offset));
	}

	internal static bool TryFormat(DateTime dateTime, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
	{
		return TryFormat(dateTime, destination, out charsWritten, format, provider, new TimeSpan(long.MinValue));
	}

	internal static bool TryFormat(DateTime dateTime, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider, TimeSpan offset)
	{
		if (format.Length == 1)
		{
			switch (format[0])
			{
			case 'O':
			case 'o':
				return TryFormatO(dateTime, offset, destination, out charsWritten);
			case 'R':
			case 'r':
				return TryFormatR(dateTime, offset, destination, out charsWritten);
			}
		}
		DateTimeFormatInfo instance = DateTimeFormatInfo.GetInstance(provider);
		StringBuilder stringBuilder = FormatStringBuilder(dateTime, format, instance, offset);
		bool flag = stringBuilder.Length <= destination.Length;
		if (flag)
		{
			stringBuilder.CopyTo(0, destination, stringBuilder.Length);
			charsWritten = stringBuilder.Length;
		}
		else
		{
			charsWritten = 0;
		}
		StringBuilderCache.Release(stringBuilder);
		return flag;
	}

	private static StringBuilder FormatStringBuilder(DateTime dateTime, ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, TimeSpan offset)
	{
		if (format.Length == 0)
		{
			bool flag = false;
			if (dateTime.Ticks < 864000000000L)
			{
				switch (dtfi.Calendar.ID)
				{
				case CalendarId.JAPAN:
				case CalendarId.TAIWAN:
				case CalendarId.HIJRI:
				case CalendarId.HEBREW:
				case CalendarId.JULIAN:
				case CalendarId.PERSIAN:
				case CalendarId.UMALQURA:
					flag = true;
					dtfi = DateTimeFormatInfo.InvariantInfo;
					break;
				}
			}
			format = ((offset.Ticks != long.MinValue) ? ((ReadOnlySpan<char>)(flag ? "yyyy'-'MM'-'ddTHH':'mm':'ss zzz" : dtfi.DateTimeOffsetPattern)) : ((ReadOnlySpan<char>)(flag ? "s" : "G")));
		}
		if (format.Length == 1)
		{
			format = ExpandPredefinedFormat(format, ref dateTime, ref dtfi, offset);
		}
		return FormatCustomized(dateTime, format, dtfi, offset, null);
	}

	internal static bool IsValidCustomDateFormat(ReadOnlySpan<char> format, bool throwOnError)
	{
		int i = 0;
		while (i < format.Length)
		{
			switch (format[i])
			{
			case '\\':
				if (i == format.Length - 1)
				{
					if (throwOnError)
					{
						throw new FormatException(SR.Format_InvalidString);
					}
					return false;
				}
				i += 2;
				break;
			case '"':
			case '\'':
			{
				char c;
				for (c = format[i++]; i < format.Length && format[i] != c; i++)
				{
				}
				if (i >= format.Length)
				{
					if (throwOnError)
					{
						throw new FormatException(SR.Format(SR.Format_BadQuote, c));
					}
					return false;
				}
				i++;
				break;
			}
			case ':':
			case 'F':
			case 'H':
			case 'K':
			case 'f':
			case 'h':
			case 'm':
			case 's':
			case 't':
			case 'z':
				if (throwOnError)
				{
					throw new FormatException(SR.Format_InvalidString);
				}
				return false;
			default:
				i++;
				break;
			}
		}
		return true;
	}

	internal static bool IsValidCustomTimeFormat(ReadOnlySpan<char> format, bool throwOnError)
	{
		int length = format.Length;
		int i = 0;
		while (i < length)
		{
			switch (format[i])
			{
			case '\\':
				if (i == length - 1)
				{
					if (throwOnError)
					{
						throw new FormatException(SR.Format_InvalidString);
					}
					return false;
				}
				i += 2;
				break;
			case '"':
			case '\'':
			{
				char c;
				for (c = format[i++]; i < length && format[i] != c; i++)
				{
				}
				if (i >= length)
				{
					if (throwOnError)
					{
						throw new FormatException(SR.Format(SR.Format_BadQuote, c));
					}
					return false;
				}
				i++;
				break;
			}
			case '/':
			case 'M':
			case 'd':
			case 'k':
			case 'y':
			case 'z':
				if (throwOnError)
				{
					throw new FormatException(SR.Format_InvalidString);
				}
				return false;
			default:
				i++;
				break;
			}
		}
		return true;
	}

	internal static bool TryFormatTimeOnlyO(int hour, int minute, int second, long fraction, Span<char> destination)
	{
		if (destination.Length < 16)
		{
			return false;
		}
		WriteTwoDecimalDigits((uint)hour, destination, 0);
		destination[2] = ':';
		WriteTwoDecimalDigits((uint)minute, destination, 3);
		destination[5] = ':';
		WriteTwoDecimalDigits((uint)second, destination, 6);
		destination[8] = '.';
		WriteDigits((uint)fraction, destination.Slice(9, 7));
		return true;
	}

	internal static bool TryFormatTimeOnlyR(int hour, int minute, int second, Span<char> destination)
	{
		if (destination.Length < 8)
		{
			return false;
		}
		WriteTwoDecimalDigits((uint)hour, destination, 0);
		destination[2] = ':';
		WriteTwoDecimalDigits((uint)minute, destination, 3);
		destination[5] = ':';
		WriteTwoDecimalDigits((uint)second, destination, 6);
		return true;
	}

	internal static bool TryFormatDateOnlyO(int year, int month, int day, Span<char> destination)
	{
		if (destination.Length < 10)
		{
			return false;
		}
		WriteFourDecimalDigits((uint)year, destination);
		destination[4] = '-';
		WriteTwoDecimalDigits((uint)month, destination, 5);
		destination[7] = '-';
		WriteTwoDecimalDigits((uint)day, destination, 8);
		return true;
	}

	internal static bool TryFormatDateOnlyR(DayOfWeek dayOfWeek, int year, int month, int day, Span<char> destination)
	{
		if (destination.Length < 16)
		{
			return false;
		}
		string text = InvariantAbbreviatedDayNames[(int)dayOfWeek];
		string text2 = InvariantAbbreviatedMonthNames[month - 1];
		destination[0] = text[0];
		destination[1] = text[1];
		destination[2] = text[2];
		destination[3] = ',';
		destination[4] = ' ';
		WriteTwoDecimalDigits((uint)day, destination, 5);
		destination[7] = ' ';
		destination[8] = text2[0];
		destination[9] = text2[1];
		destination[10] = text2[2];
		destination[11] = ' ';
		WriteFourDecimalDigits((uint)year, destination, 12);
		return true;
	}

	private static bool TryFormatO(DateTime dateTime, TimeSpan offset, Span<char> destination, out int charsWritten)
	{
		int num = 27;
		DateTimeKind dateTimeKind = DateTimeKind.Local;
		if (offset.Ticks == long.MinValue)
		{
			dateTimeKind = dateTime.Kind;
			switch (dateTimeKind)
			{
			case DateTimeKind.Local:
				offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
				num += 6;
				break;
			case DateTimeKind.Utc:
				num++;
				break;
			}
		}
		else
		{
			num += 6;
		}
		if (destination.Length < num)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		_ = ref destination[26];
		dateTime.GetDate(out var year, out var month, out var day);
		dateTime.GetTimePrecise(out var hour, out var minute, out var second, out var tick);
		WriteFourDecimalDigits((uint)year, destination);
		destination[4] = '-';
		WriteTwoDecimalDigits((uint)month, destination, 5);
		destination[7] = '-';
		WriteTwoDecimalDigits((uint)day, destination, 8);
		destination[10] = 'T';
		WriteTwoDecimalDigits((uint)hour, destination, 11);
		destination[13] = ':';
		WriteTwoDecimalDigits((uint)minute, destination, 14);
		destination[16] = ':';
		WriteTwoDecimalDigits((uint)second, destination, 17);
		destination[19] = '.';
		WriteDigits((uint)tick, destination.Slice(20, 7));
		switch (dateTimeKind)
		{
		case DateTimeKind.Local:
		{
			int num2 = (int)(offset.Ticks / 600000000);
			char c;
			if (num2 < 0)
			{
				c = '-';
				num2 = -num2;
			}
			else
			{
				c = '+';
			}
			int result;
			int value = Math.DivRem(num2, 60, out result);
			WriteTwoDecimalDigits((uint)result, destination, 31);
			destination[30] = ':';
			WriteTwoDecimalDigits((uint)value, destination, 28);
			destination[27] = c;
			break;
		}
		case DateTimeKind.Utc:
			destination[27] = 'Z';
			break;
		}
		return true;
	}

	private static bool TryFormatR(DateTime dateTime, TimeSpan offset, Span<char> destination, out int charsWritten)
	{
		if (destination.Length <= 28)
		{
			charsWritten = 0;
			return false;
		}
		if (offset.Ticks != long.MinValue)
		{
			dateTime -= offset;
		}
		dateTime.GetDate(out var year, out var month, out var day);
		dateTime.GetTime(out var hour, out var minute, out var second);
		string text = InvariantAbbreviatedDayNames[(int)dateTime.DayOfWeek];
		string text2 = InvariantAbbreviatedMonthNames[month - 1];
		destination[0] = text[0];
		destination[1] = text[1];
		destination[2] = text[2];
		destination[3] = ',';
		destination[4] = ' ';
		WriteTwoDecimalDigits((uint)day, destination, 5);
		destination[7] = ' ';
		destination[8] = text2[0];
		destination[9] = text2[1];
		destination[10] = text2[2];
		destination[11] = ' ';
		WriteFourDecimalDigits((uint)year, destination, 12);
		destination[16] = ' ';
		WriteTwoDecimalDigits((uint)hour, destination, 17);
		destination[19] = ':';
		WriteTwoDecimalDigits((uint)minute, destination, 20);
		destination[22] = ':';
		WriteTwoDecimalDigits((uint)second, destination, 23);
		destination[25] = ' ';
		destination[26] = 'G';
		destination[27] = 'M';
		destination[28] = 'T';
		charsWritten = 29;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteTwoDecimalDigits(uint value, Span<char> destination, int offset)
	{
		uint num = 48 + value;
		value /= 10;
		destination[offset + 1] = (char)(num - value * 10);
		destination[offset] = (char)(48 + value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteFourDecimalDigits(uint value, Span<char> buffer, int startingIndex = 0)
	{
		uint num = 48 + value;
		value /= 10;
		buffer[startingIndex + 3] = (char)(num - value * 10);
		num = 48 + value;
		value /= 10;
		buffer[startingIndex + 2] = (char)(num - value * 10);
		num = 48 + value;
		value /= 10;
		buffer[startingIndex + 1] = (char)(num - value * 10);
		buffer[startingIndex] = (char)(48 + value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteDigits(ulong value, Span<char> buffer)
	{
		for (int num = buffer.Length - 1; num >= 1; num--)
		{
			ulong num2 = 48 + value;
			value /= 10;
			buffer[num] = (char)(num2 - value * 10);
		}
		buffer[0] = (char)(48 + value);
	}

	internal static string[] GetAllDateTimes(DateTime dateTime, char format, DateTimeFormatInfo dtfi)
	{
		string[] array;
		switch (format)
		{
		case 'D':
		case 'F':
		case 'G':
		case 'M':
		case 'T':
		case 'Y':
		case 'd':
		case 'f':
		case 'g':
		case 'm':
		case 't':
		case 'y':
		{
			string[] allDateTimePatterns = dtfi.GetAllDateTimePatterns(format);
			array = new string[allDateTimePatterns.Length];
			for (int j = 0; j < allDateTimePatterns.Length; j++)
			{
				array[j] = Format(dateTime, allDateTimePatterns[j], dtfi);
			}
			break;
		}
		case 'U':
		{
			DateTime dateTime2 = dateTime.ToUniversalTime();
			string[] allDateTimePatterns = dtfi.GetAllDateTimePatterns(format);
			array = new string[allDateTimePatterns.Length];
			for (int i = 0; i < allDateTimePatterns.Length; i++)
			{
				array[i] = Format(dateTime2, allDateTimePatterns[i], dtfi);
			}
			break;
		}
		case 'O':
		case 'R':
		case 'o':
		case 'r':
		case 's':
		case 'u':
			array = new string[1] { Format(dateTime, char.ToString(format), dtfi) };
			break;
		default:
			throw new FormatException(SR.Format_InvalidString);
		}
		return array;
	}

	internal static string[] GetAllDateTimes(DateTime dateTime, DateTimeFormatInfo dtfi)
	{
		List<string> list = new List<string>(132);
		for (int i = 0; i < allStandardFormats.Length; i++)
		{
			string[] allDateTimes = GetAllDateTimes(dateTime, allStandardFormats[i], dtfi);
			for (int j = 0; j < allDateTimes.Length; j++)
			{
				list.Add(allDateTimes[j]);
			}
		}
		return list.ToArray();
	}
}
