using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[StructLayout(LayoutKind.Auto)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct DateTimeOffset : IComparable, ISpanFormattable, IFormattable, IComparable<DateTimeOffset>, IEquatable<DateTimeOffset>, ISerializable, IDeserializationCallback, IAdditionOperators<DateTimeOffset, TimeSpan, DateTimeOffset>, IAdditiveIdentity<DateTimeOffset, TimeSpan>, IComparisonOperators<DateTimeOffset, DateTimeOffset>, IEqualityOperators<DateTimeOffset, DateTimeOffset>, IMinMaxValue<DateTimeOffset>, ISpanParseable<DateTimeOffset>, IParseable<DateTimeOffset>, ISubtractionOperators<DateTimeOffset, TimeSpan, DateTimeOffset>, ISubtractionOperators<DateTimeOffset, DateTimeOffset, TimeSpan>
{
	public static readonly DateTimeOffset MinValue = new DateTimeOffset(0L, TimeSpan.Zero);

	public static readonly DateTimeOffset MaxValue = new DateTimeOffset(3155378975999999999L, TimeSpan.Zero);

	public static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(621355968000000000L, TimeSpan.Zero);

	private readonly DateTime _dateTime;

	private readonly short _offsetMinutes;

	public static DateTimeOffset Now => ToLocalTime(DateTime.UtcNow, throwOnOverflow: true);

	public static DateTimeOffset UtcNow
	{
		get
		{
			DateTime utcNow = DateTime.UtcNow;
			return new DateTimeOffset(0, utcNow);
		}
	}

	public DateTime DateTime => ClockDateTime;

	public DateTime UtcDateTime => DateTime.SpecifyKind(_dateTime, DateTimeKind.Utc);

	public DateTime LocalDateTime => UtcDateTime.ToLocalTime();

	private DateTime ClockDateTime => new DateTime((_dateTime + Offset).Ticks, DateTimeKind.Unspecified);

	public DateTime Date => ClockDateTime.Date;

	public int Day => ClockDateTime.Day;

	public DayOfWeek DayOfWeek => ClockDateTime.DayOfWeek;

	public int DayOfYear => ClockDateTime.DayOfYear;

	public int Hour => ClockDateTime.Hour;

	public int Millisecond => ClockDateTime.Millisecond;

	public int Minute => ClockDateTime.Minute;

	public int Month => ClockDateTime.Month;

	public TimeSpan Offset => new TimeSpan(0, _offsetMinutes, 0);

	public int Second => ClockDateTime.Second;

	public long Ticks => ClockDateTime.Ticks;

	public long UtcTicks => UtcDateTime.Ticks;

	public TimeSpan TimeOfDay => ClockDateTime.TimeOfDay;

	public int Year => ClockDateTime.Year;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TimeSpan IAdditiveIdentity<DateTimeOffset, TimeSpan>.AdditiveIdentity => default(TimeSpan);

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTimeOffset IMinMaxValue<DateTimeOffset>.MinValue => MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTimeOffset IMinMaxValue<DateTimeOffset>.MaxValue => MaxValue;

	private DateTimeOffset(short validOffsetMinutes, DateTime validDateTime)
	{
		_dateTime = validDateTime;
		_offsetMinutes = validOffsetMinutes;
	}

	public DateTimeOffset(long ticks, TimeSpan offset)
		: this(ValidateOffset(offset), ValidateDate(new DateTime(ticks), offset))
	{
	}

	public DateTimeOffset(DateTime dateTime)
	{
		TimeSpan offset = ((dateTime.Kind == DateTimeKind.Utc) ? new TimeSpan(0L) : TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime));
		_offsetMinutes = ValidateOffset(offset);
		_dateTime = ValidateDate(dateTime, offset);
	}

	public DateTimeOffset(DateTime dateTime, TimeSpan offset)
	{
		if (dateTime.Kind == DateTimeKind.Local)
		{
			if (offset != TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime))
			{
				throw new ArgumentException(SR.Argument_OffsetLocalMismatch, "offset");
			}
		}
		else if (dateTime.Kind == DateTimeKind.Utc && offset != TimeSpan.Zero)
		{
			throw new ArgumentException(SR.Argument_OffsetUtcMismatch, "offset");
		}
		_offsetMinutes = ValidateOffset(offset);
		_dateTime = ValidateDate(dateTime, offset);
	}

	public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, TimeSpan offset)
	{
		_offsetMinutes = ValidateOffset(offset);
		int num = second;
		if (second == 60 && DateTime.s_systemSupportsLeapSeconds)
		{
			second = 59;
		}
		_dateTime = ValidateDate(new DateTime(year, month, day, hour, minute, second), offset);
		if (num == 60 && !DateTime.IsValidTimeWithLeapSeconds(_dateTime.Year, _dateTime.Month, _dateTime.Day, _dateTime.Hour, _dateTime.Minute, DateTimeKind.Utc))
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
		}
	}

	public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, TimeSpan offset)
	{
		_offsetMinutes = ValidateOffset(offset);
		int num = second;
		if (second == 60 && DateTime.s_systemSupportsLeapSeconds)
		{
			second = 59;
		}
		_dateTime = ValidateDate(new DateTime(year, month, day, hour, minute, second, millisecond), offset);
		if (num == 60 && !DateTime.IsValidTimeWithLeapSeconds(_dateTime.Year, _dateTime.Month, _dateTime.Day, _dateTime.Hour, _dateTime.Minute, DateTimeKind.Utc))
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
		}
	}

	public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar, TimeSpan offset)
	{
		_offsetMinutes = ValidateOffset(offset);
		int num = second;
		if (second == 60 && DateTime.s_systemSupportsLeapSeconds)
		{
			second = 59;
		}
		_dateTime = ValidateDate(new DateTime(year, month, day, hour, minute, second, millisecond, calendar), offset);
		if (num == 60 && !DateTime.IsValidTimeWithLeapSeconds(_dateTime.Year, _dateTime.Month, _dateTime.Day, _dateTime.Hour, _dateTime.Minute, DateTimeKind.Utc))
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
		}
	}

	public DateTimeOffset ToOffset(TimeSpan offset)
	{
		return new DateTimeOffset((_dateTime + offset).Ticks, offset);
	}

	public DateTimeOffset Add(TimeSpan timeSpan)
	{
		return new DateTimeOffset(ClockDateTime.Add(timeSpan), Offset);
	}

	public DateTimeOffset AddDays(double days)
	{
		return new DateTimeOffset(ClockDateTime.AddDays(days), Offset);
	}

	public DateTimeOffset AddHours(double hours)
	{
		return new DateTimeOffset(ClockDateTime.AddHours(hours), Offset);
	}

	public DateTimeOffset AddMilliseconds(double milliseconds)
	{
		return new DateTimeOffset(ClockDateTime.AddMilliseconds(milliseconds), Offset);
	}

	public DateTimeOffset AddMinutes(double minutes)
	{
		return new DateTimeOffset(ClockDateTime.AddMinutes(minutes), Offset);
	}

	public DateTimeOffset AddMonths(int months)
	{
		return new DateTimeOffset(ClockDateTime.AddMonths(months), Offset);
	}

	public DateTimeOffset AddSeconds(double seconds)
	{
		return new DateTimeOffset(ClockDateTime.AddSeconds(seconds), Offset);
	}

	public DateTimeOffset AddTicks(long ticks)
	{
		return new DateTimeOffset(ClockDateTime.AddTicks(ticks), Offset);
	}

	public DateTimeOffset AddYears(int years)
	{
		return new DateTimeOffset(ClockDateTime.AddYears(years), Offset);
	}

	public static int Compare(DateTimeOffset first, DateTimeOffset second)
	{
		return DateTime.Compare(first.UtcDateTime, second.UtcDateTime);
	}

	int IComparable.CompareTo(object obj)
	{
		if (obj == null)
		{
			return 1;
		}
		if (!(obj is DateTimeOffset { UtcDateTime: var utcDateTime }))
		{
			throw new ArgumentException(SR.Arg_MustBeDateTimeOffset);
		}
		DateTime utcDateTime2 = UtcDateTime;
		if (utcDateTime2 > utcDateTime)
		{
			return 1;
		}
		if (utcDateTime2 < utcDateTime)
		{
			return -1;
		}
		return 0;
	}

	public int CompareTo(DateTimeOffset other)
	{
		DateTime utcDateTime = other.UtcDateTime;
		DateTime utcDateTime2 = UtcDateTime;
		if (utcDateTime2 > utcDateTime)
		{
			return 1;
		}
		if (utcDateTime2 < utcDateTime)
		{
			return -1;
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is DateTimeOffset)
		{
			return UtcDateTime.Equals(((DateTimeOffset)obj).UtcDateTime);
		}
		return false;
	}

	public bool Equals(DateTimeOffset other)
	{
		return UtcDateTime.Equals(other.UtcDateTime);
	}

	public bool EqualsExact(DateTimeOffset other)
	{
		if (ClockDateTime == other.ClockDateTime && Offset == other.Offset)
		{
			return ClockDateTime.Kind == other.ClockDateTime.Kind;
		}
		return false;
	}

	public static bool Equals(DateTimeOffset first, DateTimeOffset second)
	{
		return DateTime.Equals(first.UtcDateTime, second.UtcDateTime);
	}

	public static DateTimeOffset FromFileTime(long fileTime)
	{
		return ToLocalTime(DateTime.FromFileTimeUtc(fileTime), throwOnOverflow: true);
	}

	public static DateTimeOffset FromUnixTimeSeconds(long seconds)
	{
		if (seconds < -62135596800L || seconds > 253402300799L)
		{
			throw new ArgumentOutOfRangeException("seconds", SR.Format(SR.ArgumentOutOfRange_Range, -62135596800L, 253402300799L));
		}
		long ticks = seconds * 10000000 + 621355968000000000L;
		return new DateTimeOffset(ticks, TimeSpan.Zero);
	}

	public static DateTimeOffset FromUnixTimeMilliseconds(long milliseconds)
	{
		if (milliseconds < -62135596800000L || milliseconds > 253402300799999L)
		{
			throw new ArgumentOutOfRangeException("milliseconds", SR.Format(SR.ArgumentOutOfRange_Range, -62135596800000L, 253402300799999L));
		}
		long ticks = milliseconds * 10000 + 621355968000000000L;
		return new DateTimeOffset(ticks, TimeSpan.Zero);
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
		try
		{
			ValidateOffset(Offset);
			ValidateDate(ClockDateTime, Offset);
		}
		catch (ArgumentException innerException)
		{
			throw new SerializationException(SR.Serialization_InvalidData, innerException);
		}
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("DateTime", _dateTime);
		info.AddValue("OffsetMinutes", _offsetMinutes);
	}

	private DateTimeOffset(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		_dateTime = (DateTime)info.GetValue("DateTime", typeof(DateTime));
		_offsetMinutes = (short)info.GetValue("OffsetMinutes", typeof(short));
	}

	public override int GetHashCode()
	{
		return UtcDateTime.GetHashCode();
	}

	public static DateTimeOffset Parse(string input)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		TimeSpan offset;
		return new DateTimeOffset(DateTimeParse.Parse(input, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out offset).Ticks, offset);
	}

	public static DateTimeOffset Parse(string input, IFormatProvider? formatProvider)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Parse(input, formatProvider, DateTimeStyles.None);
	}

	public static DateTimeOffset Parse(string input, IFormatProvider? formatProvider, DateTimeStyles styles)
	{
		styles = ValidateStyles(styles, "styles");
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		TimeSpan offset;
		return new DateTimeOffset(DateTimeParse.Parse(input, DateTimeFormatInfo.GetInstance(formatProvider), styles, out offset).Ticks, offset);
	}

	public static DateTimeOffset Parse(ReadOnlySpan<char> input, IFormatProvider? formatProvider = null, DateTimeStyles styles = DateTimeStyles.None)
	{
		styles = ValidateStyles(styles, "styles");
		TimeSpan offset;
		return new DateTimeOffset(DateTimeParse.Parse(input, DateTimeFormatInfo.GetInstance(formatProvider), styles, out offset).Ticks, offset);
	}

	public static DateTimeOffset ParseExact(string input, string format, IFormatProvider? formatProvider)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		if (format == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
		}
		return ParseExact(input, format, formatProvider, DateTimeStyles.None);
	}

	public static DateTimeOffset ParseExact(string input, string format, IFormatProvider? formatProvider, DateTimeStyles styles)
	{
		styles = ValidateStyles(styles, "styles");
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		if (format == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
		}
		TimeSpan offset;
		return new DateTimeOffset(DateTimeParse.ParseExact(input, format, DateTimeFormatInfo.GetInstance(formatProvider), styles, out offset).Ticks, offset);
	}

	public static DateTimeOffset ParseExact(ReadOnlySpan<char> input, ReadOnlySpan<char> format, IFormatProvider? formatProvider, DateTimeStyles styles = DateTimeStyles.None)
	{
		styles = ValidateStyles(styles, "styles");
		TimeSpan offset;
		return new DateTimeOffset(DateTimeParse.ParseExact(input, format, DateTimeFormatInfo.GetInstance(formatProvider), styles, out offset).Ticks, offset);
	}

	public static DateTimeOffset ParseExact(string input, string[] formats, IFormatProvider? formatProvider, DateTimeStyles styles)
	{
		styles = ValidateStyles(styles, "styles");
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		TimeSpan offset;
		return new DateTimeOffset(DateTimeParse.ParseExactMultiple(input, formats, DateTimeFormatInfo.GetInstance(formatProvider), styles, out offset).Ticks, offset);
	}

	public static DateTimeOffset ParseExact(ReadOnlySpan<char> input, string[] formats, IFormatProvider? formatProvider, DateTimeStyles styles = DateTimeStyles.None)
	{
		styles = ValidateStyles(styles, "styles");
		TimeSpan offset;
		return new DateTimeOffset(DateTimeParse.ParseExactMultiple(input, formats, DateTimeFormatInfo.GetInstance(formatProvider), styles, out offset).Ticks, offset);
	}

	public TimeSpan Subtract(DateTimeOffset value)
	{
		return UtcDateTime.Subtract(value.UtcDateTime);
	}

	public DateTimeOffset Subtract(TimeSpan value)
	{
		return new DateTimeOffset(ClockDateTime.Subtract(value), Offset);
	}

	public long ToFileTime()
	{
		return UtcDateTime.ToFileTime();
	}

	public long ToUnixTimeSeconds()
	{
		long num = UtcDateTime.Ticks / 10000000;
		return num - 62135596800L;
	}

	public long ToUnixTimeMilliseconds()
	{
		long num = UtcDateTime.Ticks / 10000;
		return num - 62135596800000L;
	}

	public DateTimeOffset ToLocalTime()
	{
		return ToLocalTime(throwOnOverflow: false);
	}

	internal DateTimeOffset ToLocalTime(bool throwOnOverflow)
	{
		return ToLocalTime(UtcDateTime, throwOnOverflow);
	}

	private static DateTimeOffset ToLocalTime(DateTime utcDateTime, bool throwOnOverflow)
	{
		TimeSpan localUtcOffset = TimeZoneInfo.GetLocalUtcOffset(utcDateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
		long num = utcDateTime.Ticks + localUtcOffset.Ticks;
		if (num < 0 || num > 3155378975999999999L)
		{
			if (throwOnOverflow)
			{
				throw new ArgumentException(SR.Arg_ArgumentOutOfRangeException);
			}
			num = ((num < 0) ? 0 : 3155378975999999999L);
		}
		return new DateTimeOffset(num, localUtcOffset);
	}

	public override string ToString()
	{
		return DateTimeFormat.Format(ClockDateTime, null, null, Offset);
	}

	public string ToString(string? format)
	{
		return DateTimeFormat.Format(ClockDateTime, format, null, Offset);
	}

	public string ToString(IFormatProvider? formatProvider)
	{
		return DateTimeFormat.Format(ClockDateTime, null, formatProvider, Offset);
	}

	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		return DateTimeFormat.Format(ClockDateTime, format, formatProvider, Offset);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? formatProvider = null)
	{
		return DateTimeFormat.TryFormat(ClockDateTime, destination, out charsWritten, format, formatProvider, Offset);
	}

	public DateTimeOffset ToUniversalTime()
	{
		return new DateTimeOffset(UtcDateTime);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, out DateTimeOffset result)
	{
		DateTime result2;
		TimeSpan offset;
		bool result3 = DateTimeParse.TryParse(input, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out result2, out offset);
		result = new DateTimeOffset(result2.Ticks, offset);
		return result3;
	}

	public static bool TryParse(ReadOnlySpan<char> input, out DateTimeOffset result)
	{
		DateTime result2;
		TimeSpan offset;
		bool result3 = DateTimeParse.TryParse(input, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out result2, out offset);
		result = new DateTimeOffset(result2.Ticks, offset);
		return result3;
	}

	public static bool TryParse([NotNullWhen(true)] string? input, IFormatProvider? formatProvider, DateTimeStyles styles, out DateTimeOffset result)
	{
		styles = ValidateStyles(styles, "styles");
		if (input == null)
		{
			result = default(DateTimeOffset);
			return false;
		}
		DateTime result2;
		TimeSpan offset;
		bool result3 = DateTimeParse.TryParse(input, DateTimeFormatInfo.GetInstance(formatProvider), styles, out result2, out offset);
		result = new DateTimeOffset(result2.Ticks, offset);
		return result3;
	}

	public static bool TryParse(ReadOnlySpan<char> input, IFormatProvider? formatProvider, DateTimeStyles styles, out DateTimeOffset result)
	{
		styles = ValidateStyles(styles, "styles");
		DateTime result2;
		TimeSpan offset;
		bool result3 = DateTimeParse.TryParse(input, DateTimeFormatInfo.GetInstance(formatProvider), styles, out result2, out offset);
		result = new DateTimeOffset(result2.Ticks, offset);
		return result3;
	}

	public static bool TryParseExact([NotNullWhen(true)] string? input, [NotNullWhen(true)] string? format, IFormatProvider? formatProvider, DateTimeStyles styles, out DateTimeOffset result)
	{
		styles = ValidateStyles(styles, "styles");
		if (input == null || format == null)
		{
			result = default(DateTimeOffset);
			return false;
		}
		DateTime result2;
		TimeSpan offset;
		bool result3 = DateTimeParse.TryParseExact(input, format, DateTimeFormatInfo.GetInstance(formatProvider), styles, out result2, out offset);
		result = new DateTimeOffset(result2.Ticks, offset);
		return result3;
	}

	public static bool TryParseExact(ReadOnlySpan<char> input, ReadOnlySpan<char> format, IFormatProvider? formatProvider, DateTimeStyles styles, out DateTimeOffset result)
	{
		styles = ValidateStyles(styles, "styles");
		DateTime result2;
		TimeSpan offset;
		bool result3 = DateTimeParse.TryParseExact(input, format, DateTimeFormatInfo.GetInstance(formatProvider), styles, out result2, out offset);
		result = new DateTimeOffset(result2.Ticks, offset);
		return result3;
	}

	public static bool TryParseExact([NotNullWhen(true)] string? input, [NotNullWhen(true)] string?[]? formats, IFormatProvider? formatProvider, DateTimeStyles styles, out DateTimeOffset result)
	{
		styles = ValidateStyles(styles, "styles");
		if (input == null)
		{
			result = default(DateTimeOffset);
			return false;
		}
		DateTime result2;
		TimeSpan offset;
		bool result3 = DateTimeParse.TryParseExactMultiple(input, formats, DateTimeFormatInfo.GetInstance(formatProvider), styles, out result2, out offset);
		result = new DateTimeOffset(result2.Ticks, offset);
		return result3;
	}

	public static bool TryParseExact(ReadOnlySpan<char> input, [NotNullWhen(true)] string?[]? formats, IFormatProvider? formatProvider, DateTimeStyles styles, out DateTimeOffset result)
	{
		styles = ValidateStyles(styles, "styles");
		DateTime result2;
		TimeSpan offset;
		bool result3 = DateTimeParse.TryParseExactMultiple(input, formats, DateTimeFormatInfo.GetInstance(formatProvider), styles, out result2, out offset);
		result = new DateTimeOffset(result2.Ticks, offset);
		return result3;
	}

	private static short ValidateOffset(TimeSpan offset)
	{
		long ticks = offset.Ticks;
		if (ticks % 600000000 != 0L)
		{
			throw new ArgumentException(SR.Argument_OffsetPrecision, "offset");
		}
		if (ticks < -504000000000L || ticks > 504000000000L)
		{
			throw new ArgumentOutOfRangeException("offset", SR.Argument_OffsetOutOfRange);
		}
		return (short)(offset.Ticks / 600000000);
	}

	private static DateTime ValidateDate(DateTime dateTime, TimeSpan offset)
	{
		long num = dateTime.Ticks - offset.Ticks;
		if (num < 0 || num > 3155378975999999999L)
		{
			throw new ArgumentOutOfRangeException("offset", SR.Argument_UTCOutOfRange);
		}
		return new DateTime(num, DateTimeKind.Unspecified);
	}

	private static DateTimeStyles ValidateStyles(DateTimeStyles style, string parameterName)
	{
		if (((uint)style & 0xFFFFFF00u) != 0)
		{
			throw new ArgumentException(SR.Argument_InvalidDateTimeStyles, parameterName);
		}
		if ((style & DateTimeStyles.AssumeLocal) != 0 && (style & DateTimeStyles.AssumeUniversal) != 0)
		{
			throw new ArgumentException(SR.Argument_ConflictingDateTimeStyles, parameterName);
		}
		if ((style & DateTimeStyles.NoCurrentDateDefault) != 0)
		{
			throw new ArgumentException(SR.Argument_DateTimeOffsetInvalidDateTimeStyles, parameterName);
		}
		style &= ~DateTimeStyles.RoundtripKind;
		style &= ~DateTimeStyles.AssumeLocal;
		return style;
	}

	public static implicit operator DateTimeOffset(DateTime dateTime)
	{
		return new DateTimeOffset(dateTime);
	}

	public static DateTimeOffset operator +(DateTimeOffset dateTimeOffset, TimeSpan timeSpan)
	{
		return new DateTimeOffset(dateTimeOffset.ClockDateTime + timeSpan, dateTimeOffset.Offset);
	}

	public static DateTimeOffset operator -(DateTimeOffset dateTimeOffset, TimeSpan timeSpan)
	{
		return new DateTimeOffset(dateTimeOffset.ClockDateTime - timeSpan, dateTimeOffset.Offset);
	}

	public static TimeSpan operator -(DateTimeOffset left, DateTimeOffset right)
	{
		return left.UtcDateTime - right.UtcDateTime;
	}

	public static bool operator ==(DateTimeOffset left, DateTimeOffset right)
	{
		return left.UtcDateTime == right.UtcDateTime;
	}

	public static bool operator !=(DateTimeOffset left, DateTimeOffset right)
	{
		return left.UtcDateTime != right.UtcDateTime;
	}

	public static bool operator <(DateTimeOffset left, DateTimeOffset right)
	{
		return left.UtcDateTime < right.UtcDateTime;
	}

	public static bool operator <=(DateTimeOffset left, DateTimeOffset right)
	{
		return left.UtcDateTime <= right.UtcDateTime;
	}

	public static bool operator >(DateTimeOffset left, DateTimeOffset right)
	{
		return left.UtcDateTime > right.UtcDateTime;
	}

	public static bool operator >=(DateTimeOffset left, DateTimeOffset right)
	{
		return left.UtcDateTime >= right.UtcDateTime;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTimeOffset IAdditionOperators<DateTimeOffset, TimeSpan, DateTimeOffset>.operator +(DateTimeOffset left, TimeSpan right)
	{
		return left + right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateTimeOffset, DateTimeOffset>.operator <(DateTimeOffset left, DateTimeOffset right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateTimeOffset, DateTimeOffset>.operator <=(DateTimeOffset left, DateTimeOffset right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateTimeOffset, DateTimeOffset>.operator >(DateTimeOffset left, DateTimeOffset right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateTimeOffset, DateTimeOffset>.operator >=(DateTimeOffset left, DateTimeOffset right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<DateTimeOffset, DateTimeOffset>.operator ==(DateTimeOffset left, DateTimeOffset right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<DateTimeOffset, DateTimeOffset>.operator !=(DateTimeOffset left, DateTimeOffset right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTimeOffset IParseable<DateTimeOffset>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<DateTimeOffset>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out DateTimeOffset result)
	{
		return TryParse(s, provider, DateTimeStyles.None, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTimeOffset ISpanParseable<DateTimeOffset>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<DateTimeOffset>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out DateTimeOffset result)
	{
		return TryParse(s, provider, DateTimeStyles.None, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTimeOffset ISubtractionOperators<DateTimeOffset, TimeSpan, DateTimeOffset>.operator -(DateTimeOffset left, TimeSpan right)
	{
		return left - right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TimeSpan ISubtractionOperators<DateTimeOffset, DateTimeOffset, TimeSpan>.operator -(DateTimeOffset left, DateTimeOffset right)
	{
		return left - right;
	}
}
