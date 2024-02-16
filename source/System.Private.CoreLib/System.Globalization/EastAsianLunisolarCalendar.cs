namespace System.Globalization;

public abstract class EastAsianLunisolarCalendar : Calendar
{
	private static readonly int[] s_daysToMonth365 = new int[12]
	{
		0, 31, 59, 90, 120, 151, 181, 212, 243, 273,
		304, 334
	};

	private static readonly int[] s_daysToMonth366 = new int[12]
	{
		0, 31, 60, 91, 121, 152, 182, 213, 244, 274,
		305, 335
	};

	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunisolarCalendar;

	internal abstract int MinCalendarYear { get; }

	internal abstract int MaxCalendarYear { get; }

	internal abstract EraInfo[]? CalEraInfo { get; }

	internal abstract DateTime MinDate { get; }

	internal abstract DateTime MaxDate { get; }

	public override int TwoDigitYearMax
	{
		get
		{
			if (_twoDigitYearMax == -1)
			{
				_twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(BaseCalendarID, GetYear(new DateTime(2029, 1, 1)));
			}
			return _twoDigitYearMax;
		}
		set
		{
			VerifyWritable();
			if (value < 99 || value > MaxCalendarYear)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 99, MaxCalendarYear));
			}
			_twoDigitYearMax = value;
		}
	}

	public virtual int GetSexagenaryYear(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		TimeToLunar(time, out var year, out var _, out var _);
		return (year - 4) % 60 + 1;
	}

	public int GetCelestialStem(int sexagenaryYear)
	{
		if (sexagenaryYear < 1 || sexagenaryYear > 60)
		{
			throw new ArgumentOutOfRangeException("sexagenaryYear", sexagenaryYear, SR.Format(SR.ArgumentOutOfRange_Range, 1, 60));
		}
		return (sexagenaryYear - 1) % 10 + 1;
	}

	public int GetTerrestrialBranch(int sexagenaryYear)
	{
		if (sexagenaryYear < 1 || sexagenaryYear > 60)
		{
			throw new ArgumentOutOfRangeException("sexagenaryYear", sexagenaryYear, SR.Format(SR.ArgumentOutOfRange_Range, 1, 60));
		}
		return (sexagenaryYear - 1) % 12 + 1;
	}

	internal abstract int GetYearInfo(int LunarYear, int Index);

	internal abstract int GetYear(int year, DateTime time);

	internal abstract int GetGregorianYear(int year, int era);

	internal int MinEraCalendarYear(int era)
	{
		EraInfo[] calEraInfo = CalEraInfo;
		if (calEraInfo == null)
		{
			return MinCalendarYear;
		}
		if (era == 0)
		{
			era = CurrentEraValue;
		}
		if (era == GetEra(MinDate))
		{
			return GetYear(MinCalendarYear, MinDate);
		}
		for (int i = 0; i < calEraInfo.Length; i++)
		{
			if (era == calEraInfo[i].era)
			{
				return calEraInfo[i].minEraYear;
			}
		}
		throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
	}

	internal int MaxEraCalendarYear(int era)
	{
		EraInfo[] calEraInfo = CalEraInfo;
		if (calEraInfo == null)
		{
			return MaxCalendarYear;
		}
		if (era == 0)
		{
			era = CurrentEraValue;
		}
		if (era == GetEra(MaxDate))
		{
			return GetYear(MaxCalendarYear, MaxDate);
		}
		for (int i = 0; i < calEraInfo.Length; i++)
		{
			if (era == calEraInfo[i].era)
			{
				return calEraInfo[i].maxEraYear;
			}
		}
		throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
	}

	internal EastAsianLunisolarCalendar()
	{
	}

	internal void CheckTicksRange(long ticks)
	{
		if (ticks < MinSupportedDateTime.Ticks || ticks > MaxSupportedDateTime.Ticks)
		{
			throw new ArgumentOutOfRangeException("time", ticks, SR.Format(CultureInfo.InvariantCulture, SR.ArgumentOutOfRange_CalendarRange, MinSupportedDateTime, MaxSupportedDateTime));
		}
	}

	internal void CheckEraRange(int era)
	{
		if (era == 0)
		{
			era = CurrentEraValue;
		}
		if (era < GetEra(MinDate) || era > GetEra(MaxDate))
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
	}

	internal int CheckYearRange(int year, int era)
	{
		CheckEraRange(era);
		year = GetGregorianYear(year, era);
		if (year < MinCalendarYear || year > MaxCalendarYear)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, MinEraCalendarYear(era), MaxEraCalendarYear(era)));
		}
		return year;
	}

	internal int CheckYearMonthRange(int year, int month, int era)
	{
		year = CheckYearRange(year, era);
		if (month == 13 && GetYearInfo(year, 0) == 0)
		{
			ThrowHelper.ThrowArgumentOutOfRange_Month(month);
		}
		if (month < 1 || month > 13)
		{
			ThrowHelper.ThrowArgumentOutOfRange_Month(month);
		}
		return year;
	}

	internal int InternalGetDaysInMonth(int year, int month)
	{
		int num = 32768;
		num >>= month - 1;
		if ((GetYearInfo(year, 3) & num) == 0)
		{
			return 29;
		}
		return 30;
	}

	public override int GetDaysInMonth(int year, int month, int era)
	{
		year = CheckYearMonthRange(year, month, era);
		return InternalGetDaysInMonth(year, month);
	}

	private static bool GregorianIsLeapYear(int y)
	{
		if (y % 4 != 0)
		{
			return false;
		}
		if (y % 100 != 0)
		{
			return true;
		}
		return y % 400 == 0;
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		year = CheckYearMonthRange(year, month, era);
		int num = InternalGetDaysInMonth(year, month);
		if (day < 1 || day > num)
		{
			throw new ArgumentOutOfRangeException("day", day, SR.Format(SR.ArgumentOutOfRange_Day, num, month));
		}
		if (!LunarToGregorian(year, month, day, out var solarYear, out var solarMonth, out var solarDay))
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
		}
		return new DateTime(solarYear, solarMonth, solarDay, hour, minute, second, millisecond);
	}

	private void GregorianToLunar(int solarYear, int solarMonth, int solarDate, out int lunarYear, out int lunarMonth, out int lunarDate)
	{
		int num = (GregorianIsLeapYear(solarYear) ? s_daysToMonth366[solarMonth - 1] : s_daysToMonth365[solarMonth - 1]);
		num += solarDate;
		int num2 = num;
		lunarYear = solarYear;
		int yearInfo;
		int yearInfo2;
		if (lunarYear == MaxCalendarYear + 1)
		{
			lunarYear--;
			num2 += (GregorianIsLeapYear(lunarYear) ? 366 : 365);
			yearInfo = GetYearInfo(lunarYear, 1);
			yearInfo2 = GetYearInfo(lunarYear, 2);
		}
		else
		{
			yearInfo = GetYearInfo(lunarYear, 1);
			yearInfo2 = GetYearInfo(lunarYear, 2);
			if (solarMonth < yearInfo || (solarMonth == yearInfo && solarDate < yearInfo2))
			{
				lunarYear--;
				num2 += (GregorianIsLeapYear(lunarYear) ? 366 : 365);
				yearInfo = GetYearInfo(lunarYear, 1);
				yearInfo2 = GetYearInfo(lunarYear, 2);
			}
		}
		num2 -= s_daysToMonth365[yearInfo - 1];
		num2 -= yearInfo2 - 1;
		int num3 = 32768;
		int yearInfo3 = GetYearInfo(lunarYear, 3);
		int num4 = (((yearInfo3 & num3) != 0) ? 30 : 29);
		lunarMonth = 1;
		while (num2 > num4)
		{
			num2 -= num4;
			lunarMonth++;
			num3 >>= 1;
			num4 = (((yearInfo3 & num3) != 0) ? 30 : 29);
		}
		lunarDate = num2;
	}

	private bool LunarToGregorian(int lunarYear, int lunarMonth, int lunarDate, out int solarYear, out int solarMonth, out int solarDay)
	{
		if (lunarDate < 1 || lunarDate > 30)
		{
			solarYear = 0;
			solarMonth = 0;
			solarDay = 0;
			return false;
		}
		int num = lunarDate - 1;
		for (int i = 1; i < lunarMonth; i++)
		{
			num += InternalGetDaysInMonth(lunarYear, i);
		}
		int yearInfo = GetYearInfo(lunarYear, 1);
		int yearInfo2 = GetYearInfo(lunarYear, 2);
		bool flag = GregorianIsLeapYear(lunarYear);
		int[] array = (flag ? s_daysToMonth366 : s_daysToMonth365);
		solarDay = yearInfo2;
		if (yearInfo > 1)
		{
			solarDay += array[yearInfo - 1];
		}
		solarDay += num;
		if (solarDay > 365 + (flag ? 1 : 0))
		{
			solarYear = lunarYear + 1;
			solarDay -= 365 + (flag ? 1 : 0);
		}
		else
		{
			solarYear = lunarYear;
		}
		solarMonth = 1;
		while (solarMonth < 12 && array[solarMonth] < solarDay)
		{
			solarMonth++;
		}
		solarDay -= array[solarMonth - 1];
		return true;
	}

	private DateTime LunarToTime(DateTime time, int year, int month, int day)
	{
		LunarToGregorian(year, month, day, out var solarYear, out var solarMonth, out var solarDay);
		time.GetTime(out var hour, out var minute, out var second, out var millisecond);
		return GregorianCalendar.GetDefaultInstance().ToDateTime(solarYear, solarMonth, solarDay, hour, minute, second, millisecond);
	}

	private void TimeToLunar(DateTime time, out int year, out int month, out int day)
	{
		Calendar defaultInstance = GregorianCalendar.GetDefaultInstance();
		int year2 = defaultInstance.GetYear(time);
		int month2 = defaultInstance.GetMonth(time);
		int dayOfMonth = defaultInstance.GetDayOfMonth(time);
		GregorianToLunar(year2, month2, dayOfMonth, out year, out month, out day);
	}

	public override DateTime AddMonths(DateTime time, int months)
	{
		if (months < -120000 || months > 120000)
		{
			throw new ArgumentOutOfRangeException("months", months, SR.Format(SR.ArgumentOutOfRange_Range, -120000, 120000));
		}
		CheckTicksRange(time.Ticks);
		TimeToLunar(time, out var year, out var month, out var day);
		int num = month + months;
		if (num > 0)
		{
			int num2 = (InternalIsLeapYear(year) ? 13 : 12);
			while (num - num2 > 0)
			{
				num -= num2;
				year++;
				num2 = (InternalIsLeapYear(year) ? 13 : 12);
			}
			month = num;
		}
		else
		{
			while (num <= 0)
			{
				int num3 = (InternalIsLeapYear(year - 1) ? 13 : 12);
				num += num3;
				year--;
			}
			month = num;
		}
		int num4 = InternalGetDaysInMonth(year, month);
		if (day > num4)
		{
			day = num4;
		}
		DateTime result = LunarToTime(time, year, month, day);
		Calendar.CheckAddResult(result.Ticks, MinSupportedDateTime, MaxSupportedDateTime);
		return result;
	}

	public override DateTime AddYears(DateTime time, int years)
	{
		CheckTicksRange(time.Ticks);
		TimeToLunar(time, out var year, out var month, out var day);
		year += years;
		if (month == 13 && !InternalIsLeapYear(year))
		{
			month = 12;
			day = InternalGetDaysInMonth(year, month);
		}
		int num = InternalGetDaysInMonth(year, month);
		if (day > num)
		{
			day = num;
		}
		DateTime result = LunarToTime(time, year, month, day);
		Calendar.CheckAddResult(result.Ticks, MinSupportedDateTime, MaxSupportedDateTime);
		return result;
	}

	public override int GetDayOfYear(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		TimeToLunar(time, out var year, out var month, out var day);
		for (int i = 1; i < month; i++)
		{
			day += InternalGetDaysInMonth(year, i);
		}
		return day;
	}

	public override int GetDayOfMonth(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		TimeToLunar(time, out var _, out var _, out var day);
		return day;
	}

	public override int GetDaysInYear(int year, int era)
	{
		year = CheckYearRange(year, era);
		int num = 0;
		int num2 = (InternalIsLeapYear(year) ? 13 : 12);
		while (num2 != 0)
		{
			num += InternalGetDaysInMonth(year, num2--);
		}
		return num;
	}

	public override int GetMonth(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		TimeToLunar(time, out var _, out var month, out var _);
		return month;
	}

	public override int GetYear(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		TimeToLunar(time, out var year, out var _, out var _);
		return GetYear(year, time);
	}

	public override DayOfWeek GetDayOfWeek(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		return (DayOfWeek)((int)(time.Ticks / 864000000000L + 1) % 7);
	}

	public override int GetMonthsInYear(int year, int era)
	{
		year = CheckYearRange(year, era);
		if (!InternalIsLeapYear(year))
		{
			return 12;
		}
		return 13;
	}

	public override bool IsLeapDay(int year, int month, int day, int era)
	{
		year = CheckYearMonthRange(year, month, era);
		int num = InternalGetDaysInMonth(year, month);
		if (day < 1 || day > num)
		{
			throw new ArgumentOutOfRangeException("day", day, SR.Format(SR.ArgumentOutOfRange_Day, num, month));
		}
		int yearInfo = GetYearInfo(year, 0);
		if (yearInfo != 0)
		{
			return month == yearInfo + 1;
		}
		return false;
	}

	public override bool IsLeapMonth(int year, int month, int era)
	{
		year = CheckYearMonthRange(year, month, era);
		int yearInfo = GetYearInfo(year, 0);
		if (yearInfo != 0)
		{
			return month == yearInfo + 1;
		}
		return false;
	}

	public override int GetLeapMonth(int year, int era)
	{
		year = CheckYearRange(year, era);
		int yearInfo = GetYearInfo(year, 0);
		if (yearInfo <= 0)
		{
			return 0;
		}
		return yearInfo + 1;
	}

	internal bool InternalIsLeapYear(int year)
	{
		return GetYearInfo(year, 0) != 0;
	}

	public override bool IsLeapYear(int year, int era)
	{
		year = CheckYearRange(year, era);
		return InternalIsLeapYear(year);
	}

	public override int ToFourDigitYear(int year)
	{
		if (year < 0)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		year = base.ToFourDigitYear(year);
		CheckYearRange(year, 0);
		return year;
	}
}
