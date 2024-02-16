namespace System.Globalization;

public class TaiwanLunisolarCalendar : EastAsianLunisolarCalendar
{
	private static readonly EraInfo[] s_taiwanLunisolarEraInfo = new EraInfo[1]
	{
		new EraInfo(1, 1912, 1, 1, 1911, 1, 8088)
	};

	private readonly GregorianCalendarHelper _helper;

	private static readonly DateTime s_minDate = new DateTime(1912, 2, 18);

	private static readonly DateTime s_maxDate = new DateTime(new DateTime(2051, 2, 10, 23, 59, 59, 999).Ticks + 9999);

	private static readonly int[,] s_yinfo = new int[139, 4]
	{
		{ 0, 2, 18, 42192 },
		{ 0, 2, 6, 53840 },
		{ 5, 1, 26, 54568 },
		{ 0, 2, 14, 46400 },
		{ 0, 2, 3, 54944 },
		{ 2, 1, 23, 38608 },
		{ 0, 2, 11, 38320 },
		{ 7, 2, 1, 18872 },
		{ 0, 2, 20, 18800 },
		{ 0, 2, 8, 42160 },
		{ 5, 1, 28, 45656 },
		{ 0, 2, 16, 27216 },
		{ 0, 2, 5, 27968 },
		{ 4, 1, 24, 44456 },
		{ 0, 2, 13, 11104 },
		{ 0, 2, 2, 38256 },
		{ 2, 1, 23, 18808 },
		{ 0, 2, 10, 18800 },
		{ 6, 1, 30, 25776 },
		{ 0, 2, 17, 54432 },
		{ 0, 2, 6, 59984 },
		{ 5, 1, 26, 27976 },
		{ 0, 2, 14, 23248 },
		{ 0, 2, 4, 11104 },
		{ 3, 1, 24, 37744 },
		{ 0, 2, 11, 37600 },
		{ 7, 1, 31, 51560 },
		{ 0, 2, 19, 51536 },
		{ 0, 2, 8, 54432 },
		{ 6, 1, 27, 55888 },
		{ 0, 2, 15, 46416 },
		{ 0, 2, 5, 22176 },
		{ 4, 1, 25, 43736 },
		{ 0, 2, 13, 9680 },
		{ 0, 2, 2, 37584 },
		{ 2, 1, 22, 51544 },
		{ 0, 2, 10, 43344 },
		{ 7, 1, 29, 46248 },
		{ 0, 2, 17, 27808 },
		{ 0, 2, 6, 46416 },
		{ 5, 1, 27, 21928 },
		{ 0, 2, 14, 19872 },
		{ 0, 2, 3, 42416 },
		{ 3, 1, 24, 21176 },
		{ 0, 2, 12, 21168 },
		{ 8, 1, 31, 43344 },
		{ 0, 2, 18, 59728 },
		{ 0, 2, 8, 27296 },
		{ 6, 1, 28, 44368 },
		{ 0, 2, 15, 43856 },
		{ 0, 2, 5, 19296 },
		{ 4, 1, 25, 42352 },
		{ 0, 2, 13, 42352 },
		{ 0, 2, 2, 21088 },
		{ 3, 1, 21, 59696 },
		{ 0, 2, 9, 55632 },
		{ 7, 1, 30, 23208 },
		{ 0, 2, 17, 22176 },
		{ 0, 2, 6, 38608 },
		{ 5, 1, 27, 19176 },
		{ 0, 2, 15, 19152 },
		{ 0, 2, 3, 42192 },
		{ 4, 1, 23, 53864 },
		{ 0, 2, 11, 53840 },
		{ 8, 1, 31, 54568 },
		{ 0, 2, 18, 46400 },
		{ 0, 2, 7, 46752 },
		{ 6, 1, 28, 38608 },
		{ 0, 2, 16, 38320 },
		{ 0, 2, 5, 18864 },
		{ 4, 1, 25, 42168 },
		{ 0, 2, 13, 42160 },
		{ 10, 2, 2, 45656 },
		{ 0, 2, 20, 27216 },
		{ 0, 2, 9, 27968 },
		{ 6, 1, 29, 44448 },
		{ 0, 2, 17, 43872 },
		{ 0, 2, 6, 38256 },
		{ 5, 1, 27, 18808 },
		{ 0, 2, 15, 18800 },
		{ 0, 2, 4, 25776 },
		{ 3, 1, 23, 27216 },
		{ 0, 2, 10, 59984 },
		{ 8, 1, 31, 27432 },
		{ 0, 2, 19, 23232 },
		{ 0, 2, 7, 43872 },
		{ 5, 1, 28, 37736 },
		{ 0, 2, 16, 37600 },
		{ 0, 2, 5, 51552 },
		{ 4, 1, 24, 54440 },
		{ 0, 2, 12, 54432 },
		{ 0, 2, 1, 55888 },
		{ 2, 1, 22, 23208 },
		{ 0, 2, 9, 22176 },
		{ 7, 1, 29, 43736 },
		{ 0, 2, 18, 9680 },
		{ 0, 2, 7, 37584 },
		{ 5, 1, 26, 51544 },
		{ 0, 2, 14, 43344 },
		{ 0, 2, 3, 46240 },
		{ 4, 1, 23, 46416 },
		{ 0, 2, 10, 44368 },
		{ 9, 1, 31, 21928 },
		{ 0, 2, 19, 19360 },
		{ 0, 2, 8, 42416 },
		{ 6, 1, 28, 21176 },
		{ 0, 2, 16, 21168 },
		{ 0, 2, 5, 43312 },
		{ 4, 1, 25, 29864 },
		{ 0, 2, 12, 27296 },
		{ 0, 2, 1, 44368 },
		{ 2, 1, 22, 19880 },
		{ 0, 2, 10, 19296 },
		{ 6, 1, 29, 42352 },
		{ 0, 2, 17, 42208 },
		{ 0, 2, 6, 53856 },
		{ 5, 1, 26, 59696 },
		{ 0, 2, 13, 54576 },
		{ 0, 2, 3, 23200 },
		{ 3, 1, 23, 27472 },
		{ 0, 2, 11, 38608 },
		{ 11, 1, 31, 19176 },
		{ 0, 2, 19, 19152 },
		{ 0, 2, 8, 42192 },
		{ 6, 1, 28, 53848 },
		{ 0, 2, 15, 53840 },
		{ 0, 2, 4, 54560 },
		{ 5, 1, 24, 55968 },
		{ 0, 2, 12, 46496 },
		{ 0, 2, 1, 22224 },
		{ 2, 1, 22, 19160 },
		{ 0, 2, 10, 18864 },
		{ 7, 1, 30, 42168 },
		{ 0, 2, 17, 42160 },
		{ 0, 2, 6, 43600 },
		{ 5, 1, 26, 46376 },
		{ 0, 2, 14, 27936 },
		{ 0, 2, 2, 44448 },
		{ 3, 1, 23, 21936 }
	};

