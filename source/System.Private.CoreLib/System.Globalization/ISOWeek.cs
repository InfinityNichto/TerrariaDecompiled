namespace System.Globalization;

public static class ISOWeek
{
	public static int GetWeekOfYear(DateTime date)
	{
		int weekNumber = GetWeekNumber(date);
		if (weekNumber < 1)
		{
			return GetWeeksInYear(date.Year - 1);
		}
		if (weekNumber > GetWeeksInYear(date.Year))
		{
			return 1;
		}
		return weekNumber;
	}

	public static int GetYear(DateTime date)
	{
		int weekNumber = GetWeekNumber(date);
		if (weekNumber < 1)
		{
			return date.Year - 1;
		}
		if (weekNumber > GetWeeksInYear(date.Year))
		{
			return date.Year + 1;
		}
		return date.Year;
	}

	public static DateTime GetYearStart(int year)
	{
		return ToDateTime(year, 1, DayOfWeek.Monday);
	}

	public static DateTime GetYearEnd(int year)
	{
		return ToDateTime(year, GetWeeksInYear(year), DayOfWeek.Sunday);
	}

	public static int GetWeeksInYear(int year)
	{
		if (year < 1 || year > 9999)
		{
			throw new ArgumentOutOfRangeException("year", SR.ArgumentOutOfRange_Year);
		}
		if (P(year) == 4 || P(year - 1) == 3)
		{
			return 53;
		}
		return 52;
		static int P(int y)
		{
			return (y + y / 4 - y / 100 + y / 400) % 7;
		}
	}

	public static DateTime ToDateTime(int year, int week, DayOfWeek dayOfWeek)
	{
		if (year < 1 || year > 9999)
		{
			throw new ArgumentOutOfRangeException("year", SR.ArgumentOutOfRange_Year);
		}
		if (week < 1 || week > 53)
		{
			throw new ArgumentOutOfRangeException("week", SR.ArgumentOutOfRange_Week_ISO);
		}
		if (dayOfWeek < DayOfWeek.Sunday || dayOfWeek > (DayOfWeek)7)
		{
			throw new ArgumentOutOfRangeException("dayOfWeek", SR.ArgumentOutOfRange_DayOfWeek);
		}
		int num = GetWeekday(new DateTime(year, 1, 4).DayOfWeek) + 3;
		int num2 = week * 7 + GetWeekday(dayOfWeek) - num;
		return new DateTime(year, 1, 1).AddDays(num2 - 1);
	}

	private static int GetWeekNumber(DateTime date)
	{
		return (date.DayOfYear - GetWeekday(date.DayOfWeek) + 10) / 7;
	}

	private static int GetWeekday(DayOfWeek dayOfWeek)
	{
		if (dayOfWeek != 0)
		{
			return (int)dayOfWeek;
		}
		return 7;
	}
}
