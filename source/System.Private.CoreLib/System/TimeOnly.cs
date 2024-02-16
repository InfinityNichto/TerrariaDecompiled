using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;

namespace System;

public readonly struct TimeOnly : IComparable, IComparable<TimeOnly>, IEquatable<TimeOnly>, ISpanFormattable, IFormattable, IComparisonOperators<TimeOnly, TimeOnly>, IEqualityOperators<TimeOnly, TimeOnly>, IMinMaxValue<TimeOnly>, ISpanParseable<TimeOnly>, IParseable<TimeOnly>, ISubtractionOperators<TimeOnly, TimeOnly, TimeSpan>
{
	private readonly long _ticks;

	public static TimeOnly MinValue => new TimeOnly(0uL);

	public static TimeOnly MaxValue => new TimeOnly(863999999999uL);

	public int Hour => new TimeSpan(_ticks).Hours;

	public int Minute => new TimeSpan(_ticks).Minutes;

	public int Second => new TimeSpan(_ticks).Seconds;

	public int Millisecond => new TimeSpan(_ticks).Milliseconds;

	public long Ticks => _ticks;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TimeOnly IMinMaxValue<TimeOnly>.MinValue => MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TimeOnly IMinMaxValue<TimeOnly>.MaxValue => MaxValue;

	public TimeOnly(int hour, int minute)
		: this(DateTime.TimeToTicks(hour, minute, 0, 0))
	{
	}

	public TimeOnly(int hour, int minute, int second)
		: this(DateTime.TimeToTicks(hour, minute, second, 0))
	{
	}

	public TimeOnly(int hour, int minute, int second, int millisecond)
		: this(DateTime.TimeToTicks(hour, minute, second, millisecond))
	{
	}

	public TimeOnly(long ticks)
	{
		if ((ulong)ticks > 863999999999uL)
		{
			throw new ArgumentOutOfRangeException("ticks", SR.ArgumentOutOfRange_TimeOnlyBadTicks);
		}
		_ticks = ticks;
	}

	internal TimeOnly(ulong ticks)
	{
		_ticks = (long)ticks;
	}

	private TimeOnly AddTicks(long ticks)
	{
		return new TimeOnly((_ticks + 864000000000L + ticks % 864000000000L) % 864000000000L);
	}

	private TimeOnly AddTicks(long ticks, out int wrappedDays)
	{
		wrappedDays = (int)(ticks / 864000000000L);
		long num = _ticks + ticks % 864000000000L;
		if (num < 0)
		{
			wrappedDays--;
			num += 864000000000L;
		}
		else if (num >= 864000000000L)
		{
			wrappedDays++;
			num -= 864000000000L;
		}
		return new TimeOnly(num);
	}

	public TimeOnly Add(TimeSpan value)
	{
		return AddTicks(value.Ticks);
	}

	public TimeOnly Add(TimeSpan value, out int wrappedDays)
	{
		return AddTicks(value.Ticks, out wrappedDays);
	}

	public TimeOnly AddHours(double value)
	{
		return AddTicks((long)(value * 36000000000.0));
	}

	public TimeOnly AddHours(double value, out int wrappedDays)
	{
		return AddTicks((long)(value * 36000000000.0), out wrappedDays);
	}

	public TimeOnly AddMinutes(double value)
	{
		return AddTicks((long)(value * 600000000.0));
	}

	public TimeOnly AddMinutes(double value, out int wrappedDays)
	{
		return AddTicks((long)(value * 600000000.0), out wrappedDays);
	}

	public bool IsBetween(TimeOnly start, TimeOnly end)
	{
		long ticks = start._ticks;
		long ticks2 = end._ticks;
		if (ticks > ticks2)
		{
			if (ticks > _ticks)
			{
				return ticks2 > _ticks;
			}
			return true;
		}
		if (ticks <= _ticks)
		{
			return ticks2 > _ticks;
		}
		return false;
	}

	public static bool operator ==(TimeOnly left, TimeOnly right)
	{
		return left._ticks == right._ticks;
	}

	public static bool operator !=(TimeOnly left, TimeOnly right)
	{
		return left._ticks != right._ticks;
	}

	public static bool operator >(TimeOnly left, TimeOnly right)
	{
		return left._ticks > right._ticks;
	}

	public static bool operator >=(TimeOnly left, TimeOnly right)
	{
		return left._ticks >= right._ticks;
	}

	public static bool operator <(TimeOnly left, TimeOnly right)
	{
		return left._ticks < right._ticks;
	}

	public static bool operator <=(TimeOnly left, TimeOnly right)
	{
		return left._ticks <= right._ticks;
	}

	public static TimeSpan operator -(TimeOnly t1, TimeOnly t2)
	{
		return new TimeSpan((t1._ticks - t2._ticks + 864000000000L) % 864000000000L);
	}

	public static TimeOnly FromTimeSpan(TimeSpan timeSpan)
	{
		return new TimeOnly(timeSpan._ticks);
	}

	public static TimeOnly FromDateTime(DateTime dateTime)
	{
		return new TimeOnly(dateTime.TimeOfDay.Ticks);
	}

	public TimeSpan ToTimeSpan()
	{
		return new TimeSpan(_ticks);
	}

	internal DateTime ToDateTime()
	{
		return new DateTime(_ticks);
	}

	public int CompareTo(TimeOnly value)
	{
		return _ticks.CompareTo(value._ticks);
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (!(value is TimeOnly value2))
		{
			throw new ArgumentException(SR.Arg_MustBeTimeOnly);
		}
		return CompareTo(value2);
	}

	public bool Equals(TimeOnly value)
	{
		return _ticks == value._ticks;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is TimeOnly timeOnly)
		{
			return _ticks == timeOnly._ticks;
		}
		return false;
	}

	public override int GetHashCode()
	{
		long ticks = _ticks;
		return (int)ticks ^ (int)(ticks >> 32);
	}

	public static TimeOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null, DateTimeStyles style = DateTimeStyles.None)
	{
		TimeOnly result;
		ParseFailureKind parseFailureKind = TryParseInternal(s, provider, style, out result);
		if (parseFailureKind != 0)
		{
			ThrowOnError(parseFailureKind, s);
		}
		return result;
	}

	public static TimeOnly ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider = null, DateTimeStyles style = DateTimeStyles.None)
	{
		TimeOnly result;
		ParseFailureKind parseFailureKind = TryParseExactInternal(s, format, provider, style, out result);
		if (parseFailureKind != 0)
		{
			ThrowOnError(parseFailureKind, s);
		}
		return result;
	}

	public static TimeOnly ParseExact(ReadOnlySpan<char> s, string[] formats)
	{
		return ParseExact(s, formats, null);
	}

	public static TimeOnly ParseExact(ReadOnlySpan<char> s, string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		TimeOnly result;
		ParseFailureKind parseFailureKind = TryParseExactInternal(s, formats, provider, style, out result);
		if (parseFailureKind != 0)
		{
			ThrowOnError(parseFailureKind, s);
		}
		return result;
	}

	public static TimeOnly Parse(string s)
	{
		return Parse(s, null);
	}

	public static TimeOnly Parse(string s, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), provider, style);
	}

	public static TimeOnly ParseExact(string s, string format)
	{
		return ParseExact(s, format, null);
	}

	public static TimeOnly ParseExact(string s, string format, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
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

	public static TimeOnly ParseExact(string s, string[] formats)
	{
		return ParseExact(s, formats, null);
	}

	public static TimeOnly ParseExact(string s, string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return ParseExact(s.AsSpan(), formats, provider, style);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out TimeOnly result)
	{
		return TryParse(s, null, DateTimeStyles.None, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
	{
		return TryParseInternal(s, provider, style, out result) == ParseFailureKind.None;
	}

	private static ParseFailureKind TryParseInternal(ReadOnlySpan<char> s, IFormatProvider provider, DateTimeStyles style, out TimeOnly result)
	{
		if (((uint)style & 0xFFFFFFF8u) != 0)
		{
			result = default(TimeOnly);
			return ParseFailureKind.FormatWithParameter;
		}
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		if (!DateTimeParse.TryParse(s, DateTimeFormatInfo.GetInstance(provider), style, ref result2))
		{
			result = default(TimeOnly);
			return ParseFailureKind.FormatWithOriginalDateTime;
		}
		if ((result2.flags & (ParseFlags.HaveYear | ParseFlags.HaveMonth | ParseFlags.HaveDay | ParseFlags.HaveDate | ParseFlags.TimeZoneUsed | ParseFlags.TimeZoneUtc | ParseFlags.ParsedMonthName | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern)) != 0)
		{
			result = default(TimeOnly);
			return ParseFailureKind.WrongParts;
		}
		result = new TimeOnly(result2.parsedDate.TimeOfDay.Ticks);
		return ParseFailureKind.None;
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, out TimeOnly result)
	{
		return TryParseExact(s, format, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
	{
		return TryParseExactInternal(s, format, provider, style, out result) == ParseFailureKind.None;
	}

	private static ParseFailureKind TryParseExactInternal(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider provider, DateTimeStyles style, out TimeOnly result)
	{
		if (((uint)style & 0xFFFFFFF8u) != 0)
		{
			result = default(TimeOnly);
			return ParseFailureKind.FormatWithParameter;
		}
		if (format.Length == 1)
		{
			switch (format[0])
			{
			case 'O':
			case 'o':
				format = "HH':'mm':'ss'.'fffffff";
				provider = CultureInfo.InvariantCulture.DateTimeFormat;
				break;
			case 'R':
			case 'r':
				format = "HH':'mm':'ss";
				provider = CultureInfo.InvariantCulture.DateTimeFormat;
				break;
			}
		}
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		if (!DateTimeParse.TryParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style, ref result2))
		{
			result = default(TimeOnly);
			return ParseFailureKind.FormatWithOriginalDateTime;
		}
		if ((result2.flags & (ParseFlags.HaveYear | ParseFlags.HaveMonth | ParseFlags.HaveDay | ParseFlags.HaveDate | ParseFlags.TimeZoneUsed | ParseFlags.TimeZoneUtc | ParseFlags.ParsedMonthName | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern)) != 0)
		{
			result = default(TimeOnly);
			return ParseFailureKind.WrongParts;
		}
		result = new TimeOnly(result2.parsedDate.TimeOfDay.Ticks);
		return ParseFailureKind.None;
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true)] string?[]? formats, out TimeOnly result)
	{
		return TryParseExact(s, formats, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
	{
		return TryParseExactInternal(s, formats, provider, style, out result) == ParseFailureKind.None;
	}

	private static ParseFailureKind TryParseExactInternal(ReadOnlySpan<char> s, string[] formats, IFormatProvider provider, DateTimeStyles style, out TimeOnly result)
	{
		if (((uint)style & 0xFFFFFFF8u) != 0 || formats == null)
		{
			result = default(TimeOnly);
			return ParseFailureKind.FormatWithParameter;
		}
		DateTimeFormatInfo instance = DateTimeFormatInfo.GetInstance(provider);
		for (int i = 0; i < formats.Length; i++)
		{
			DateTimeFormatInfo dtfi = instance;
			string text = formats[i];
			if (string.IsNullOrEmpty(text))
			{
				result = default(TimeOnly);
				return ParseFailureKind.FormatWithFormatSpecifier;
			}
			if (text.Length == 1)
			{
				switch (text[0])
				{
				case 'O':
				case 'o':
					text = "HH':'mm':'ss'.'fffffff";
					dtfi = CultureInfo.InvariantCulture.DateTimeFormat;
					break;
				case 'R':
				case 'r':
					text = "HH':'mm':'ss";
					dtfi = CultureInfo.InvariantCulture.DateTimeFormat;
					break;
				}
			}
			DateTimeResult result2 = default(DateTimeResult);
			result2.Init(s);
			if (DateTimeParse.TryParseExact(s, text, dtfi, style, ref result2) && (result2.flags & (ParseFlags.HaveYear | ParseFlags.HaveMonth | ParseFlags.HaveDay | ParseFlags.HaveDate | ParseFlags.TimeZoneUsed | ParseFlags.TimeZoneUtc | ParseFlags.ParsedMonthName | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern)) == 0)
			{
				result = new TimeOnly(result2.parsedDate.TimeOfDay.Ticks);
				return ParseFailureKind.None;
			}
		}
		result = default(TimeOnly);
		return ParseFailureKind.FormatWithOriginalDateTime;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out TimeOnly result)
	{
		return TryParse(s, null, DateTimeStyles.None, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
	{
		if (s == null)
		{
			result = default(TimeOnly);
			return false;
		}
		return TryParse(s.AsSpan(), provider, style, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, out TimeOnly result)
	{
		return TryParseExact(s, format, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
	{
		if (s == null || format == null)
		{
			result = default(TimeOnly);
			return false;
		}
		return TryParseExact(s.AsSpan(), format.AsSpan(), provider, style, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string?[]? formats, out TimeOnly result)
	{
		return TryParseExact(s, formats, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
	{
		if (s == null)
		{
			result = default(TimeOnly);
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
			throw new FormatException(SR.Format(SR.Format_BadTimeOnly, s.ToString()));
		case ParseFailureKind.FormatWithFormatSpecifier:
			throw new FormatException(SR.Argument_BadFormatSpecifier);
		default:
			throw new FormatException(SR.Format(SR.Format_DateTimeOnlyContainsNoneDateParts, s.ToString(), "TimeOnly"));
		}
	}

	public string ToLongTimeString()
	{
		return ToString("T");
	}

	public string ToShortTimeString()
	{
		return ToString();
	}

	public override string ToString()
	{
		return ToString("t");
	}

	public string ToString(string? format)
	{
		return ToString(format, null);
	}

	public string ToString(IFormatProvider? provider)
	{
		return ToString("t", provider);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		if (format == null || format.Length == 0)
		{
			format = "t";
		}
		if (format.Length == 1)
		{
			switch (format[0])
			{
			case 'O':
			case 'o':
				return string.Create(16, this, delegate(Span<char> destination, TimeOnly value)
				{
					bool flag = DateTimeFormat.TryFormatTimeOnlyO(value.Hour, value.Minute, value.Second, value._ticks % 10000000, destination);
				});
			case 'R':
			case 'r':
				return string.Create(8, this, delegate(Span<char> destination, TimeOnly value)
				{
					bool flag2 = DateTimeFormat.TryFormatTimeOnlyR(value.Hour, value.Minute, value.Second, destination);
				});
			case 'T':
			case 't':
				return DateTimeFormat.Format(ToDateTime(), format, provider);
			default:
				throw new FormatException(SR.Format_InvalidString);
			}
		}
		DateTimeFormat.IsValidCustomTimeFormat(format.AsSpan(), throwOnError: true);
		return DateTimeFormat.Format(ToDateTime(), format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		if (format.Length == 0)
		{
			format = "t";
		}
		if (format.Length == 1)
		{
			switch (format[0])
			{
			case 'O':
			case 'o':
				if (!DateTimeFormat.TryFormatTimeOnlyO(Hour, Minute, Second, _ticks % 10000000, destination))
				{
					charsWritten = 0;
					return false;
				}
				charsWritten = 16;
				return true;
			case 'R':
			case 'r':
				if (!DateTimeFormat.TryFormatTimeOnlyR(Hour, Minute, Second, destination))
				{
					charsWritten = 0;
					return false;
				}
				charsWritten = 8;
				return true;
			case 'T':
			case 't':
				return DateTimeFormat.TryFormat(ToDateTime(), destination, out charsWritten, format, provider);
			default:
				charsWritten = 0;
				return false;
			}
		}
		if (!DateTimeFormat.IsValidCustomTimeFormat(format, throwOnError: false))
		{
			charsWritten = 0;
			return false;
		}
		return DateTimeFormat.TryFormat(ToDateTime(), destination, out charsWritten, format, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<TimeOnly, TimeOnly>.operator <(TimeOnly left, TimeOnly right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<TimeOnly, TimeOnly>.operator <=(TimeOnly left, TimeOnly right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<TimeOnly, TimeOnly>.operator >(TimeOnly left, TimeOnly right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<TimeOnly, TimeOnly>.operator >=(TimeOnly left, TimeOnly right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<TimeOnly, TimeOnly>.operator ==(TimeOnly left, TimeOnly right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<TimeOnly, TimeOnly>.operator !=(TimeOnly left, TimeOnly right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TimeOnly IParseable<TimeOnly>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<TimeOnly>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out TimeOnly result)
	{
		return TryParse(s, provider, DateTimeStyles.None, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TimeOnly ISpanParseable<TimeOnly>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<TimeOnly>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out TimeOnly result)
	{
		return TryParse(s, provider, DateTimeStyles.None, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TimeSpan ISubtractionOperators<TimeOnly, TimeOnly, TimeSpan>.operator -(TimeOnly left, TimeOnly right)
	{
		return left - right;
	}
}
