using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;

namespace System;

public readonly struct DateOnly : IComparable, IComparable<DateOnly>, IEquatable<DateOnly>, ISpanFormattable, IFormattable, IComparisonOperators<DateOnly, DateOnly>, IEqualityOperators<DateOnly, DateOnly>, IMinMaxValue<DateOnly>, ISpanParseable<DateOnly>, IParseable<DateOnly>
{
	private readonly int _dayNumber;

	public static DateOnly MinValue => new DateOnly(0);

	public static DateOnly MaxValue => new DateOnly(3652058);

	public int Year => GetEquivalentDateTime().Year;

	public int Month => GetEquivalentDateTime().Month;

	public int Day => GetEquivalentDateTime().Day;

	public DayOfWeek DayOfWeek => GetEquivalentDateTime().DayOfWeek;

	public int DayOfYear => GetEquivalentDateTime().DayOfYear;

	public int DayNumber => _dayNumber;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateOnly IMinMaxValue<DateOnly>.MinValue => MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateOnly IMinMaxValue<DateOnly>.MaxValue => MaxValue;

	private static int DayNumberFromDateTime(DateTime dt)
	{
		return (int)(dt.Ticks / 864000000000L);
	}

	private DateTime GetEquivalentDateTime()
	{
		return DateTime.UnsafeCreate(_dayNumber * 864000000000L);
	}

	private DateOnly(int dayNumber)
	{
		_dayNumber = dayNumber;
	}

	public DateOnly(int year, int month, int day)
	{
		_dayNumber = DayNumberFromDateTime(new DateTime(year, month, day));
	}

	public DateOnly(int year, int month, int day, Calendar calendar)
	{
		_dayNumber = DayNumberFromDateTime(new DateTime(year, month, day, calendar));
	}

	public static DateOnly FromDayNumber(int dayNumber)
	{
		if ((uint)dayNumber > 3652058u)
		{
			ThrowHelper.ThrowArgumentOutOfRange_DayNumber(dayNumber);
		}
		return new DateOnly(dayNumber);
	}

	public DateOnly AddDays(int value)
	{
		int num = _dayNumber + value;
		if ((uint)num > 3652058u)
		{
			ThrowOutOfRange();
		}
		return new DateOnly(num);
		static void ThrowOutOfRange()
		{
			throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_AddValue);
		}
	}

	public DateOnly AddMonths(int value)
	{
		return new DateOnly(DayNumberFromDateTime(GetEquivalentDateTime().AddMonths(value)));
	}

	public DateOnly AddYears(int value)
	{
		return new DateOnly(DayNumberFromDateTime(GetEquivalentDateTime().AddYears(value)));
	}

	public static bool operator ==(DateOnly left, DateOnly right)
	{
		return left._dayNumber == right._dayNumber;
	}

	public static bool operator !=(DateOnly left, DateOnly right)
	{
		return left._dayNumber != right._dayNumber;
	}

	public static bool operator >(DateOnly left, DateOnly right)
	{
		return left._dayNumber > right._dayNumber;
	}

	public static bool operator >=(DateOnly left, DateOnly right)
	{
		return left._dayNumber >= right._dayNumber;
	}

	public static bool operator <(DateOnly left, DateOnly right)
	{
		return left._dayNumber < right._dayNumber;
	}

	public static bool operator <=(DateOnly left, DateOnly right)
	{
		return left._dayNumber <= right._dayNumber;
	}

	public DateTime ToDateTime(TimeOnly time)
	{
		return new DateTime(_dayNumber * 864000000000L + time.Ticks);
	}

	public DateTime ToDateTime(TimeOnly time, DateTimeKind kind)
	{
		return new DateTime(_dayNumber * 864000000000L + time.Ticks, kind);
	}

	public static DateOnly FromDateTime(DateTime dateTime)
	{
		return new DateOnly(DayNumberFromDateTime(dateTime));
	}

	public int CompareTo(DateOnly value)
	{
		return _dayNumber.CompareTo(value._dayNumber);
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (!(value is DateOnly value2))
		{
			throw new ArgumentException(SR.Arg_MustBeDateOnly);
		}
		return CompareTo(value2);
	}

	public bool Equals(DateOnly value)
	{
		return _dayNumber == value._dayNumber;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is DateOnly dateOnly)
		{
			return _dayNumber == dateOnly._dayNumber;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _dayNumber;
	}

	public static DateOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null, DateTimeStyles style = DateTimeStyles.None)
	{
		DateOnly result;
		ParseFailureKind parseFailureKind = TryParseInternal(s, provider, style, out result);
		if (parseFailureKind != 0)
		{
			ThrowOnError(parseFailureKind, s);
		}
		return result;
	}

	public static DateOnly ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider = null, DateTimeStyles style = DateTimeStyles.None)
	{
		DateOnly result;
		ParseFailureKind parseFailureKind = TryParseExactInternal(s, format, provider, style, out result);
		if (parseFailureKind != 0)
		{
			ThrowOnError(parseFailureKind, s);
		}
		return result;
	}

	public static DateOnly ParseExact(ReadOnlySpan<char> s, string[] formats)
	{
		return ParseExact(s, formats, null);
	}

	public static DateOnly ParseExact(ReadOnlySpan<char> s, string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		DateOnly result;
		ParseFailureKind parseFailureKind = TryParseExactInternal(s, formats, provider, style, out result);
		if (parseFailureKind != 0)
		{
			ThrowOnError(parseFailureKind, s);
		}
		return result;
	}

	public static DateOnly Parse(string s)
	{
		return Parse(s, null);
	}

	public static DateOnly Parse(string s, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), provider, style);
	}

	public static DateOnly ParseExact(string s, string format)
	{
		return ParseExact(s, format, null);
	}

	public static DateOnly ParseExact(string s, string format, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if (format == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
		}
		return ParseExact(s.AsSpan(), format.AsSpan(), provider, style);
	}

	public static DateOnly ParseExact(string s, string[] formats)
	{
		return ParseExact(s, formats, null);
	}

	public static DateOnly ParseExact(string s, string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return ParseExact(s.AsSpan(), formats, provider, style);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out DateOnly result)
	{
		return TryParse(s, null, DateTimeStyles.None, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		return TryParseInternal(s, provider, style, out result) == ParseFailureKind.None;
	}

	private static ParseFailureKind TryParseInternal(ReadOnlySpan<char> s, IFormatProvider provider, DateTimeStyles style, out DateOnly result)
	{
		if (((uint)style & 0xFFFFFFF8u) != 0)
		{
			result = default(DateOnly);
			return ParseFailureKind.FormatWithParameter;
		}
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		if (!DateTimeParse.TryParse(s, DateTimeFormatInfo.GetInstance(provider), style, ref result2))
		{
			result = default(DateOnly);
			return ParseFailureKind.FormatWithOriginalDateTime;
		}
		if ((result2.flags & (ParseFlags.HaveHour | ParseFlags.HaveMinute | ParseFlags.HaveSecond | ParseFlags.HaveTime | ParseFlags.TimeZoneUsed | ParseFlags.TimeZoneUtc | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern)) != 0)
		{
			result = default(DateOnly);
			return ParseFailureKind.WrongParts;
		}
		result = new DateOnly(DayNumberFromDateTime(result2.parsedDate));
		return ParseFailureKind.None;
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, out DateOnly result)
	{
		return TryParseExact(s, format, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		return TryParseExactInternal(s, format, provider, style, out result) == ParseFailureKind.None;
	}

	private static ParseFailureKind TryParseExactInternal(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider provider, DateTimeStyles style, out DateOnly result)
	{
		if (((uint)style & 0xFFFFFFF8u) != 0)
		{
			result = default(DateOnly);
			return ParseFailureKind.FormatWithParameter;
		}
		if (format.Length == 1)
		{
			switch (format[0])
			{
			case 'O':
			case 'o':
				format = "yyyy'-'MM'-'dd";
				provider = CultureInfo.InvariantCulture.DateTimeFormat;
				break;
			case 'R':
			case 'r':
				format = "ddd, dd MMM yyyy";
				provider = CultureInfo.InvariantCulture.DateTimeFormat;
				break;
			}
		}
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		if (!DateTimeParse.TryParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style, ref result2))
		{
			result = default(DateOnly);
			return ParseFailureKind.FormatWithOriginalDateTime;
		}
		if ((result2.flags & (ParseFlags.HaveHour | ParseFlags.HaveMinute | ParseFlags.HaveSecond | ParseFlags.HaveTime | ParseFlags.TimeZoneUsed | ParseFlags.TimeZoneUtc | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern)) != 0)
		{
			result = default(DateOnly);
			return ParseFailureKind.WrongParts;
		}
		result = new DateOnly(DayNumberFromDateTime(result2.parsedDate));
		return ParseFailureKind.None;
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true)] string?[]? formats, out DateOnly result)
	{
		return TryParseExact(s, formats, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		return TryParseExactInternal(s, formats, provider, style, out result) == ParseFailureKind.None;
	}

	private static ParseFailureKind TryParseExactInternal(ReadOnlySpan<char> s, string[] formats, IFormatProvider provider, DateTimeStyles style, out DateOnly result)
	{
		if (((uint)style & 0xFFFFFFF8u) != 0 || formats == null)
		{
			result = default(DateOnly);
			return ParseFailureKind.FormatWithParameter;
		}
		DateTimeFormatInfo instance = DateTimeFormatInfo.GetInstance(provider);
		for (int i = 0; i < formats.Length; i++)
		{
			DateTimeFormatInfo dtfi = instance;
			string text = formats[i];
			if (string.IsNullOrEmpty(text))
			{
				result = default(DateOnly);
				return ParseFailureKind.FormatWithFormatSpecifier;
			}
			if (text.Length == 1)
			{
				switch (text[0])
				{
				case 'O':
				case 'o':
					text = "yyyy'-'MM'-'dd";
					dtfi = CultureInfo.InvariantCulture.DateTimeFormat;
					break;
				case 'R':
				case 'r':
					text = "ddd, dd MMM yyyy";
					dtfi = CultureInfo.InvariantCulture.DateTimeFormat;
					break;
				}
			}
			DateTimeResult result2 = default(DateTimeResult);
			result2.Init(s);
			if (DateTimeParse.TryParseExact(s, text, dtfi, style, ref result2) && (result2.flags & (ParseFlags.HaveHour | ParseFlags.HaveMinute | ParseFlags.HaveSecond | ParseFlags.HaveTime | ParseFlags.TimeZoneUsed | ParseFlags.TimeZoneUtc | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern)) == 0)
			{
				result = new DateOnly(DayNumberFromDateTime(result2.parsedDate));
				return ParseFailureKind.None;
			}
		}
		result = default(DateOnly);
		return ParseFailureKind.FormatWithOriginalDateTime;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out DateOnly result)
	{
		return TryParse(s, null, DateTimeStyles.None, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		if (s == null)
		{
			result = default(DateOnly);
			return false;
		}
		return TryParse(s.AsSpan(), provider, style, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, out DateOnly result)
	{
		return TryParseExact(s, format, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		if (s == null || format == null)
		{
			result = default(DateOnly);
			return false;
		}
		return TryParseExact(s.AsSpan(), format.AsSpan(), provider, style, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string?[]? formats, out DateOnly result)
	{
		return TryParseExact(s, formats, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		if (s == null)
		{
			result = default(DateOnly);
			return false;
		}
		return TryParseExact(s.AsSpan(), formats, provider, style, out result);
	}

	private static void ThrowOnError(ParseFailureKind result, ReadOnlySpan<char> s)
	{
		switch (result)
		{
		case ParseFailureKind.FormatWithParameter:
			throw new ArgumentException(SR.Argument_InvalidDateStyles, "style");
		case ParseFailureKind.FormatWithOriginalDateTime:
			throw new FormatException(SR.Format(SR.Format_BadDateOnly, s.ToString()));
		case ParseFailureKind.FormatWithFormatSpecifier:
			throw new FormatException(SR.Argument_BadFormatSpecifier);
		default:
			throw new FormatException(SR.Format(SR.Format_DateTimeOnlyContainsNoneDateParts, s.ToString(), "DateOnly"));
		}
	}

	public string ToLongDateString()
	{
		return ToString("D");
	}

	public string ToShortDateString()
	{
		return ToString();
	}

	public override string ToString()
	{
		return ToString("d");
	}

	public string ToString(string? format)
	{
		return ToString(format, null);
	}

	public string ToString(IFormatProvider? provider)
	{
		return ToString("d", provider);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		if (format == null || format.Length == 0)
		{
			format = "d";
		}
		if (format.Length == 1)
		{
			switch (format[0])
			{
			case 'O':
			case 'o':
				return string.Create(10, this, delegate(Span<char> destination, DateOnly value)
				{
					bool flag = DateTimeFormat.TryFormatDateOnlyO(value.Year, value.Month, value.Day, destination);
				});
			case 'R':
			case 'r':
				return string.Create(16, this, delegate(Span<char> destination, DateOnly value)
				{
					bool flag2 = DateTimeFormat.TryFormatDateOnlyR(value.DayOfWeek, value.Year, value.Month, value.Day, destination);
				});
			case 'D':
			case 'M':
			case 'Y':
			case 'd':
			case 'm':
			case 'y':
				return DateTimeFormat.Format(GetEquivalentDateTime(), format, provider);
			default:
				throw new FormatException(SR.Format_InvalidString);
			}
		}
		DateTimeFormat.IsValidCustomDateFormat(format.AsSpan(), throwOnError: true);
		return DateTimeFormat.Format(GetEquivalentDateTime(), format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		if (format.Length == 0)
		{
			format = "d";
		}
		if (format.Length == 1)
		{
			switch (format[0])
			{
			case 'O':
			case 'o':
				if (!DateTimeFormat.TryFormatDateOnlyO(Year, Month, Day, destination))
				{
					charsWritten = 0;
					return false;
				}
				charsWritten = 10;
				return true;
			case 'R':
			case 'r':
				if (!DateTimeFormat.TryFormatDateOnlyR(DayOfWeek, Year, Month, Day, destination))
				{
					charsWritten = 0;
					return false;
				}
				charsWritten = 16;
				return true;
			case 'D':
			case 'M':
			case 'Y':
			case 'd':
			case 'm':
			case 'y':
				return DateTimeFormat.TryFormat(GetEquivalentDateTime(), destination, out charsWritten, format, provider);
			default:
				charsWritten = 0;
				return false;
			}
		}
		if (!DateTimeFormat.IsValidCustomDateFormat(format, throwOnError: false))
		{
			charsWritten = 0;
			return false;
		}
		return DateTimeFormat.TryFormat(GetEquivalentDateTime(), destination, out charsWritten, format, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateOnly, DateOnly>.operator <(DateOnly left, DateOnly right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateOnly, DateOnly>.operator <=(DateOnly left, DateOnly right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateOnly, DateOnly>.operator >(DateOnly left, DateOnly right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateOnly, DateOnly>.operator >=(DateOnly left, DateOnly right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<DateOnly, DateOnly>.operator ==(DateOnly left, DateOnly right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<DateOnly, DateOnly>.operator !=(DateOnly left, DateOnly right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateOnly IParseable<DateOnly>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<DateOnly>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out DateOnly result)
	{
		return TryParse(s, provider, DateTimeStyles.None, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateOnly ISpanParseable<DateOnly>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<DateOnly>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out DateOnly result)
	{
		return TryParse(s, provider, DateTimeStyles.None, out result);
	}
}
