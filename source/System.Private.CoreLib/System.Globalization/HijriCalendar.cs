using Internal.Win32;

namespace System.Globalization;

public class HijriCalendar : Calendar
{
	public static readonly int HijriEra = 1;

	private static readonly int[] s_hijriMonthDays = new int[13]
	{
		0, 30, 59, 89, 118, 148, 177, 207, 236, 266,
		295, 325, 355
	};

	private int _hijriAdvance = int.MinValue;

	private static readonly DateTime s_calendarMinValue = new DateTime(622, 7, 18);

	private static readonly DateTime s_calendarMaxValue = DateTime.MaxValue;

	public override DateTime MinSupportedDateTime => s_calendarMinValue;

	public override DateTime MaxSupportedDateTime => s_calendarMaxValue;

	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunarCalendar;

	internal override CalendarId ID => CalendarId.HIJRI;

	protected override int DaysInYearBeforeMinSupportedYear => 354;

	public int HijriAdjustment
	{
		get
		{
			if (_hijriAdvance == int.MinValue)
			{
				_hijriAdvance = GetHijriDateAdjustment();
			}
			return _hijriAdvance;
		}
		set
		{
			if (value < -2 || value > 2)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, -2, 2));
			}
			VerifyWritable();
			_hijriAdvance = value;
		}
	}

	public override int[] Eras => new int[1] { HijriEra };

	public override int TwoDigitYearMax
	{
		get
		{
			if (_twoDigitYearMax == -1)
			{
				_twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(ID, 1451);
			}
			return _twoDigitYearMax;
		}
		set
		{
			VerifyWritable();
			if (value < 99 || value > 9666)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 99, 9666));
			}
			_twoDigitYearMax = value;
		}
	}

	private long GetAbsoluteDateHijri(int y, int m, int d)
	{
		return DaysUpToHijriYear(y) + s_hijriMonthDays[m - 1] + d - 1 - HijriAdjustment;
	}

	private long DaysUpToHijriYear(int HijriYear)
	{
		int num = (HijriYear - 1) / 30 * 30;
		int num2 = HijriYear - num - 1;
		long num3 = (long)num * 10631L / 30 + 227013;
		while (num2 > 0)
		{
			num3 += 354 + (IsLeapYear(num2, 0) ? 1 : 0);
			num2--;
		}
		return num3;
	}

	internal static void CheckTicksRange(long ticks)
	{
		if (ticks < s_calendarMinValue.Ticks || ticks > s_calendarMaxValue.Ticks)
		{
			throw new ArgumentOutOfRangeException("time", ticks, SR.Format(CultureInfo.InvariantCulture, SR.ArgumentOutOfRange_CalendarRange, s_calendarMinValue, s_calendarMaxValue));
		}
	}

	internal static void CheckEraRange(int era)
	{
		if (era != 0 && era != HijriEra)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
	}

	internal static void CheckYearRange(int year, int era)
	{
		CheckEraRange(era);
		if (year < 1 || year > 9666)
		{
			throw new ArgumentOutOfRangeException("year", SR.Format(SR.ArgumentOutOfRange_Range, 1, 9666));
		}
	}

	internal static void CheckYearMonthRange(int year, int month, int era)
	{
		CheckYearRange(year, era);
		if (year == 9666 && month > 4)
		{
			throw new ArgumentOutOfRangeException("month", month, SR.Format(SR.ArgumentOutOfRange_Range, 1, 4));
		}
		if (month < 1 || month > 12)
		{
			ThrowHelper.ThrowArgumentOutOfRange_Month(month);
		}
	}

	internal virtual int GetDatePart(long ticks, int part)
	{
		CheckTicksRange(ticks);
		long num = ticks / 864000000000L + 1;
		num += HijriAdjustment;
		int num2 = (int)((num - 227013) * 30 / 10631) + 1;
		long num3 = DaysUpToHijriYear(num2);
		long num4 = GetDaysInYear(num2, 0);
		if (num < num3)
		{
			num3 -= num4;
			num2--;
		}
		else if (num == num3)
		{
			num2--;
			num3 -= GetDaysInYear(num2, 0);
		}
		else if (num > num3 + num4)
		{
			num3 += num4;
			num2++;
		}
		if (part == 0)
		{
			return num2;
		}
		int i = 1;
		num -= num3;
		if (part == 1)
		{
			return (int)num;
		}
		for (; i <= 12 && num > s_hijriMonthDays[i - 1]; i++)
		{
		}
		i--;
		if (part == 2)
		{
			return i;
		}
		int result = (int)(num - s_hijriMonthDays[i - 1]);
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
		long ticks = GetAbsoluteDateHijri(datePart, datePart2, num) * 864000000000L + time.Ticks % 864000000000L;
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
		if (month == 12)
		{
			if (!IsLeapYear(year, 0))
			{
				return 29;
			}
			return 30;
		}
		if (month % 2 != 1)
		{
			return 29;
		}
		return 30;
	}

	public override int GetDaysInYear(int year, int era)
	{
		CheckYearRange(year, era);
		if (!IsLeapYear(year, 0))
		{
			return 354;
		}
		return 355;
	}

	public override int GetEra(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		return HijriEra;
	}

	public override int GetMonth(DateTime time)
	{
		return GetDatePart(time.Ticks, 2);
	}

	public override int GetMonthsInYear(int year, int era)
	{
		CheckYearRange(year, era);
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
		return (year * 11 + 14) % 30 < 11;
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		int daysInMonth = GetDaysInMonth(year, month, era);
		if (day < 1 || day > daysInMonth)
		{
			throw new ArgumentOutOfRangeException("day", day, SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
		}
		long absoluteDateHijri = GetAbsoluteDateHijri(year, month, day);
		if (absoluteDateHijri < 0)
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
		}
		return new DateTime(absoluteDateHijri * 864000000000L + Calendar.TimeToTicks(hour, minute, second, millisecond));
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
		if (year > 9666)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1, 9666));
		}
		return year;
	}

	private int GetHijriDateAdjustment()
	{
		if (_hijriAdvance == int.MinValue)
		{
			_hijriAdvance = GetAdvanceHijriDate();
		}
		return _hijriAdvance;
	}

	private static int GetAdvanceHijriDate()
	{
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Control Panel\\International");
		if (registryKey == null)
		{
			return 0;
		}
		object value = registryKey.GetValue("AddHijriDate");
		if (value == null)
		{
			return 0;
		}
		int result = 0;
		string text = value.ToString();
		if (string.Compare(text, 0, "AddHijriDate", 0, "AddHijriDate".Length, StringComparison.OrdinalIgnoreCase) == 0)
		{
			if (text.Length == "AddHijriDate".Length)
			{
				result = -1;
			}
			else
			{
				try
				{
					int num = int.Parse(text.AsSpan("AddHijriDate".Length), NumberStyles.Integer, CultureInfo.InvariantCulture);
					if (num >= -2 && num <= 2)
					{
						result = num;
					}
				}
				catch (ArgumentException)
				{
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
			}
		}
		return result;
	}
}
