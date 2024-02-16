namespace System.Globalization;

public abstract class Calendar : ICloneable
{
	private int _currentEraValue = -1;

	private bool _isReadOnly;

	public const int CurrentEra = 0;

	internal int _twoDigitYearMax = -1;

	public virtual DateTime MinSupportedDateTime => DateTime.MinValue;

	public virtual DateTime MaxSupportedDateTime => DateTime.MaxValue;

	public virtual CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.Unknown;

	internal virtual CalendarId ID => CalendarId.UNINITIALIZED_VALUE;

	internal virtual CalendarId BaseCalendarID => ID;

	public bool IsReadOnly => _isReadOnly;

	internal virtual int CurrentEraValue
	{
		get
		{
			if (_currentEraValue == -1)
			{
				_currentEraValue = CalendarData.GetCalendarCurrentEra(this);
			}
			return _currentEraValue;
		}
	}

	public abstract int[] Eras { get; }

	protected virtual int DaysInYearBeforeMinSupportedYear => 365;

	public virtual int TwoDigitYearMax
	{
		get
		{
			return _twoDigitYearMax;
		}
		set
		{
			VerifyWritable();
			_twoDigitYearMax = value;
		}
	}

	public virtual object Clone()
	{
		object obj = MemberwiseClone();
		((Calendar)obj).SetReadOnlyState(readOnly: false);
		return obj;
	}

	public static Calendar ReadOnly(Calendar calendar)
	{
		if (calendar == null)
		{
			throw new ArgumentNullException("calendar");
		}
		if (calendar.IsReadOnly)
		{
			return calendar;
		}
		Calendar calendar2 = (Calendar)calendar.MemberwiseClone();
		calendar2.SetReadOnlyState(readOnly: true);
		return calendar2;
	}

