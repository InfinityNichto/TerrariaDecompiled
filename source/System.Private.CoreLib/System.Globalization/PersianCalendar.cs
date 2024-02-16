namespace System.Globalization;

public class PersianCalendar : Calendar
{
	public static readonly int PersianEra = 1;

	private static readonly long s_persianEpoch = new DateTime(622, 3, 22).Ticks / 864000000000L;

	private static readonly int[] s_daysToMonth = new int[13]
	{
		0, 31, 62, 93, 124, 155, 186, 216, 246, 276,
		306, 336, 366
	};

	private static readonly DateTime s_minDate = new DateTime(622, 3, 22);

	private static readonly DateTime s_maxDate = DateTime.MaxValue;

	public override DateTime MinSupportedDateTime => s_minDate;

	public override DateTime MaxSupportedDateTime => s_maxDate;

	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

	internal override CalendarId BaseCalendarID => CalendarId.GREGORIAN;

	internal override CalendarId ID => CalendarId.PERSIAN;

	public override int[] Eras => new int[1] { PersianEra };

	public override int TwoDigitYearMax
	{
		get
		{
			if (_twoDigitYearMax == -1)
			{
				_twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(ID, 1410);
			}
			return _twoDigitYearMax;
		}
		set
		{
			VerifyWritable();
			if (value < 99 || value > 9378)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 99, 9378));
			}
			_twoDigitYearMax = value;
		}
	}

	private static long GetAbsoluteDatePersian(int year, int month, int day)
	{
		if (year < 1 || year > 9378 || month < 1 || month > 12)
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
		}
		int num = DaysInPreviousMonths(month) + day - 1;
		int num2 = (int)(365.242189 * (double)(year - 1));
		long num3 = CalendricalCalculationsHelper.PersianNewYearOnOrBefore(s_persianEpoch + num2 + 180);
		return num3 + num;
	}

	internal static void CheckTicksRange(long ticks)
	{
		if (ticks < s_minDate.Ticks || ticks > s_maxDate.Ticks)
		{
			throw new ArgumentOutOfRangeException("time", ticks, SR.Format(SR.ArgumentOutOfRange_CalendarRange, s_minDate, s_maxDate));
		}
	}

	internal static void CheckEraRange(int era)
	{
		if (era != 0 && era != PersianEra)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
	}

	internal static void CheckYearRange(int year, int era)
	{
		CheckEraRange(era);
		if (year < 1 || year > 9378)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1, 9378));
		}
	}

	internal static void CheckYearMonthRange(int year, int month, int era)
	{
		CheckYearRange(year, era);
		if (year == 9378 && month > 10)
		{
			throw new ArgumentOutOfRangeException("month", month, SR.Format(SR.ArgumentOutOfRange_Range, 1, 10));
		}
		if (month < 1 || month > 12)
		{
			ThrowHelper.ThrowArgumentOutOfRange_Month(month);
		}
	}

	private static int MonthFromOrdinalDay(int ordinalDay)
	{
		int i;
		for (i = 0; ordinalDay > s_daysToMonth[i]; i++)
		{
		}
		return i;
	}

	private static int DaysInPreviousMonths(int month)
	{
		month--;
		return s_daysToMonth[month];
	}

	internal int GetDatePart(long ticks, int part)
	{
		CheckTicksRange(ticks);
		long num = ticks / 864000000000L + 1;
		long num2 = CalendricalCalculationsHelper.PersianNewYearOnOrBefore(num);
		int num3 = (int)Math.Floor((double)(num2 - s_persianEpoch) / 365.242189 + 0.5) + 1;
		if (part == 0)
		{
			return num3;
		}
		int num4 = (int)(num - CalendricalCalculationsHelper.GetNumberOfDays(ToDateTime(num3, 1, 1, 0, 0, 0, 0, 1)));
		if (part == 1)
		{
			return num4;
		}
		int num5 = MonthFromOrdinalDay(num4);
		if (part == 2)
		{
			return num5;
		}
		int result = num4 - DaysInPreviousMonths(num5);
		if (part == 3)
		{
			return result;
		}
		throw new InvalidOperationException(SR.InvalidOperation_DateTimeParsing);
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
		int daysInMonth = GetDaysInMonth(datePart, datePart2);
		if (num > daysInMonth)
		{
			num = daysInMonth;
		}
		long ticks = GetAbsoluteDatePersian(datePart, datePart2, num) * 864000000000L + time.Ticks % 864000000000L;
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
		CheckYearMonthRange(year, month, era);
		if (month == 10 && year == 9378)
		{
			return 13;
		}
		int num = s_daysToMonth[month] - s_daysToMonth[month - 1];
		if (month == 12 && !IsLeapYear(year))
		{
			num--;
		}
		return num;
	}

	public override int GetDaysInYear(int year, int era)
	{
		CheckYearRange(year, era);
		if (year == 9378)
		{
			return s_daysToMonth[9] + 13;
		}
		if (!IsLeapYear(year, 0))
		{
			return 365;
		}
		return 366;
	}

	public override int GetEra(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		return PersianEra;
	}

	public override int GetMonth(DateTime time)
	{
		return GetDatePart(time.Ticks, 2);
	}

	public override int GetMonthsInYear(int year, int era)
	{
		CheckYearRange(year, era);
		if (year == 9378)
		{
			return 10;
		}
		return 12;
	}

	public override int GetYear(DateTime time)
	{
		return GetDatePart(time.Ticks, 0);
	}

	public override bool IsLeapDay(int year, int month, int day, int era)
	{
		int daysInMonth = GetDaysInMonth(year, month, era);
		if (day < 1 || day > daysInMonth)
		{
			throw new ArgumentOutOfRangeException("day", day, SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
		}
		if (IsLeapYear(year, era) && month == 12)
		{
			return day == 30;
		}
		return false;
	}

	public override int GetLeapMonth(int year, int era)
	{
		CheckYearRange(year, era);
		return 0;
	}

	public override bool IsLeapMonth(int year, int month, int era)
	{
		CheckYearMonthRange(year, month, era);
		return false;
	}

	public override bool IsLeapYear(int year, int era)
	{
		CheckYearRange(year, era);
		if (year == 9378)
		{
			return false;
		}
		return GetAbsoluteDatePersian(year + 1, 1, 1) - GetAbsoluteDatePersian(year, 1, 1) == 366;
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		int daysInMonth = GetDaysInMonth(year, month, era);
		if (day < 1 || day > daysInMonth)
		{
			throw new ArgumentOutOfRangeException("day", day, SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
		}
		long absoluteDatePersian = GetAbsoluteDatePersian(year, month, day);
		if (absoluteDatePersian < 0)
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
		}
		return new DateTime(absoluteDatePersian * 864000000000L + Calendar.TimeToTicks(hour, minute, second, millisecond));
	}

	public override int ToFourDigitYear(int year)
	{
		if (year < 0)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (year < 100)
		{
			return base.ToFourDigitYear(year);
		}
		if (year > 9378)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1, 9378));
		}
		return year;
	}
}
