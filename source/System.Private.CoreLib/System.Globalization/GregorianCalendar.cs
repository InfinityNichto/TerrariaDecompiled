namespace System.Globalization;

public class GregorianCalendar : Calendar
{
	public const int ADEra = 1;

	private GregorianCalendarTypes _type;

	private static readonly int[] DaysToMonth365 = new int[13]
	{
		0, 31, 59, 90, 120, 151, 181, 212, 243, 273,
		304, 334, 365
	};

	private static readonly int[] DaysToMonth366 = new int[13]
	{
		0, 31, 60, 91, 121, 152, 182, 213, 244, 274,
		305, 335, 366
	};

	private static volatile Calendar s_defaultInstance;

	public override DateTime MinSupportedDateTime => DateTime.MinValue;

	public override DateTime MaxSupportedDateTime => DateTime.MaxValue;

	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

	public virtual GregorianCalendarTypes CalendarType
	{
		get
		{
			return _type;
		}
		set
		{
			VerifyWritable();
			if (value < GregorianCalendarTypes.Localized || value > GregorianCalendarTypes.TransliteratedFrench)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, GregorianCalendarTypes.Localized, GregorianCalendarTypes.TransliteratedFrench));
			}
			_type = value;
		}
	}

	internal override CalendarId ID => (CalendarId)_type;

	public override int[] Eras => new int[1] { 1 };

	public override int TwoDigitYearMax
	{
		get
		{
			if (_twoDigitYearMax == -1)
			{
				_twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(ID, 2029);
			}
			return _twoDigitYearMax;
		}
		set
		{
			VerifyWritable();
			if (value < 99 || value > 9999)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 99, 9999));
			}
			_twoDigitYearMax = value;
		}
	}

	internal static Calendar GetDefaultInstance()
	{
		return s_defaultInstance ?? (s_defaultInstance = new GregorianCalendar());
	}

	public GregorianCalendar()
		: this(GregorianCalendarTypes.Localized)
	{
	}

	public GregorianCalendar(GregorianCalendarTypes type)
	{
		if (type < GregorianCalendarTypes.Localized || type > GregorianCalendarTypes.TransliteratedFrench)
		{
			throw new ArgumentOutOfRangeException("type", type, SR.Format(SR.ArgumentOutOfRange_Range, GregorianCalendarTypes.Localized, GregorianCalendarTypes.TransliteratedFrench));
		}
		_type = type;
	}

	internal static long GetAbsoluteDate(int year, int month, int day)
	{
		if (year >= 1 && year <= 9999 && month >= 1 && month <= 12)
		{
			int[] array = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? DaysToMonth366 : DaysToMonth365);
			if (day >= 1 && day <= array[month] - array[month - 1])
			{
				int num = year - 1;
				return num * 365 + num / 4 - num / 100 + num / 400 + array[month - 1] + day - 1;
			}
		}
		throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
	}

	internal virtual long DateToTicks(int year, int month, int day)
	{
		return GetAbsoluteDate(year, month, day) * 864000000000L;
	}

	public override DateTime AddMonths(DateTime time, int months)
	{
		if (months < -120000 || months > 120000)
		{
			throw new ArgumentOutOfRangeException("months", months, SR.Format(SR.ArgumentOutOfRange_Range, -120000, 120000));
		}
		time.GetDate(out var year, out var month, out var day);
		int num = month - 1 + months;
		if (num >= 0)
		{
			month = num % 12 + 1;
			year += num / 12;
		}
		else
		{
			month = 12 + (num + 1) % 12;
			year += (num - 11) / 12;
		}
		int[] array = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? DaysToMonth366 : DaysToMonth365);
		int num2 = array[month] - array[month - 1];
		if (day > num2)
		{
			day = num2;
		}
		long ticks = DateToTicks(year, month, day) + time.Ticks % 864000000000L;
		Calendar.CheckAddResult(ticks, MinSupportedDateTime, MaxSupportedDateTime);
		return new DateTime(ticks);
	}

	public override DateTime AddYears(DateTime time, int years)
	{
		return AddMonths(time, years * 12);
	}

	public override int GetDayOfMonth(DateTime time)
	{
		return time.Day;
	}

	public override DayOfWeek GetDayOfWeek(DateTime time)
	{
		return time.DayOfWeek;
	}

	public override int GetDayOfYear(DateTime time)
	{
		return time.DayOfYear;
	}

	public override int GetDaysInMonth(int year, int month, int era)
	{
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
		return DateTime.DaysInMonth(year, month);
	}

	public override int GetDaysInYear(int year, int era)
	{
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
		if (!DateTime.IsLeapYear(year))
		{
			return 365;
		}
		return 366;
	}

	public override int GetEra(DateTime time)
	{
		return 1;
	}

	public override int GetMonth(DateTime time)
	{
		return time.Month;
	}

	public override int GetMonthsInYear(int year, int era)
	{
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
		if (year < 1 || year > 9999)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1, 9999));
		}
		return 12;
	}

	public override int GetYear(DateTime time)
	{
		return time.Year;
	}

	internal override bool IsValidYear(int year, int era)
	{
		if (year >= 1)
		{
			return year <= 9999;
		}
		return false;
	}

	internal override bool IsValidDay(int year, int month, int day, int era)
	{
		if ((era != 0 && era != 1) || year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
		{
			return false;
		}
		int[] array = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? DaysToMonth366 : DaysToMonth365);
		return day <= array[month] - array[month - 1];
	}

	public override bool IsLeapDay(int year, int month, int day, int era)
	{
		if (month < 1 || month > 12)
		{
			throw new ArgumentOutOfRangeException("month", month, SR.Format(SR.ArgumentOutOfRange_Range, 1, 12));
		}
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
		if (year < 1 || year > 9999)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1, 9999));
		}
		if (day < 1 || day > GetDaysInMonth(year, month))
		{
			throw new ArgumentOutOfRangeException("day", day, SR.Format(SR.ArgumentOutOfRange_Range, 1, GetDaysInMonth(year, month)));
		}
		if (IsLeapYear(year) && month == 2)
		{
			return day == 29;
		}
		return false;
	}

	public override int GetLeapMonth(int year, int era)
	{
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
		if (year < 1 || year > 9999)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1, 9999));
		}
		return 0;
	}

	public override bool IsLeapMonth(int year, int month, int era)
	{
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
		if (year < 1 || year > 9999)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1, 9999));
		}
		if (month < 1 || month > 12)
		{
			throw new ArgumentOutOfRangeException("month", month, SR.Format(SR.ArgumentOutOfRange_Range, 1, 12));
		}
		return false;
	}

	public override bool IsLeapYear(int year, int era)
	{
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
		return DateTime.IsLeapYear(year);
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
		return new DateTime(year, month, day, hour, minute, second, millisecond);
	}

	internal override bool TryToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era, out DateTime result)
	{
		if (era != 0 && era != 1)
		{
			result = DateTime.MinValue;
			return false;
		}
		return DateTime.TryCreate(year, month, day, hour, minute, second, millisecond, out result);
	}

	public override int ToFourDigitYear(int year)
	{
		if (year < 0)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (year > 9999)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1, 9999));
		}
		return base.ToFourDigitYear(year);
	}
}
