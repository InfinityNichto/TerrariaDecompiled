using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Threading;

namespace System;

[Serializable]
[StructLayout(LayoutKind.Auto)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct DateTime : IComparable, ISpanFormattable, IFormattable, IConvertible, IComparable<DateTime>, IEquatable<DateTime>, ISerializable, IAdditionOperators<DateTime, TimeSpan, DateTime>, IAdditiveIdentity<DateTime, TimeSpan>, IComparisonOperators<DateTime, DateTime>, IEqualityOperators<DateTime, DateTime>, IMinMaxValue<DateTime>, ISpanParseable<DateTime>, IParseable<DateTime>, ISubtractionOperators<DateTime, TimeSpan, DateTime>, ISubtractionOperators<DateTime, DateTime, TimeSpan>
{
	private sealed class LeapSecondCache
	{
		internal ulong OSFileTimeTicksAtStartOfValidityWindow;

		internal ulong DotnetDateDataAtStartOfValidityWindow;
	}

	private static readonly uint[] s_daysToMonth365 = new uint[13]
	{
		0u, 31u, 59u, 90u, 120u, 151u, 181u, 212u, 243u, 273u,
		304u, 334u, 365u
	};

	private static readonly uint[] s_daysToMonth366 = new uint[13]
	{
		0u, 31u, 60u, 91u, 121u, 152u, 182u, 213u, 244u, 274u,
		305u, 335u, 366u
	};

	public static readonly DateTime MinValue;

	public static readonly DateTime MaxValue = new DateTime(3155378975999999999L, DateTimeKind.Unspecified);

	public static readonly DateTime UnixEpoch = new DateTime(621355968000000000L, DateTimeKind.Utc);

	private readonly ulong _dateData;

	internal static readonly bool s_systemSupportsLeapSeconds = SystemSupportsLeapSeconds();

	private unsafe static readonly delegate* unmanaged[SuppressGCTransition]<ulong*, void> s_pfnGetSystemTimeAsFileTime = GetGetSystemTimeAsFileTimeFnPtr();

	private static LeapSecondCache s_leapSecondCache = new LeapSecondCache();

	private static ReadOnlySpan<byte> DaysInMonth365 => new byte[12]
	{
		31, 28, 31, 30, 31, 30, 31, 31, 30, 31,
		30, 31
	};

	private static ReadOnlySpan<byte> DaysInMonth366 => new byte[12]
	{
		31, 29, 31, 30, 31, 30, 31, 31, 30, 31,
		30, 31
	};

	private ulong UTicks => _dateData & 0x3FFFFFFFFFFFFFFFuL;

	private ulong InternalKind => _dateData & 0xC000000000000000uL;

	public DateTime Date
	{
		get
		{
			ulong uTicks = UTicks;
			return new DateTime((uTicks - uTicks % 864000000000L) | InternalKind);
		}
	}

	public int Day => GetDatePart(3);

	public DayOfWeek DayOfWeek => (DayOfWeek)((uint)((int)(UTicks / 864000000000L) + 1) % 7u);

	public int DayOfYear => GetDatePart(1);

	public int Hour => (int)((uint)(UTicks / 36000000000L) % 24);

	public DateTimeKind Kind
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return InternalKind switch
			{
				0uL => DateTimeKind.Unspecified, 
				4611686018427387904uL => DateTimeKind.Utc, 
				_ => DateTimeKind.Local, 
			};
		}
	}

	public int Millisecond => (int)(UTicks / 10000 % 1000);

	public int Minute => (int)(UTicks / 600000000 % 60);

	public int Month => GetDatePart(2);

	public static DateTime Now
	{
		get
		{
			DateTime utcNow = UtcNow;
			bool isAmbiguousLocalDst;
			long ticks = TimeZoneInfo.GetDateTimeNowUtcOffsetFromUtc(utcNow, out isAmbiguousLocalDst).Ticks;
			long num = utcNow.Ticks + ticks;
			if ((ulong)num <= 3155378975999999999uL)
			{
				if (!isAmbiguousLocalDst)
				{
					return new DateTime((ulong)num | 0x8000000000000000uL);
				}
				return new DateTime((ulong)num | 0xC000000000000000uL);
			}
			return new DateTime((num < 0) ? 9223372036854775808uL : 12378751012854775807uL);
		}
	}

	public int Second => (int)(UTicks / 10000000 % 60);

	public long Ticks => (long)(_dateData & 0x3FFFFFFFFFFFFFFFL);

	public TimeSpan TimeOfDay => new TimeSpan((long)(UTicks % 864000000000L));

	public static DateTime Today => Now.Date;

	public int Year => GetDatePart(0);

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TimeSpan IAdditiveIdentity<DateTime, TimeSpan>.AdditiveIdentity => default(TimeSpan);

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTime IMinMaxValue<DateTime>.MinValue => MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTime IMinMaxValue<DateTime>.MaxValue => MaxValue;

	public unsafe static DateTime UtcNow
	{
		get
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out ulong num);
			s_pfnGetSystemTimeAsFileTime(&num);
			ulong num2 = num;
			if (s_systemSupportsLeapSeconds)
			{
				LeapSecondCache leapSecondCache = s_leapSecondCache;
				ulong num3 = num2 - leapSecondCache.OSFileTimeTicksAtStartOfValidityWindow;
				if (num3 < 3000000000u)
				{
					return new DateTime(leapSecondCache.DotnetDateDataAtStartOfValidityWindow + num3);
				}
				return UpdateLeapSecondCacheAndReturnUtcNow();
			}
			return new DateTime(num2 + 5116597250427387904L);
		}
	}

	public DateTime(long ticks)
	{
		if ((ulong)ticks > 3155378975999999999uL)
		{
			ThrowTicksOutOfRange();
		}
		_dateData = (ulong)ticks;
	}

	private DateTime(ulong dateData)
	{
		_dateData = dateData;
	}

	internal static DateTime UnsafeCreate(long ticks)
	{
		return new DateTime((ulong)ticks);
	}

	public DateTime(long ticks, DateTimeKind kind)
	{
		if ((ulong)ticks > 3155378975999999999uL)
		{
			ThrowTicksOutOfRange();
		}
		if ((uint)kind > 2u)
		{
			ThrowInvalidKind();
		}
		_dateData = (ulong)ticks | ((ulong)(uint)kind << 62);
	}

	internal DateTime(long ticks, DateTimeKind kind, bool isAmbiguousDst)
	{
		if ((ulong)ticks > 3155378975999999999uL)
		{
			ThrowTicksOutOfRange();
		}
		_dateData = (ulong)ticks | (isAmbiguousDst ? 13835058055282163712uL : 9223372036854775808uL);
	}

	private static void ThrowTicksOutOfRange()
	{
		throw new ArgumentOutOfRangeException("ticks", SR.ArgumentOutOfRange_DateTimeBadTicks);
	}

	private static void ThrowInvalidKind()
	{
		throw new ArgumentException(SR.Argument_InvalidDateTimeKind, "kind");
	}

	private static void ThrowMillisecondOutOfRange()
	{
		throw new ArgumentOutOfRangeException("millisecond", SR.Format(SR.ArgumentOutOfRange_Range, 0, 999));
	}

	private static void ThrowDateArithmetic(int param)
	{
		throw new ArgumentOutOfRangeException(param switch
		{
			0 => "value", 
			1 => "t", 
			_ => "months", 
		}, SR.ArgumentOutOfRange_DateArithmetic);
	}

	public DateTime(int year, int month, int day)
	{
		_dateData = DateToTicks(year, month, day);
	}

	public DateTime(int year, int month, int day, Calendar calendar)
		: this(year, month, day, 0, 0, 0, calendar)
	{
	}

	public DateTime(int year, int month, int day, int hour, int minute, int second)
	{
		if (second != 60 || !s_systemSupportsLeapSeconds)
		{
			_dateData = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
			return;
		}
		this = new DateTime(year, month, day, hour, minute, 59);
		ValidateLeapSecond();
	}

	public DateTime(int year, int month, int day, int hour, int minute, int second, DateTimeKind kind)
	{
		if ((uint)kind > 2u)
		{
			ThrowInvalidKind();
		}
		if (second != 60 || !s_systemSupportsLeapSeconds)
		{
			ulong num = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
			_dateData = num | (ulong)((long)kind << 62);
		}
		else
		{
			this = new DateTime(year, month, day, hour, minute, 59, kind);
			ValidateLeapSecond();
		}
	}

	public DateTime(int year, int month, int day, int hour, int minute, int second, Calendar calendar)
	{
		if (calendar == null)
		{
			throw new ArgumentNullException("calendar");
		}
		if (second != 60 || !s_systemSupportsLeapSeconds)
		{
			_dateData = calendar.ToDateTime(year, month, day, hour, minute, second, 0).UTicks;
			return;
		}
		this = new DateTime(year, month, day, hour, minute, 59, calendar);
		ValidateLeapSecond();
	}

	public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
	{
		if ((uint)millisecond >= 1000u)
		{
			ThrowMillisecondOutOfRange();
		}
		if (second != 60 || !s_systemSupportsLeapSeconds)
		{
			ulong num = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
			num += (uint)(millisecond * 10000);
			_dateData = num;
		}
		else
		{
			this = new DateTime(year, month, day, hour, minute, 59, millisecond);
			ValidateLeapSecond();
		}
	}

	public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind kind)
	{
		if ((uint)millisecond >= 1000u)
		{
			ThrowMillisecondOutOfRange();
		}
		if ((uint)kind > 2u)
		{
			ThrowInvalidKind();
		}
		if (second != 60 || !s_systemSupportsLeapSeconds)
		{
			ulong num = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
			num += (uint)(millisecond * 10000);
			_dateData = num | (ulong)((long)kind << 62);
		}
		else
		{
			this = new DateTime(year, month, day, hour, minute, 59, millisecond, kind);
			ValidateLeapSecond();
		}
	}

	public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar)
	{
		if (calendar == null)
		{
			throw new ArgumentNullException("calendar");
		}
		if (second != 60 || !s_systemSupportsLeapSeconds)
		{
			_dateData = calendar.ToDateTime(year, month, day, hour, minute, second, millisecond).UTicks;
			return;
		}
		this = new DateTime(year, month, day, hour, minute, 59, millisecond, calendar);
		ValidateLeapSecond();
	}

	public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar, DateTimeKind kind)
	{
		if (calendar == null)
		{
			throw new ArgumentNullException("calendar");
		}
		if ((uint)millisecond >= 1000u)
		{
			ThrowMillisecondOutOfRange();
		}
		if ((uint)kind > 2u)
		{
			ThrowInvalidKind();
		}
		if (second != 60 || !s_systemSupportsLeapSeconds)
		{
			ulong uTicks = calendar.ToDateTime(year, month, day, hour, minute, second, millisecond).UTicks;
			_dateData = uTicks | (ulong)((long)kind << 62);
		}
		else
		{
			this = new DateTime(year, month, day, hour, minute, 59, millisecond, calendar, kind);
			ValidateLeapSecond();
		}
	}

	private void ValidateLeapSecond()
	{
		if (!IsValidTimeWithLeapSeconds(Year, Month, Day, Hour, Minute, Kind))
		{
			ThrowHelper.ThrowArgumentOutOfRange_BadHourMinuteSecond();
		}
	}

	private DateTime(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
		}
		bool flag = false;
		SerializationInfoEnumerator enumerator = info.GetEnumerator();
		while (true)
		{
			if (enumerator.MoveNext())
			{
				string name = enumerator.Name;
				if (!(name == "ticks"))
				{
					if (name == "dateData")
					{
						_dateData = Convert.ToUInt64(enumerator.Value, CultureInfo.InvariantCulture);
						break;
					}
				}
				else
				{
					_dateData = (ulong)Convert.ToInt64(enumerator.Value, CultureInfo.InvariantCulture);
					flag = true;
				}
				continue;
			}
			if (flag)
			{
				break;
			}
			throw new SerializationException(SR.Serialization_MissingDateTimeData);
		}
		if (UTicks > 3155378975999999999L)
		{
			throw new SerializationException(SR.Serialization_DateTimeTicksOutOfRange);
		}
	}

	public DateTime Add(TimeSpan value)
	{
		return AddTicks(value._ticks);
	}

	private DateTime Add(double value, int scale)
	{
		double num = value * (double)scale + ((value >= 0.0) ? 0.5 : (-0.5));
		if (num <= -315537897600000.0 || num >= 315537897600000.0)
		{
			ThrowOutOfRange();
		}
		return AddTicks((long)num * 10000);
		static void ThrowOutOfRange()
		{
			throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_AddValue);
		}
	}

	public DateTime AddDays(double value)
	{
		return Add(value, 86400000);
	}

	public DateTime AddHours(double value)
	{
		return Add(value, 3600000);
	}

	public DateTime AddMilliseconds(double value)
	{
		return Add(value, 1);
	}

	public DateTime AddMinutes(double value)
	{
		return Add(value, 60000);
	}

	public DateTime AddMonths(int months)
	{
		if (months < -120000 || months > 120000)
		{
			throw new ArgumentOutOfRangeException("months", SR.ArgumentOutOfRange_DateTimeBadMonths);
		}
		GetDate(out var year, out var month, out var day);
		int num = year;
		int num2 = day;
		int num3 = month + months;
		if (num3 > 0)
		{
			int num4 = (int)((uint)(num3 - 1) / 12u);
			num += num4;
			num3 -= num4 * 12;
		}
		else
		{
			num += num3 / 12 - 1;
			num3 = 12 + num3 % 12;
		}
		if (num < 1 || num > 9999)
		{
			ThrowDateArithmetic(2);
		}
		uint[] array = (IsLeapYear(num) ? s_daysToMonth366 : s_daysToMonth365);
		uint num5 = array[num3 - 1];
		int num6 = (int)(array[num3] - num5);
		if (num2 > num6)
		{
			num2 = num6;
		}
		uint num7 = (uint)((int)(DaysToYear((uint)num) + num5) + num2 - 1);
		return new DateTime(((ulong)((long)num7 * 864000000000L) + UTicks % 864000000000L) | InternalKind);
	}

	public DateTime AddSeconds(double value)
	{
		return Add(value, 1000);
	}

	public DateTime AddTicks(long value)
	{
		ulong num = (ulong)(Ticks + value);
		if (num > 3155378975999999999L)
		{
			ThrowDateArithmetic(0);
		}
		return new DateTime(num | InternalKind);
	}

	internal bool TryAddTicks(long value, out DateTime result)
	{
		ulong num = (ulong)(Ticks + value);
		if (num > 3155378975999999999L)
		{
			result = default(DateTime);
			return false;
		}
		result = new DateTime(num | InternalKind);
		return true;
	}

	public DateTime AddYears(int value)
	{
		if (value < -10000 || value > 10000)
		{
			throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_DateTimeBadYears);
		}
		GetDate(out var year, out var month, out var day);
		int num = year + value;
		if (num < 1 || num > 9999)
		{
			ThrowDateArithmetic(0);
		}
		uint num2 = DaysToYear((uint)num);
		int num3 = month - 1;
		int num4 = day - 1;
		if (IsLeapYear(num))
		{
			num2 += s_daysToMonth366[num3];
		}
		else
		{
			if (num4 == 28 && num3 == 1)
			{
				num4--;
			}
			num2 += s_daysToMonth365[num3];
		}
		num2 += (uint)num4;
		return new DateTime(((ulong)((long)num2 * 864000000000L) + UTicks % 864000000000L) | InternalKind);
	}

	public static int Compare(DateTime t1, DateTime t2)
	{
		long ticks = t1.Ticks;
		long ticks2 = t2.Ticks;
		if (ticks > ticks2)
		{
			return 1;
		}
		if (ticks < ticks2)
		{
			return -1;
		}
		return 0;
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (!(value is DateTime))
		{
			throw new ArgumentException(SR.Arg_MustBeDateTime);
		}
		return Compare(this, (DateTime)value);
	}

	public int CompareTo(DateTime value)
	{
		return Compare(this, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong DateToTicks(int year, int month, int day)
	{
		if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
		{
			ThrowHelper.ThrowArgumentOutOfRange_BadYearMonthDay();
		}
		uint[] array = (IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365);
		if ((uint)day > array[month] - array[month - 1])
		{
			ThrowHelper.ThrowArgumentOutOfRange_BadYearMonthDay();
		}
		uint num = (uint)((int)(DaysToYear((uint)year) + array[month - 1]) + day - 1);
		return (ulong)num * 864000000000uL;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint DaysToYear(uint year)
	{
		uint num = year - 1;
		uint num2 = num / 100;
		return num * 1461 / 4 - num2 + num2 / 4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong TimeToTicks(int hour, int minute, int second)
	{
		if ((uint)hour >= 24u || (uint)minute >= 60u || (uint)second >= 60u)
		{
			ThrowHelper.ThrowArgumentOutOfRange_BadHourMinuteSecond();
		}
		int num = hour * 3600 + minute * 60 + second;
		return (ulong)(uint)num * 10000000uL;
	}

	internal static ulong TimeToTicks(int hour, int minute, int second, int millisecond)
	{
		ulong num = TimeToTicks(hour, minute, second);
		if ((uint)millisecond >= 1000u)
		{
			ThrowMillisecondOutOfRange();
		}
		return num + (uint)(millisecond * 10000);
	}

	public static int DaysInMonth(int year, int month)
	{
		if (month < 1 || month > 12)
		{
			ThrowHelper.ThrowArgumentOutOfRange_Month(month);
		}
		return (IsLeapYear(year) ? DaysInMonth366 : DaysInMonth365)[month - 1];
	}

	internal static long DoubleDateToTicks(double value)
	{
		if (!(value < 2958466.0) || !(value > -657435.0))
		{
			throw new ArgumentException(SR.Arg_OleAutDateInvalid);
		}
		long num = (long)(value * 86400000.0 + ((value >= 0.0) ? 0.5 : (-0.5)));
		if (num < 0)
		{
			num -= num % 86400000 * 2;
		}
		num += 59926435200000L;
		if (num < 0 || num >= 315537897600000L)
		{
			throw new ArgumentException(SR.Arg_OleAutDateScale);
		}
		return num * 10000;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is DateTime)
		{
			return Ticks == ((DateTime)value).Ticks;
		}
		return false;
	}

	public bool Equals(DateTime value)
	{
		return Ticks == value.Ticks;
	}

	public static bool Equals(DateTime t1, DateTime t2)
	{
		return t1.Ticks == t2.Ticks;
	}

	public static DateTime FromBinary(long dateData)
	{
		if ((dateData & long.MinValue) != 0L)
		{
			long num = dateData & 0x3FFFFFFFFFFFFFFFL;
			if (num > 4611685154427387904L)
			{
				num -= 4611686018427387904L;
			}
			bool isAmbiguousLocalDst = false;
			long ticks;
			if (num < 0)
			{
				ticks = TimeZoneInfo.GetLocalUtcOffset(MinValue, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
			}
			else if (num > 3155378975999999999L)
			{
				ticks = TimeZoneInfo.GetLocalUtcOffset(MaxValue, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
			}
			else
			{
				DateTime time = new DateTime(num, DateTimeKind.Utc);
				ticks = TimeZoneInfo.GetUtcOffsetFromUtc(time, TimeZoneInfo.Local, out var _, out isAmbiguousLocalDst).Ticks;
			}
			num += ticks;
			if (num < 0)
			{
				num += 864000000000L;
			}
			if ((ulong)num > 3155378975999999999uL)
			{
				throw new ArgumentException(SR.Argument_DateTimeBadBinaryData, "dateData");
			}
			return new DateTime(num, DateTimeKind.Local, isAmbiguousLocalDst);
		}
		if ((ulong)(dateData & 0x3FFFFFFFFFFFFFFFL) > 3155378975999999999uL)
		{
			throw new ArgumentException(SR.Argument_DateTimeBadBinaryData, "dateData");
		}
		return new DateTime((ulong)dateData);
	}

	public static DateTime FromFileTime(long fileTime)
	{
		return FromFileTimeUtc(fileTime).ToLocalTime();
	}

	public static DateTime FromFileTimeUtc(long fileTime)
	{
		if ((ulong)fileTime > 2650467743999999999uL)
		{
			throw new ArgumentOutOfRangeException("fileTime", SR.ArgumentOutOfRange_FileTimeInvalid);
		}
		if (s_systemSupportsLeapSeconds)
		{
			return FromFileTimeLeapSecondsAware((ulong)fileTime);
		}
		ulong num = (ulong)(fileTime + 504911232000000000L);
		return new DateTime(num | 0x4000000000000000uL);
	}

	public static DateTime FromOADate(double d)
	{
		return new DateTime(DoubleDateToTicks(d), DateTimeKind.Unspecified);
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
		}
		info.AddValue("ticks", Ticks);
		info.AddValue("dateData", _dateData);
	}

	public bool IsDaylightSavingTime()
	{
		if (InternalKind == 4611686018427387904L)
		{
			return false;
		}
		return TimeZoneInfo.Local.IsDaylightSavingTime(this, TimeZoneInfoOptions.NoThrowOnInvalidTime);
	}

	public static DateTime SpecifyKind(DateTime value, DateTimeKind kind)
	{
		if ((uint)kind > 2u)
		{
			ThrowInvalidKind();
		}
		return new DateTime(value.UTicks | (ulong)((long)kind << 62));
	}

	public long ToBinary()
	{
		if ((_dateData & 0x8000000000000000uL) != 0L)
		{
			TimeSpan localUtcOffset = TimeZoneInfo.GetLocalUtcOffset(this, TimeZoneInfoOptions.NoThrowOnInvalidTime);
			long ticks = Ticks;
			long num = ticks - localUtcOffset.Ticks;
			if (num < 0)
			{
				num = 4611686018427387904L + num;
			}
			return num | long.MinValue;
		}
		return (long)_dateData;
	}

	private int GetDatePart(int part)
	{
		uint num = (uint)(UTicks / 864000000000L);
		uint num2 = num / 146097;
		num -= num2 * 146097;
		uint num3 = num / 36524;
		if (num3 == 4)
		{
			num3 = 3u;
		}
		num -= num3 * 36524;
		uint num4 = num / 1461;
		num -= num4 * 1461;
		uint num5 = num / 365;
		if (num5 == 4)
		{
			num5 = 3u;
		}
		if (part == 0)
		{
			return (int)(num2 * 400 + num3 * 100 + num4 * 4 + num5 + 1);
		}
		num -= num5 * 365;
		if (part == 1)
		{
			return (int)(num + 1);
		}
		uint[] array = ((num5 == 3 && (num4 != 24 || num3 == 3)) ? s_daysToMonth366 : s_daysToMonth365);
		uint num6;
		for (num6 = (num >> 5) + 1; num >= array[num6]; num6++)
		{
		}
		if (part == 2)
		{
			return (int)num6;
		}
		return (int)(num - array[num6 - 1] + 1);
	}

	internal void GetDate(out int year, out int month, out int day)
	{
		uint num = (uint)(UTicks / 864000000000L);
		uint num2 = num / 146097;
		num -= num2 * 146097;
		uint num3 = num / 36524;
		if (num3 == 4)
		{
			num3 = 3u;
		}
		num -= num3 * 36524;
		uint num4 = num / 1461;
		num -= num4 * 1461;
		uint num5 = num / 365;
		if (num5 == 4)
		{
			num5 = 3u;
		}
		year = (int)(num2 * 400 + num3 * 100 + num4 * 4 + num5 + 1);
		num -= num5 * 365;
		uint[] array = ((num5 == 3 && (num4 != 24 || num3 == 3)) ? s_daysToMonth366 : s_daysToMonth365);
		uint num6;
		for (num6 = (num >> 5) + 1; num >= array[num6]; num6++)
		{
		}
		month = (int)num6;
		day = (int)(num - array[num6 - 1] + 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void GetTime(out int hour, out int minute, out int second)
	{
		ulong num = UTicks / 10000000;
		ulong num2 = num / 60;
		second = (int)(num - num2 * 60);
		ulong num3 = num2 / 60;
		minute = (int)(num2 - num3 * 60);
		hour = (int)((uint)num3 % 24);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void GetTime(out int hour, out int minute, out int second, out int millisecond)
	{
		ulong num = UTicks / 10000;
		ulong num2 = num / 1000;
		millisecond = (int)(num - num2 * 1000);
		ulong num3 = num2 / 60;
		second = (int)(num2 - num3 * 60);
		ulong num4 = num3 / 60;
		minute = (int)(num3 - num4 * 60);
		hour = (int)((uint)num4 % 24);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void GetTimePrecise(out int hour, out int minute, out int second, out int tick)
	{
		ulong uTicks = UTicks;
		ulong num = uTicks / 10000000;
		tick = (int)(uTicks - num * 10000000);
		ulong num2 = num / 60;
		second = (int)(num - num2 * 60);
		ulong num3 = num2 / 60;
		minute = (int)(num2 - num3 * 60);
		hour = (int)((uint)num3 % 24);
	}

	public override int GetHashCode()
	{
		long ticks = Ticks;
		return (int)ticks ^ (int)(ticks >> 32);
	}

	internal bool IsAmbiguousDaylightSavingTime()
	{
		return InternalKind == 13835058055282163712uL;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsLeapYear(int year)
	{
		if (year < 1 || year > 9999)
		{
			ThrowHelper.ThrowArgumentOutOfRange_Year();
		}
		if (((uint)year & 3u) != 0)
		{
			return false;
		}
		if ((year & 0xF) == 0)
		{
			return true;
		}
		if ((uint)year % 25u == 0)
		{
			return false;
		}
		return true;
	}

	public static DateTime Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return DateTimeParse.Parse(s, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None);
	}

	public static DateTime Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return DateTimeParse.Parse(s, DateTimeFormatInfo.GetInstance(provider), DateTimeStyles.None);
	}

	public static DateTime Parse(string s, IFormatProvider? provider, DateTimeStyles styles)
	{
		DateTimeFormatInfo.ValidateStyles(styles, styles: true);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return DateTimeParse.Parse(s, DateTimeFormatInfo.GetInstance(provider), styles);
	}

	public static DateTime Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null, DateTimeStyles styles = DateTimeStyles.None)
	{
		DateTimeFormatInfo.ValidateStyles(styles, styles: true);
		return DateTimeParse.Parse(s, DateTimeFormatInfo.GetInstance(provider), styles);
	}

	public static DateTime ParseExact(string s, string format, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if (format == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
		}
		return DateTimeParse.ParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), DateTimeStyles.None);
	}

	public static DateTime ParseExact(string s, string format, IFormatProvider? provider, DateTimeStyles style)
	{
		DateTimeFormatInfo.ValidateStyles(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if (format == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
		}
		return DateTimeParse.ParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style);
	}

	public static DateTime ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		DateTimeFormatInfo.ValidateStyles(style);
		return DateTimeParse.ParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style);
	}

	public static DateTime ParseExact(string s, string[] formats, IFormatProvider? provider, DateTimeStyles style)
	{
		DateTimeFormatInfo.ValidateStyles(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return DateTimeParse.ParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style);
	}

	public static DateTime ParseExact(ReadOnlySpan<char> s, string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		DateTimeFormatInfo.ValidateStyles(style);
		return DateTimeParse.ParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style);
	}

	public TimeSpan Subtract(DateTime value)
	{
		return new TimeSpan(Ticks - value.Ticks);
	}

	public DateTime Subtract(TimeSpan value)
	{
		ulong num = (ulong)(Ticks - value._ticks);
		if (num > 3155378975999999999L)
		{
			ThrowDateArithmetic(0);
		}
		return new DateTime(num | InternalKind);
	}

	private static double TicksToOADate(long value)
	{
		if (value == 0L)
		{
			return 0.0;
		}
		if (value < 864000000000L)
		{
			value += 599264352000000000L;
		}
		if (value < 31241376000000000L)
		{
			throw new OverflowException(SR.Arg_OleAutDateInvalid);
		}
		long num = (value - 599264352000000000L) / 10000;
		if (num < 0)
		{
			long num2 = num % 86400000;
			if (num2 != 0L)
			{
				num -= (86400000 + num2) * 2;
			}
		}
		return (double)num / 86400000.0;
	}

	public double ToOADate()
	{
		return TicksToOADate(Ticks);
	}

	public long ToFileTime()
	{
		return ToUniversalTime().ToFileTimeUtc();
	}

	public long ToFileTimeUtc()
	{
		long num = (((_dateData & 0x8000000000000000uL) != 0L) ? ToUniversalTime().Ticks : Ticks);
		if (s_systemSupportsLeapSeconds)
		{
			return (long)ToFileTimeLeapSecondsAware(num);
		}
		num -= 504911232000000000L;
		if (num < 0)
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_FileTimeInvalid);
		}
		return num;
	}

	public DateTime ToLocalTime()
	{
		if ((_dateData & 0x8000000000000000uL) != 0L)
		{
			return this;
		}
		bool isDaylightSavings;
		bool isAmbiguousLocalDst;
		long ticks = TimeZoneInfo.GetUtcOffsetFromUtc(this, TimeZoneInfo.Local, out isDaylightSavings, out isAmbiguousLocalDst).Ticks;
		long num = Ticks + ticks;
		if ((ulong)num <= 3155378975999999999uL)
		{
			if (!isAmbiguousLocalDst)
			{
				return new DateTime((ulong)num | 0x8000000000000000uL);
			}
			return new DateTime((ulong)num | 0xC000000000000000uL);
		}
		return new DateTime((num < 0) ? 9223372036854775808uL : 12378751012854775807uL);
	}

	public string ToLongDateString()
	{
		return DateTimeFormat.Format(this, "D", null);
	}

	public string ToLongTimeString()
	{
		return DateTimeFormat.Format(this, "T", null);
	}

	public string ToShortDateString()
	{
		return DateTimeFormat.Format(this, "d", null);
	}

	public string ToShortTimeString()
	{
		return DateTimeFormat.Format(this, "t", null);
	}

	public override string ToString()
	{
		return DateTimeFormat.Format(this, null, null);
	}

	public string ToString(string? format)
	{
		return DateTimeFormat.Format(this, format, null);
	}

	public string ToString(IFormatProvider? provider)
	{
		return DateTimeFormat.Format(this, null, provider);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return DateTimeFormat.Format(this, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return DateTimeFormat.TryFormat(this, destination, out charsWritten, format, provider);
	}

	public DateTime ToUniversalTime()
	{
		return TimeZoneInfo.ConvertTimeToUtc(this, TimeZoneInfoOptions.NoThrowOnInvalidTime);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out DateTime result)
	{
		if (s == null)
		{
			result = default(DateTime);
			return false;
		}
		return DateTimeParse.TryParse(s, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out DateTime result)
	{
		return DateTimeParse.TryParse(s, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, DateTimeStyles styles, out DateTime result)
	{
		DateTimeFormatInfo.ValidateStyles(styles, styles: true);
		if (s == null)
		{
			result = default(DateTime);
			return false;
		}
		return DateTimeParse.TryParse(s, DateTimeFormatInfo.GetInstance(provider), styles, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles styles, out DateTime result)
	{
		DateTimeFormatInfo.ValidateStyles(styles, styles: true);
		return DateTimeParse.TryParse(s, DateTimeFormatInfo.GetInstance(provider), styles, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
	{
		DateTimeFormatInfo.ValidateStyles(style);
		if (s == null || format == null)
		{
			result = default(DateTime);
			return false;
		}
		return DateTimeParse.TryParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style, out result);
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
	{
		DateTimeFormatInfo.ValidateStyles(style);
		return DateTimeParse.TryParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
	{
		DateTimeFormatInfo.ValidateStyles(style);
		if (s == null)
		{
			result = default(DateTime);
			return false;
		}
		return DateTimeParse.TryParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style, out result);
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateTime result)
	{
		DateTimeFormatInfo.ValidateStyles(style);
		return DateTimeParse.TryParseExactMultiple(s, formats, DateTimeFormatInfo.GetInstance(provider), style, out result);
	}

	public static DateTime operator +(DateTime d, TimeSpan t)
	{
		ulong num = (ulong)(d.Ticks + t._ticks);
		if (num > 3155378975999999999L)
		{
			ThrowDateArithmetic(1);
		}
		return new DateTime(num | d.InternalKind);
	}

	public static DateTime operator -(DateTime d, TimeSpan t)
	{
		ulong num = (ulong)(d.Ticks - t._ticks);
		if (num > 3155378975999999999L)
		{
			ThrowDateArithmetic(1);
		}
		return new DateTime(num | d.InternalKind);
	}

	public static TimeSpan operator -(DateTime d1, DateTime d2)
	{
		return new TimeSpan(d1.Ticks - d2.Ticks);
	}

	public static bool operator ==(DateTime d1, DateTime d2)
	{
		return d1.Ticks == d2.Ticks;
	}

	public static bool operator !=(DateTime d1, DateTime d2)
	{
		return d1.Ticks != d2.Ticks;
	}

	public static bool operator <(DateTime t1, DateTime t2)
	{
		return t1.Ticks < t2.Ticks;
	}

	public static bool operator <=(DateTime t1, DateTime t2)
	{
		return t1.Ticks <= t2.Ticks;
	}

	public static bool operator >(DateTime t1, DateTime t2)
	{
		return t1.Ticks > t2.Ticks;
	}

	public static bool operator >=(DateTime t1, DateTime t2)
	{
		return t1.Ticks >= t2.Ticks;
	}

	public string[] GetDateTimeFormats()
	{
		return GetDateTimeFormats(CultureInfo.CurrentCulture);
	}

	public string[] GetDateTimeFormats(IFormatProvider? provider)
	{
		return DateTimeFormat.GetAllDateTimes(this, DateTimeFormatInfo.GetInstance(provider));
	}

	public string[] GetDateTimeFormats(char format)
	{
		return GetDateTimeFormats(format, CultureInfo.CurrentCulture);
	}

	public string[] GetDateTimeFormats(char format, IFormatProvider? provider)
	{
		return DateTimeFormat.GetAllDateTimes(this, format, DateTimeFormatInfo.GetInstance(provider));
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.DateTime;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		throw InvalidCast("Boolean");
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		throw InvalidCast("Char");
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		throw InvalidCast("SByte");
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		throw InvalidCast("Byte");
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		throw InvalidCast("Int16");
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		throw InvalidCast("UInt16");
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		throw InvalidCast("Int32");
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		throw InvalidCast("UInt32");
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		throw InvalidCast("Int64");
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		throw InvalidCast("UInt64");
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		throw InvalidCast("Single");
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		throw InvalidCast("Double");
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		throw InvalidCast("Decimal");
	}

	private static Exception InvalidCast(string to)
	{
		return new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "DateTime", to));
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		return this;
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	internal static bool TryCreate(int year, int month, int day, int hour, int minute, int second, int millisecond, out DateTime result)
	{
		result = default(DateTime);
		if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
		{
			return false;
		}
		if ((uint)hour >= 24u || (uint)minute >= 60u || (uint)millisecond >= 1000u)
		{
			return false;
		}
		uint[] array = (IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365);
		if ((uint)day > array[month] - array[month - 1])
		{
			return false;
		}
		ulong num = (ulong)(uint)((int)(DaysToYear((uint)year) + array[month - 1]) + day - 1) * 864000000000uL;
		if ((uint)second < 60u)
		{
			num += TimeToTicks(hour, minute, second) + (uint)(millisecond * 10000);
		}
		else
		{
			if (second != 60 || !s_systemSupportsLeapSeconds || !IsValidTimeWithLeapSeconds(year, month, day, hour, minute, DateTimeKind.Unspecified))
			{
				return false;
			}
			num += TimeToTicks(hour, minute, 59) + 9990000;
		}
		result = new DateTime(num);
		return true;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTime IAdditionOperators<DateTime, TimeSpan, DateTime>.operator +(DateTime left, TimeSpan right)
	{
		return left + right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateTime, DateTime>.operator <(DateTime left, DateTime right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateTime, DateTime>.operator <=(DateTime left, DateTime right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateTime, DateTime>.operator >(DateTime left, DateTime right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<DateTime, DateTime>.operator >=(DateTime left, DateTime right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<DateTime, DateTime>.operator ==(DateTime left, DateTime right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<DateTime, DateTime>.operator !=(DateTime left, DateTime right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTime IParseable<DateTime>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<DateTime>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out DateTime result)
	{
		return TryParse(s, provider, DateTimeStyles.None, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTime ISpanParseable<DateTime>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<DateTime>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out DateTime result)
	{
		return TryParse(s, provider, DateTimeStyles.None, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static DateTime ISubtractionOperators<DateTime, TimeSpan, DateTime>.operator -(DateTime left, TimeSpan right)
	{
		return left - right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TimeSpan ISubtractionOperators<DateTime, DateTime, TimeSpan>.operator -(DateTime left, DateTime right)
	{
		return left - right;
	}

	private unsafe static bool SystemSupportsLeapSeconds()
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Interop.NtDll.SYSTEM_LEAP_SECOND_INFORMATION sYSTEM_LEAP_SECOND_INFORMATION);
		if (Interop.NtDll.NtQuerySystemInformation(206, &sYSTEM_LEAP_SECOND_INFORMATION, (uint)sizeof(Interop.NtDll.SYSTEM_LEAP_SECOND_INFORMATION), null) == 0)
		{
			return sYSTEM_LEAP_SECOND_INFORMATION.Enabled != Interop.BOOLEAN.FALSE;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal unsafe static bool IsValidTimeWithLeapSeconds(int year, int month, int day, int hour, int minute, DateTimeKind kind)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Interop.Kernel32.SYSTEMTIME sYSTEMTIME);
		sYSTEMTIME.Year = (ushort)year;
		sYSTEMTIME.Month = (ushort)month;
		sYSTEMTIME.DayOfWeek = 0;
		sYSTEMTIME.Day = (ushort)day;
		sYSTEMTIME.Hour = (ushort)hour;
		sYSTEMTIME.Minute = (ushort)minute;
		sYSTEMTIME.Second = 60;
		sYSTEMTIME.Milliseconds = 0;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Interop.Kernel32.SYSTEMTIME sYSTEMTIME2);
		if (kind != DateTimeKind.Utc && Interop.Kernel32.TzSpecificLocalTimeToSystemTime(IntPtr.Zero, &sYSTEMTIME, &sYSTEMTIME2) != 0)
		{
			return true;
		}
		System.Runtime.CompilerServices.Unsafe.SkipInit(out ulong num);
		if (kind != DateTimeKind.Local && Interop.Kernel32.SystemTimeToFileTime(&sYSTEMTIME, &num) != 0)
		{
			return true;
		}
		return false;
	}

	private unsafe static DateTime FromFileTimeLeapSecondsAware(ulong fileTime)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Interop.Kernel32.SYSTEMTIME time);
		if (Interop.Kernel32.FileTimeToSystemTime(&fileTime, &time) == Interop.BOOL.FALSE)
		{
			throw new ArgumentOutOfRangeException("fileTime", SR.ArgumentOutOfRange_DateTimeBadTicks);
		}
		return CreateDateTimeFromSystemTime(in time, fileTime % 10000);
	}

	private unsafe static ulong ToFileTimeLeapSecondsAware(long ticks)
	{
		DateTime dateTime = new DateTime(ticks);
		dateTime.GetDate(out var year, out var month, out var day);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Interop.Kernel32.SYSTEMTIME sYSTEMTIME);
		sYSTEMTIME.Year = (ushort)year;
		sYSTEMTIME.Month = (ushort)month;
		sYSTEMTIME.DayOfWeek = 0;
		sYSTEMTIME.Day = (ushort)day;
		dateTime.GetTimePrecise(out var hour, out var minute, out var second, out var tick);
		sYSTEMTIME.Hour = (ushort)hour;
		sYSTEMTIME.Minute = (ushort)minute;
		sYSTEMTIME.Second = (ushort)second;
		sYSTEMTIME.Milliseconds = 0;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out ulong num);
		if (Interop.Kernel32.SystemTimeToFileTime(&sYSTEMTIME, &num) == Interop.BOOL.FALSE)
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_FileTimeInvalid);
		}
		return num + (uint)tick;
	}

	private static DateTime CreateDateTimeFromSystemTime(in Interop.Kernel32.SYSTEMTIME time, ulong hundredNanoSecond)
	{
		uint year = time.Year;
		uint[] array = (IsLeapYear((int)year) ? s_daysToMonth366 : s_daysToMonth365);
		int num = time.Month - 1;
		uint num2 = DaysToYear(year) + array[num] + time.Day - 1;
		ulong num3 = (ulong)num2 * 864000000000uL;
		num3 += (ulong)(time.Hour * 36000000000L);
		num3 += (ulong)((long)time.Minute * 600000000L);
		uint second = time.Second;
		if (second <= 59)
		{
			ulong num4 = (uint)((int)(second * 10000000) + time.Milliseconds * 10000) + hundredNanoSecond;
			return new DateTime((num3 + num4) | 0x4000000000000000uL);
		}
		num3 += 4611686019027387903L;
		return new DateTime(num3);
	}

	private unsafe static delegate* unmanaged[SuppressGCTransition]<ulong*, void> GetGetSystemTimeAsFileTimeFnPtr()
	{
		IntPtr handle = Interop.Kernel32.LoadLibraryEx("kernel32.dll", IntPtr.Zero, 2048);
		IntPtr intPtr = NativeLibrary.GetExport(handle, "GetSystemTimeAsFileTime");
		if (NativeLibrary.TryGetExport(handle, "GetSystemTimePreciseAsFileTime", out var address))
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out long num);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out long num2);
			for (int i = 0; i < 10; i++)
			{
				((delegate* unmanaged[SuppressGCTransition]<long*, void>)(void*)intPtr)(&num);
				((delegate* unmanaged[SuppressGCTransition]<long*, void>)(void*)address)(&num2);
				if (Math.Abs(num2 - num) <= 1000000)
				{
					intPtr = address;
					break;
				}
			}
		}
		return (delegate* unmanaged[SuppressGCTransition]<ulong*, void>)(void*)intPtr;
	}

	private unsafe static DateTime UpdateLeapSecondCacheAndReturnUtcNow()
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out ulong num);
		s_pfnGetSystemTimeAsFileTime(&num);
		ulong hundredNanoSecond = num % 10000;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Interop.Kernel32.SYSTEMTIME time);
		if (Interop.Kernel32.FileTimeToSystemTime(&num, &time) == Interop.BOOL.FALSE)
		{
			return LowGranularityNonCachedFallback();
		}
		if (time.Second >= 60)
		{
			return CreateDateTimeFromSystemTime(in time, hundredNanoSecond);
		}
		ulong num2 = num + 3000000000u;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Interop.Kernel32.SYSTEMTIME sYSTEMTIME);
		if (Interop.Kernel32.FileTimeToSystemTime(&num2, &sYSTEMTIME) == Interop.BOOL.FALSE)
		{
			return LowGranularityNonCachedFallback();
		}
		ulong num3;
		ulong num4;
		if (sYSTEMTIME.Second == time.Second)
		{
			num3 = num;
			num4 = CreateDateTimeFromSystemTime(in time, hundredNanoSecond)._dateData;
		}
		else
		{
			Interop.Kernel32.SYSTEMTIME time2 = time;
			time2.Hour = 0;
			time2.Minute = 0;
			time2.Second = 0;
			time2.Milliseconds = 0;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out ulong num5);
			if (Interop.Kernel32.SystemTimeToFileTime(&time2, &num5) == Interop.BOOL.FALSE)
			{
				return LowGranularityNonCachedFallback();
			}
			num3 = num5 + 863990000000L - 3000000000u;
			if (num - num3 >= 3000000000u)
			{
				return CreateDateTimeFromSystemTime(in time, hundredNanoSecond);
			}
			num4 = CreateDateTimeFromSystemTime(in time2, 0uL)._dateData + 863990000000L - 3000000000u;
		}
		Volatile.Write(ref s_leapSecondCache, new LeapSecondCache
		{
			OSFileTimeTicksAtStartOfValidityWindow = num3,
			DotnetDateDataAtStartOfValidityWindow = num4
		});
		return new DateTime(num4 + num - num3);
		[MethodImpl(MethodImplOptions.NoInlining)]
		static unsafe DateTime LowGranularityNonCachedFallback()
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out Interop.Kernel32.SYSTEMTIME time3);
			Interop.Kernel32.GetSystemTime(&time3);
			return CreateDateTimeFromSystemTime(in time3, 0uL);
		}
	}
}
