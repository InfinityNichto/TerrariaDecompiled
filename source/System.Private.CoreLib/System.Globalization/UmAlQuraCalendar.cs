namespace System.Globalization;

public class UmAlQuraCalendar : Calendar
{
	private struct DateMapping
	{
		internal int HijriMonthsLengthFlags;

		internal DateTime GregorianDate;

		internal DateMapping(int MonthsLengthFlags, int GYear, int GMonth, int GDay)
		{
			HijriMonthsLengthFlags = MonthsLengthFlags;
			GregorianDate = new DateTime(GYear, GMonth, GDay);
		}
	}

	private static readonly DateMapping[] s_hijriYearInfo = InitDateMapping();

	public const int UmAlQuraEra = 1;

	private static readonly DateTime s_minDate = new DateTime(1900, 4, 30);

	private static readonly DateTime s_maxDate = new DateTime(new DateTime(2077, 11, 16, 23, 59, 59, 999).Ticks + 9999);

	public override DateTime MinSupportedDateTime => s_minDate;

	public override DateTime MaxSupportedDateTime => s_maxDate;

	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunarCalendar;

	internal override CalendarId BaseCalendarID => CalendarId.HIJRI;

	internal override CalendarId ID => CalendarId.UMALQURA;

	protected override int DaysInYearBeforeMinSupportedYear => 355;