	public override DateTime MinSupportedDateTime => s_minDate;

	public override DateTime MaxSupportedDateTime => s_maxDate;

	protected override int DaysInYearBeforeMinSupportedYear => 384;

	internal override int MinCalendarYear => 1912;

	internal override int MaxCalendarYear => 2050;

	internal override DateTime MinDate => s_minDate;

	internal override DateTime MaxDate => s_maxDate;

	internal override EraInfo[]? CalEraInfo => s_taiwanLunisolarEraInfo;

	internal override CalendarId BaseCalendarID => CalendarId.TAIWAN;

	internal override CalendarId ID => CalendarId.TAIWANLUNISOLAR;

	public override int[] Eras => _helper.Eras;

	internal override int GetYearInfo(int lunarYear, int index)
	{
		if (lunarYear < 1912 || lunarYear > 2050)
		{
			throw new ArgumentOutOfRangeException("year", lunarYear, SR.Format(SR.ArgumentOutOfRange_Range, 1912, 2050));
		}
		return s_yinfo[lunarYear - 1912, index];
	}

	internal override int GetYear(int year, DateTime time)
	{
		return _helper.GetYear(year, time);
	}

	internal override int GetGregorianYear(int year, int era)
	{
		return _helper.GetGregorianYear(year, era);
	}

	public TaiwanLunisolarCalendar()
	{
		_helper = new GregorianCalendarHelper(this, s_taiwanLunisolarEraInfo);
	}

	public override int GetEra(DateTime time)
	{
		return _helper.GetEra(time);
	}
}
