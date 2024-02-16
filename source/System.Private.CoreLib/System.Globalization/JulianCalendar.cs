namespace System.Globalization;

public class JulianCalendar : Calendar
{
	public static readonly int JulianEra = 1;

	private static readonly int[] s_daysToMonth365 = new int[13]
	{
		0, 31, 59, 90, 120, 151, 181, 212, 243, 273,
		304, 334, 365
	};

	private static readonly int[] s_daysToMonth366 = new int[13]
	{
		0, 31, 60, 91, 121, 152, 182, 213, 244, 274,
		305, 335, 366
	};

	internal int MaxYear = 9999;

	public override DateTime MinSupportedDateTime => DateTime.MinValue;

	public override DateTime MaxSupportedDateTime => DateTime.MaxValue;

	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

	internal override CalendarId ID => CalendarId.JULIAN;

	public override int[] Eras => new int[1] { JulianEra };

	public override int TwoDigitYearMax
	{
		get
		{
			return _twoDigitYearMax;
		}
		set
		{
			VerifyWritable();
			if (value < 99 || value > MaxYear)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 99, MaxYear));
			}
			_twoDigitYearMax = value;
		}
	}

	public JulianCalendar()
	{
		_twoDigitYearMax = 2029;
	}

	internal static void CheckEraRange(int era)
	{
		if (era != 0 && era != JulianEra)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
	}

	internal void CheckYearEraRange(int year, int era)
	{
		CheckEraRange(era);
		if (year <= 0 || year > MaxYear)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1, MaxYear));
		}
	}

	internal static void CheckMonthRange(int month)
	{
		if (month < 1 || month > 12)
		{
			ThrowHelper.ThrowArgumentOutOfRange_Month(month);
		}
	}

	internal static void CheckDayRange(int year, int month, int day)
	{
		if (year == 1 && month == 1 && day < 3)
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
		}
		int[] array = ((year % 4 == 0) ? s_daysToMonth366 : s_daysToMonth365);
		int num = array[month] - array[month - 1];
		if (day < 1 || day > num)
		{
			throw new ArgumentOutOfRangeException("day", day, SR.Format(SR.ArgumentOutOfRange_Range, 1, num));
		}
	}

	internal static int GetDatePart(long ticks, int part)
	{
		long num = ticks + 1728000000000L;
		int num2 = (int)(num / 864000000000L);
		int num3 = num2 / 1461;
		num2 -= num3 * 1461;
		int num4 = num2 / 365;
		if (num4 == 4)
		{
			num4 = 3;
		}
		if (part == 0)
		{
			return num3 * 4 + num4 + 1;
		}
		num2 -= num4 * 365;
		if (part == 1)
		{
			return num2 + 1;
		}
		int[] array = ((num4 == 3) ? s_daysToMonth366 : s_daysToMonth365);
		int i;
		for (i = (num2 >> 5) + 1; num2 >= array[i]; i++)
		{
		}
		if (part == 2)
		{
			return i;
		}
		return num2 - array[i - 1] + 1;
	}

	internal static long DateToTicks(int year, int month, int day)
	{
		int[] array = ((year % 4 == 0) ? s_daysToMonth366 : s_daysToMonth365);
		int num = year - 1;
		int num2 = num * 365 + num / 4 + array[month - 1] + day - 1;
		return (num2 - 2) * 864000000000L;
	}

	public override DateTime AddMonths(DateTime time, int months)
	{
		if (months < -120000 || months > 120000)
		{
			throw new ArgumentOutOfRangeException("months", months, SR.Format(SR.ArgumentOutOfRange_Range, -120000, 120000));
		}
		int datePart = GetDatePart(time.Ticks, 0);
		int datePart2 = GetDatePart(time.Ticks, 2);
		int num = GetDatePart(time.Ticks, 3);
		int num2 = datePart2 - 1 + months;
		if (num2 >= 0)
		{
			datePart2 = num2 % 12 + 1;
			datePart += num2 / 12;
		}
		else
		{
			datePart2 = 12 + (num2 + 1) % 12;
			datePart += (num2 - 11) / 12;
		}
		int[] array = ((datePart % 4 == 0 && (datePart % 100 != 0 || datePart % 400 == 0)) ? s_daysToMonth366 : s_daysToMonth365);
		int num3 = array[datePart2] - array[datePart2 - 1];
		if (num > num3)
		{
			num = num3;
		}
		long ticks = DateToTicks(datePart, datePart2, num) + time.Ticks % 864000000000L;
		Calendar.CheckAddResult(ticks, MinSupportedDateTime, MaxSupportedDateTime);
		return new DateTime(ticks);
	}

	public override DateTime AddYears(DateTime time, int years)
	{
		return AddMonths(time, years * 12);
	}

	public override int GetDayOfMonth(DateTime time)
	{
		return GetDatePart(time.Ticks, 3);
	}

	public override DayOfWeek GetDayOfWeek(DateTime time)
	{
		return (DayOfWeek)((int)(time.Ticks / 864000000000L + 1) % 7);
	}

	public override int GetDayOfYear(DateTime time)
	{
		return GetDatePart(time.Ticks, 1);
	}

	public override int GetDaysInMonth(int year, int month, int era)
	{
		CheckYearEraRange(year, era);
		CheckMonthRange(month);
		int[] array = ((year % 4 == 0) ? s_daysToMonth366 : s_daysToMonth365);
		return array[month] - array[month - 1];
	}

	public override int GetDaysInYear(int year, int era)
	{
		if (!IsLeapYear(year, era))
		{
			return 365;
		}
		return 366;
	}

	public override int GetEra(DateTime time)
	{
		return JulianEra;
	}

	public override int GetMonth(DateTime time)
	{
		return GetDatePart(time.Ticks, 2);
	}

	public override int GetMonthsInYear(int year, int era)
	{
		CheckYearEraRange(year, era);
		return 12;
	}

	public override int GetYear(DateTime time)
	{
		return GetDatePart(time.Ticks, 0);
	}

	public override bool IsLeapDay(int year, int month, int day, int era)
	{
		CheckMonthRange(month);
		if (IsLeapYear(year, era))
		{
			CheckDayRange(year, month, day);
			if (month == 2)
			{
				return day == 29;
			}
			return false;
		}
		CheckDayRange(year, month, day);
		return false;
	}

	public override int GetLeapMonth(int year, int era)
	{
		CheckYearEraRange(year, era);
		return 0;
	}

	public override bool IsLeapMonth(int year, int month, int era)
	{
		CheckYearEraRange(year, era);
		CheckMonthRange(month);
		return false;
	}

	public override bool IsLeapYear(int year, int era)
	{
		CheckYearEraRange(year, era);
		return year % 4 == 0;
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		CheckYearEraRange(year, era);
		CheckMonthRange(month);
		CheckDayRange(year, month, day);
		if (millisecond < 0 || millisecond >= 1000)
		{
			throw new ArgumentOutOfRangeException("millisecond", millisecond, SR.Format(SR.ArgumentOutOfRange_Range, 0, 999));
		}
		if (hour < 0 || hour >= 24 || minute < 0 || minute >= 60 || second < 0 || second >= 60)
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
		}
		return new DateTime(DateToTicks(year, month, day) + new TimeSpan(0, hour, minute, second, millisecond).Ticks);
	}

	public override int ToFourDigitYear(int year)
	{
		if (year < 0)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (year > MaxYear)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 1, MaxYear));
		}
		return base.ToFourDigitYear(year);
	}
}