	public override int[] Eras => new int[1] { 1 };

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
			if (value != 99 && (value < 1318 || value > 1500))
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 1318, 1500));
			}
			VerifyWritable();
			_twoDigitYearMax = value;
		}
	}

	private static DateMapping[] InitDateMapping()
	{
		short[] array = new short[736]
		{
			746, 1900, 4, 30, 1769, 1901, 4, 19, 3794, 1902,
			4, 9, 3748, 1903, 3, 30, 3402, 1904, 3, 18,
			2710, 1905, 3, 7, 1334, 1906, 2, 24, 2741, 1907,
			2, 13, 3498, 1908, 2, 3, 2980, 1909, 1, 23,
			2889, 1910, 1, 12, 2707, 1911, 1, 1, 1323, 1911,
			12, 21, 2647, 1912, 12, 9, 1206, 1913, 11, 29,
			2741, 1914, 11, 18, 1450, 1915, 11, 8, 3413, 1916,
			10, 27, 3370, 1917, 10, 17, 2646, 1918, 10, 6,
			1198, 1919, 9, 25, 2397, 1920, 9, 13, 748, 1921,
			9, 3, 1749, 1922, 8, 23, 1706, 1923, 8, 13,
			1365, 1924, 8, 1, 1195, 1925, 7, 21, 2395, 1926,
			7, 10, 698, 1927, 6, 30, 1397, 1928, 6, 18,
			2994, 1929, 6, 8, 1892, 1930, 5, 29, 1865, 1931,
			5, 18, 1621, 1932, 5, 6, 683, 1933, 4, 25,
			1371, 1934, 4, 14, 2778, 1935, 4, 4, 1748, 1936,
			3, 24, 3785, 1937, 3, 13, 3474, 1938, 3, 3,
			3365, 1939, 2, 20, 2637, 1940, 2, 9, 685, 1941,
			1, 28, 1389, 1942, 1, 17, 2922, 1943, 1, 7,
			2898, 1943, 12, 28, 2725, 1944, 12, 16, 2635, 1945,
			12, 5, 1175, 1946, 11, 24, 2359, 1947, 11, 13,
			694, 1948, 11, 2, 1397, 1949, 10, 22, 3434, 1950,
			10, 12, 3410, 1951, 10, 2, 2710, 1952, 9, 20,
			2349, 1953, 9, 9, 605, 1954, 8, 29, 1245, 1955,
			8, 18, 2778, 1956, 8, 7, 1492, 1957, 7, 28,
			3497, 1958, 7, 17, 3410, 1959, 7, 7, 2730, 1960,
			6, 25, 1238, 1961, 6, 14, 2486, 1962, 6, 3,
			884, 1963, 5, 24, 1897, 1964, 5, 12, 1874, 1965,
			5, 2, 1701, 1966, 4, 21, 1355, 1967, 4, 10,
			2731, 1968, 3, 29, 1370, 1969, 3, 19, 2773, 1970,
			3, 8, 3538, 1971, 2, 26, 3492, 1972, 2, 16,
			3401, 1973, 2, 4, 2709, 1974, 1, 24, 1325, 1975,
			1, 13, 2653, 1976, 1, 2, 1370, 1976, 12, 22,
			2773, 1977, 12, 11, 1706, 1978, 12, 1, 1685, 1979,
			11, 20, 1323, 1980, 11, 8, 2647, 1981, 10, 28,
			1198, 1982, 10, 18, 2422, 1983, 10, 7, 1388, 1984,
			9, 26, 2901, 1985, 9, 15, 2730, 1986, 9, 5,
			2645, 1987, 8, 25, 1197, 1988, 8, 13, 2397, 1989,
			8, 2, 730, 1990, 7, 23, 1497, 1991, 7, 12,
			3506, 1992, 7, 1, 2980, 1993, 6, 21, 2890, 1994,
			6, 10, 2645, 1995, 5, 30, 693, 1996, 5, 18,
			1397, 1997, 5, 7, 2922, 1998, 4, 27, 3026, 1999,
			4, 17, 3012, 2000, 4, 6, 2953, 2001, 3, 26,
			2709, 2002, 3, 15, 1325, 2003, 3, 4, 1453, 2004,
			2, 21, 2922, 2005, 2, 10, 1748, 2006, 1, 31,
			3529, 2007, 1, 20, 3474, 2008, 1, 10, 2726, 2008,
			12, 29, 2390, 2009, 12, 18, 686, 2010, 12, 7,
			1389, 2011, 11, 26, 874, 2012, 11, 15, 2901, 2013,
			11, 4, 2730, 2014, 10, 25, 2381, 2015, 10, 14,
			1181, 2016, 10, 2, 2397, 2017, 9, 21, 698, 2018,
			9, 11, 1461, 2019, 8, 31, 1450, 2020, 8, 20,
			3413, 2021, 8, 9, 2714, 2022, 7, 30, 2350, 2023,
			7, 19, 622, 2024, 7, 7, 1373, 2025, 6, 26,
			2778, 2026, 6, 16, 1748, 2027, 6, 6, 1701, 2028,
			5, 25, 1355, 2029, 5, 14, 2711, 2030, 5, 3,
			1358, 2031, 4, 23, 2734, 2032, 4, 11, 1452, 2033,
			4, 1, 2985, 2034, 3, 21, 3474, 2035, 3, 11,
			2853, 2036, 2, 28, 1611, 2037, 2, 16, 3243, 2038,
			2, 5, 1370, 2039, 1, 26, 2901, 2040, 1, 15,
			1746, 2041, 1, 4, 3749, 2041, 12, 24, 3658, 2042,
			12, 14, 2709, 2043, 12, 3, 1325, 2044, 11, 21,
			2733, 2045, 11, 10, 876, 2046, 10, 31, 1881, 2047,
			10, 20, 1746, 2048, 10, 9, 1685, 2049, 9, 28,
			1325, 2050, 9, 17, 2651, 2051, 9, 6, 1210, 2052,
			8, 26, 2490, 2053, 8, 15, 948, 2054, 8, 5,
			2921, 2055, 7, 25, 2898, 2056, 7, 14, 2726, 2057,
			7, 3, 1206, 2058, 6, 22, 2413, 2059, 6, 11,
			748, 2060, 5, 31, 1753, 2061, 5, 20, 3762, 2062,
			5, 10, 3412, 2063, 4, 30, 3370, 2064, 4, 18,
			2646, 2065, 4, 7, 1198, 2066, 3, 27, 2413, 2067,
			3, 16, 3434, 2068, 3, 5, 2900, 2069, 2, 23,
			2857, 2070, 2, 12, 2707, 2071, 2, 1, 1323, 2072,
			1, 21, 2647, 2073, 1, 9, 1334, 2073, 12, 30,
			2741, 2074, 12, 19, 1706, 2075, 12, 9, 3731, 2076,
			11, 27, 0, 2077, 11, 17
		};
		DateMapping[] array2 = new DateMapping[array.Length / 4];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = new DateMapping(array[i * 4], array[i * 4 + 1], array[i * 4 + 2], array[i * 4 + 3]);
		}
		return array2;
	}

	private static void ConvertHijriToGregorian(int HijriYear, int HijriMonth, int HijriDay, out int yg, out int mg, out int dg)
	{
		int num = HijriDay - 1;
		int num2 = HijriYear - 1318;
		DateTime gregorianDate = s_hijriYearInfo[num2].GregorianDate;
		int num3 = s_hijriYearInfo[num2].HijriMonthsLengthFlags;
		for (int i = 1; i < HijriMonth; i++)
		{
			num = num + 29 + (num3 & 1);
			num3 >>= 1;
		}
		gregorianDate.AddDays(num).GetDate(out yg, out mg, out dg);
	}

	private static long GetAbsoluteDateUmAlQura(int year, int month, int day)
	{
		ConvertHijriToGregorian(year, month, day, out var yg, out var mg, out var dg);
		return GregorianCalendar.GetAbsoluteDate(yg, mg, dg);
	}

	internal static void CheckTicksRange(long ticks)
	{
		if (ticks < s_minDate.Ticks || ticks > s_maxDate.Ticks)
		{
			throw new ArgumentOutOfRangeException("time", ticks, SR.Format(CultureInfo.InvariantCulture, SR.ArgumentOutOfRange_CalendarRange, s_minDate, s_maxDate));
		}
	}

	internal static void CheckEraRange(int era)
	{
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
		}
	}

	internal static void CheckYearRange(int year, int era)
	{
		CheckEraRange(era);
		if (year < 1318 || year > 1500)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1318, 1500));
		}
	}

	internal static void CheckYearMonthRange(int year, int month, int era)
	{
		CheckYearRange(year, era);
		if (month < 1 || month > 12)
		{
			ThrowHelper.ThrowArgumentOutOfRange_Month(month);
		}
	}

	private static void ConvertGregorianToHijri(DateTime time, out int HijriYear, out int HijriMonth, out int HijriDay)
	{
		int num = (int)((time.Ticks - s_minDate.Ticks) / 864000000000L) / 355;
		while (time.CompareTo(s_hijriYearInfo[++num].GregorianDate) > 0)
		{
		}
		if (time.CompareTo(s_hijriYearInfo[num].GregorianDate) != 0)
		{
			num--;
		}
		TimeSpan timeSpan = time.Subtract(s_hijriYearInfo[num].GregorianDate);
		int num2 = num + 1318;
		int num3 = 1;
		int num4 = 1;
		double num5 = timeSpan.TotalDays;
		int num6 = s_hijriYearInfo[num].HijriMonthsLengthFlags;
		int num7 = 29 + (num6 & 1);
		while (num5 >= (double)num7)
		{
			num5 -= (double)num7;
			num6 >>= 1;
			num7 = 29 + (num6 & 1);
			num3++;
		}
		num4 += (int)num5;
		HijriDay = num4;
		HijriMonth = num3;
		HijriYear = num2;
	}

	private static int GetDatePart(DateTime time, int part)
	{
		long ticks = time.Ticks;
		CheckTicksRange(ticks);
		ConvertGregorianToHijri(time, out var HijriYear, out var HijriMonth, out var HijriDay);
		return part switch
		{
			0 => HijriYear, 
			2 => HijriMonth, 
			3 => HijriDay, 
			1 => (int)(GetAbsoluteDateUmAlQura(HijriYear, HijriMonth, HijriDay) - GetAbsoluteDateUmAlQura(HijriYear, 1, 1) + 1), 
			_ => throw new InvalidOperationException(SR.InvalidOperation_DateTimeParsing), 
		};
	}

	public override DateTime AddMonths(DateTime time, int months)
	{
		if (months < -120000 || months > 120000)
		{
			throw new ArgumentOutOfRangeException("months", months, SR.Format(SR.ArgumentOutOfRange_Range, -120000, 120000));
		}
		int datePart = GetDatePart(time, 0);
		int datePart2 = GetDatePart(time, 2);
		int num = GetDatePart(time, 3);
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
		if (num > 29)
		{
			int daysInMonth = GetDaysInMonth(datePart, datePart2);
			if (num > daysInMonth)
			{
				num = daysInMonth;
			}
		}
		CheckYearRange(datePart, 1);
		DateTime result = new DateTime(GetAbsoluteDateUmAlQura(datePart, datePart2, num) * 864000000000L + time.Ticks % 864000000000L);
		Calendar.CheckAddResult(result.Ticks, MinSupportedDateTime, MaxSupportedDateTime);
		return result;
	}

	public override DateTime AddYears(DateTime time, int years)
	{
		return AddMonths(time, years * 12);
	}

	public override int GetDayOfMonth(DateTime time)
	{
		return GetDatePart(time, 3);
	}

	public override DayOfWeek GetDayOfWeek(DateTime time)
	{
		return (DayOfWeek)((int)(time.Ticks / 864000000000L + 1) % 7);
	}

	public override int GetDayOfYear(DateTime time)
	{
		return GetDatePart(time, 1);
	}

	public override int GetDaysInMonth(int year, int month, int era)
	{
		CheckYearMonthRange(year, month, era);
		if ((s_hijriYearInfo[year - 1318].HijriMonthsLengthFlags & (1 << month - 1)) == 0)
		{
			return 29;
		}
		return 30;
	}

	internal static int RealGetDaysInYear(int year)
	{
		int num = 0;
		int num2 = s_hijriYearInfo[year - 1318].HijriMonthsLengthFlags;
		for (int i = 1; i <= 12; i++)
		{
			num = num + 29 + (num2 & 1);
			num2 >>= 1;
		}
		return num;
	}

	public override int GetDaysInYear(int year, int era)
	{
		CheckYearRange(year, era);
		return RealGetDaysInYear(year);
	}

	public override int GetEra(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		return 1;
	}

	public override int GetMonth(DateTime time)
	{
		return GetDatePart(time, 2);
	}

	public override int GetMonthsInYear(int year, int era)
	{
		CheckYearRange(year, era);
		return 12;
	}

	public override int GetYear(DateTime time)
	{
		return GetDatePart(time, 0);
	}

	public override bool IsLeapDay(int year, int month, int day, int era)
	{
		if (day >= 1 && day <= 29)
		{
			CheckYearMonthRange(year, month, era);
			return false;
		}
		int daysInMonth = GetDaysInMonth(year, month, era);
		if (day < 1 || day > daysInMonth)
		{
			throw new ArgumentOutOfRangeException("day", day, SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
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
		return RealGetDaysInYear(year) == 355;
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		if (day >= 1 && day <= 29)
		{
			CheckYearMonthRange(year, month, era);
		}
		else
		{
			int daysInMonth = GetDaysInMonth(year, month, era);
			if (day < 1 || day > daysInMonth)
			{
				throw new ArgumentOutOfRangeException("day", day, SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
			}
		}
		long absoluteDateUmAlQura = GetAbsoluteDateUmAlQura(year, month, day);
		if (absoluteDateUmAlQura < 0)
		{
			throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
		}
		return new DateTime(absoluteDateUmAlQura * 864000000000L + Calendar.TimeToTicks(hour, minute, second, millisecond));
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
		if (year < 1318 || year > 1500)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1318, 1500));
		}
		return year;
	}
}