	internal void VerifyWritable()
	{
		if (_isReadOnly)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
		}
	}

	internal void SetReadOnlyState(bool readOnly)
	{
		_isReadOnly = readOnly;
	}

	internal static void CheckAddResult(long ticks, DateTime minValue, DateTime maxValue)
	{
		if (ticks < minValue.Ticks || ticks > maxValue.Ticks)
		{
			throw new ArgumentException(SR.Format(SR.Argument_ResultCalendarRange, minValue, maxValue));
		}
	}

	internal DateTime Add(DateTime time, double value, int scale)
	{
		double num = value * (double)scale + ((value >= 0.0) ? 0.5 : (-0.5));
		if (!(num > -315537897600000.0) || !(num < 315537897600000.0))
		{
			throw new ArgumentOutOfRangeException("value", value, SR.ArgumentOutOfRange_AddValue);
		}
		long num2 = (long)num;
		long ticks = time.Ticks + num2 * 10000;
		CheckAddResult(ticks, MinSupportedDateTime, MaxSupportedDateTime);
		return new DateTime(ticks);
	}

	public virtual DateTime AddMilliseconds(DateTime time, double milliseconds)
	{
		return Add(time, milliseconds, 1);
	}

	public virtual DateTime AddDays(DateTime time, int days)
	{
		return Add(time, days, 86400000);
	}

	public virtual DateTime AddHours(DateTime time, int hours)
	{
		return Add(time, hours, 3600000);
	}

	public virtual DateTime AddMinutes(DateTime time, int minutes)
	{
		return Add(time, minutes, 60000);
	}

	public abstract DateTime AddMonths(DateTime time, int months);

	public virtual DateTime AddSeconds(DateTime time, int seconds)
	{
		return Add(time, seconds, 1000);
	}

	public virtual DateTime AddWeeks(DateTime time, int weeks)
	{
		return AddDays(time, weeks * 7);
	}

	public abstract DateTime AddYears(DateTime time, int years);

	public abstract int GetDayOfMonth(DateTime time);

	public abstract DayOfWeek GetDayOfWeek(DateTime time);

	public abstract int GetDayOfYear(DateTime time);

	public virtual int GetDaysInMonth(int year, int month)
	{
		return GetDaysInMonth(year, month, 0);
	}

	public abstract int GetDaysInMonth(int year, int month, int era);

	public virtual int GetDaysInYear(int year)
	{
		return GetDaysInYear(year, 0);
	}

	public abstract int GetDaysInYear(int year, int era);

	public abstract int GetEra(DateTime time);

	public virtual int GetHour(DateTime time)
	{
		return (int)(time.Ticks / 36000000000L % 24);
	}

	public virtual double GetMilliseconds(DateTime time)
	{
		return time.Ticks / 10000 % 1000;
	}

	public virtual int GetMinute(DateTime time)
	{
		return (int)(time.Ticks / 600000000 % 60);
	}

	public abstract int GetMonth(DateTime time);

	public virtual int GetMonthsInYear(int year)
	{
		return GetMonthsInYear(year, 0);
	}

	public abstract int GetMonthsInYear(int year, int era);

	public virtual int GetSecond(DateTime time)
	{
		return (int)(time.Ticks / 10000000 % 60);
	}

	internal int GetFirstDayWeekOfYear(DateTime time, int firstDayOfWeek)
	{
		int num = GetDayOfYear(time) - 1;
		int num2 = (int)(GetDayOfWeek(time) - num % 7);
		int num3 = (num2 - firstDayOfWeek + 14) % 7;
		return (num + num3) / 7 + 1;
	}

	private int GetWeekOfYearFullDays(DateTime time, int firstDayOfWeek, int fullDays)
	{
		int num = GetDayOfYear(time) - 1;
		int num2 = (int)(GetDayOfWeek(time) - num % 7);
		int num3 = (firstDayOfWeek - num2 + 14) % 7;
		if (num3 != 0 && num3 >= fullDays)
		{
			num3 -= 7;
		}
		int num4 = num - num3;
		if (num4 >= 0)
		{
			return num4 / 7 + 1;
		}
		if (time <= MinSupportedDateTime.AddDays(num))
		{
			return GetWeekOfYearOfMinSupportedDateTime(firstDayOfWeek, fullDays);
		}
		return GetWeekOfYearFullDays(time.AddDays(-(num + 1)), firstDayOfWeek, fullDays);
	}

	private int GetWeekOfYearOfMinSupportedDateTime(int firstDayOfWeek, int minimumDaysInFirstWeek)
	{
		int num = GetDayOfYear(MinSupportedDateTime) - 1;
		int num2 = (int)(GetDayOfWeek(MinSupportedDateTime) - num % 7);
		int num3 = (firstDayOfWeek + 7 - num2) % 7;
		if (num3 == 0 || num3 >= minimumDaysInFirstWeek)
		{
			return 1;
		}
		int num4 = DaysInYearBeforeMinSupportedYear - 1;
		int num5 = num2 - 1 - num4 % 7;
		int num6 = (firstDayOfWeek - num5 + 14) % 7;
		int num7 = num4 - num6;
		if (num6 >= minimumDaysInFirstWeek)
		{
			num7 += 7;
		}
		return num7 / 7 + 1;
	}

	public virtual int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
	{
		if (firstDayOfWeek < DayOfWeek.Sunday || firstDayOfWeek > DayOfWeek.Saturday)
		{
			throw new ArgumentOutOfRangeException("firstDayOfWeek", firstDayOfWeek, SR.Format(SR.ArgumentOutOfRange_Range, DayOfWeek.Sunday, DayOfWeek.Saturday));
		}
		return rule switch
		{
			CalendarWeekRule.FirstDay => GetFirstDayWeekOfYear(time, (int)firstDayOfWeek), 
			CalendarWeekRule.FirstFullWeek => GetWeekOfYearFullDays(time, (int)firstDayOfWeek, 7), 
			CalendarWeekRule.FirstFourDayWeek => GetWeekOfYearFullDays(time, (int)firstDayOfWeek, 4), 
			_ => throw new ArgumentOutOfRangeException("rule", rule, SR.Format(SR.ArgumentOutOfRange_Range, CalendarWeekRule.FirstDay, CalendarWeekRule.FirstFourDayWeek)), 
		};
	}

	public abstract int GetYear(DateTime time);

	public virtual bool IsLeapDay(int year, int month, int day)
	{
		return IsLeapDay(year, month, day, 0);
	}

	public abstract bool IsLeapDay(int year, int month, int day, int era);

	public virtual bool IsLeapMonth(int year, int month)
	{
		return IsLeapMonth(year, month, 0);
	}

	public abstract bool IsLeapMonth(int year, int month, int era);

	public virtual int GetLeapMonth(int year)
	{
		return GetLeapMonth(year, 0);
	}

	public virtual int GetLeapMonth(int year, int era)
	{
		if (!IsLeapYear(year, era))
		{
			return 0;
		}
		int monthsInYear = GetMonthsInYear(year, era);
		for (int i = 1; i <= monthsInYear; i++)
		{
			if (IsLeapMonth(year, i, era))
			{
				return i;
			}
		}
		return 0;
	}

	public virtual bool IsLeapYear(int year)
	{
		return IsLeapYear(year, 0);
	}

	public abstract bool IsLeapYear(int year, int era);

	public virtual DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
	{
		return ToDateTime(year, month, day, hour, minute, second, millisecond, 0);
	}

	public abstract DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era);

	internal virtual bool TryToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era, out DateTime result)
	{
		result = DateTime.MinValue;
		try
		{
			result = ToDateTime(year, month, day, hour, minute, second, millisecond, era);
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	internal virtual bool IsValidYear(int year, int era)
	{
		if (year >= GetYear(MinSupportedDateTime))
		{
			return year <= GetYear(MaxSupportedDateTime);
		}
		return false;
	}

	internal virtual bool IsValidMonth(int year, int month, int era)
	{
		if (IsValidYear(year, era) && month >= 1)
		{
			return month <= GetMonthsInYear(year, era);
		}
		return false;
	}

	internal virtual bool IsValidDay(int year, int month, int day, int era)
	{
		if (IsValidMonth(year, month, era) && day >= 1)
		{
			return day <= GetDaysInMonth(year, month, era);
		}
		return false;
	}

	public virtual int ToFourDigitYear(int year)
	{
		if (year < 0)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (year < 100)
		{
			return (TwoDigitYearMax / 100 - ((year > TwoDigitYearMax % 100) ? 1 : 0)) * 100 + year;
		}
		return year;
	}

	internal static long TimeToTicks(int hour, int minute, int second, int millisecond)
	{
		if (hour < 0 || hour >= 24 || minute < 0 || minute >= 60 || second < 0 || second >= 60)
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
		}
		if (millisecond < 0 || millisecond >= 1000)
		{
			throw new ArgumentOutOfRangeException("millisecond", millisecond, SR.Format(SR.ArgumentOutOfRange_Range, 0, 999));
		}
		return InternalGlobalizationHelper.TimeToTicks(hour, minute, second) + (long)millisecond * 10000L;
	}

	internal static int GetSystemTwoDigitYearSetting(CalendarId CalID, int defaultYearValue)
	{
		int num = (GlobalizationMode.UseNls ? CalendarData.NlsGetTwoDigitYearMax(CalID) : CalendarData.IcuGetTwoDigitYearMax(CalID));
		if (num < 0)
		{
			return defaultYearValue;
		}
		return num;
	}
}
