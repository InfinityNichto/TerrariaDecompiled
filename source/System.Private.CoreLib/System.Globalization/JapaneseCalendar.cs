using System.Collections.Generic;
using System.IO;
using System.Security;
using Internal.Win32;

namespace System.Globalization;

public class JapaneseCalendar : Calendar
{
	private static readonly DateTime s_calendarMinValue = new DateTime(1868, 9, 8);

	private static volatile EraInfo[] s_japaneseEraInfo;

	internal static volatile Calendar s_defaultInstance;

	internal GregorianCalendarHelper _helper;

	private static readonly string[] s_abbreviatedEnglishEraNames = new string[5] { "M", "T", "S", "H", "R" };

	public override DateTime MinSupportedDateTime => s_calendarMinValue;

	public override DateTime MaxSupportedDateTime => DateTime.MaxValue;

	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

	internal override CalendarId ID => CalendarId.JAPAN;

	public override int[] Eras => _helper.Eras;

	public override int TwoDigitYearMax
	{
		get
		{
			if (_twoDigitYearMax == -1)
			{
				_twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(ID, 99);
			}
			return _twoDigitYearMax;
		}
		set
		{
			VerifyWritable();
			if (value < 99 || value > _helper.MaxYear)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 99, _helper.MaxYear));
			}
			_twoDigitYearMax = value;
		}
	}

	internal static EraInfo[] GetEraInfo()
	{
		object obj = s_japaneseEraInfo;
		if (obj == null)
		{
			obj = (s_japaneseEraInfo = (GlobalizationMode.UseNls ? NlsGetJapaneseEras() : IcuGetJapaneseEras()));
			if (obj == null)
			{
				obj = new EraInfo[5]
				{
					new EraInfo(5, 2019, 5, 1, 2018, 1, 7981, "令和", "令", "R"),
					new EraInfo(4, 1989, 1, 8, 1988, 1, 31, "平成", "平", "H"),
					new EraInfo(3, 1926, 12, 25, 1925, 1, 64, "昭和", "昭", "S"),
					new EraInfo(2, 1912, 7, 30, 1911, 1, 15, "大正", "大", "T"),
					new EraInfo(1, 1868, 1, 1, 1867, 1, 45, "明治", "明", "M")
				};
				s_japaneseEraInfo = (EraInfo[])obj;
			}
		}
		return (EraInfo[])obj;
	}

	internal static Calendar GetDefaultInstance()
	{
		return s_defaultInstance ?? (s_defaultInstance = new JapaneseCalendar());
	}

	public JapaneseCalendar()
	{
		try
		{
			new CultureInfo("ja-JP");
		}
		catch (ArgumentException innerException)
		{
			throw new TypeInitializationException(GetType().ToString(), innerException);
		}
		_helper = new GregorianCalendarHelper(this, GetEraInfo());
	}

	public override DateTime AddMonths(DateTime time, int months)
	{
		return _helper.AddMonths(time, months);
	}

	public override DateTime AddYears(DateTime time, int years)
	{
		return _helper.AddYears(time, years);
	}

	public override int GetDaysInMonth(int year, int month, int era)
	{
		return _helper.GetDaysInMonth(year, month, era);
	}

	public override int GetDaysInYear(int year, int era)
	{
		return _helper.GetDaysInYear(year, era);
	}

	public override int GetDayOfMonth(DateTime time)
	{
		return _helper.GetDayOfMonth(time);
	}

	public override DayOfWeek GetDayOfWeek(DateTime time)
	{
		return _helper.GetDayOfWeek(time);
	}

	public override int GetDayOfYear(DateTime time)
	{
		return _helper.GetDayOfYear(time);
	}

	public override int GetMonthsInYear(int year, int era)
	{
		return _helper.GetMonthsInYear(year, era);
	}

	public override int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
	{
		return _helper.GetWeekOfYear(time, rule, firstDayOfWeek);
	}

	public override int GetEra(DateTime time)
	{
		return _helper.GetEra(time);
	}

	public override int GetMonth(DateTime time)
	{
		return _helper.GetMonth(time);
	}

	public override int GetYear(DateTime time)
	{
		return _helper.GetYear(time);
	}

	public override bool IsLeapDay(int year, int month, int day, int era)
	{
		return _helper.IsLeapDay(year, month, day, era);
	}

	public override bool IsLeapYear(int year, int era)
	{
		return _helper.IsLeapYear(year, era);
	}

	public override int GetLeapMonth(int year, int era)
	{
		return _helper.GetLeapMonth(year, era);
	}

	public override bool IsLeapMonth(int year, int month, int era)
	{
		return _helper.IsLeapMonth(year, month, era);
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		return _helper.ToDateTime(year, month, day, hour, minute, second, millisecond, era);
	}

	public override int ToFourDigitYear(int year)
	{
		if (year <= 0)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (year > _helper.MaxYear)
		{
			throw new ArgumentOutOfRangeException("year", year, SR.Format(SR.ArgumentOutOfRange_Range, 1, _helper.MaxYear));
		}
		return year;
	}

	internal static string[] EraNames()
	{
		EraInfo[] eraInfo = GetEraInfo();
		string[] array = new string[eraInfo.Length];
		for (int i = 0; i < eraInfo.Length; i++)
		{
			array[i] = eraInfo[eraInfo.Length - i - 1].eraName;
		}
		return array;
	}

	internal static string[] AbbrevEraNames()
	{
		EraInfo[] eraInfo = GetEraInfo();
		string[] array = new string[eraInfo.Length];
		for (int i = 0; i < eraInfo.Length; i++)
		{
			array[i] = eraInfo[eraInfo.Length - i - 1].abbrevEraName;
		}
		return array;
	}

	internal static string[] EnglishEraNames()
	{
		EraInfo[] eraInfo = GetEraInfo();
		string[] array = new string[eraInfo.Length];
		for (int i = 0; i < eraInfo.Length; i++)
		{
			array[i] = eraInfo[eraInfo.Length - i - 1].englishEraName;
		}
		return array;
	}

	internal override bool IsValidYear(int year, int era)
	{
		return _helper.IsValidYear(year, era);
	}

	private static EraInfo[] IcuGetJapaneseEras()
	{
		if (GlobalizationMode.Invariant)
		{
			return null;
		}
		if (!CalendarData.EnumCalendarInfo("ja-JP", CalendarId.JAPAN, CalendarDataType.EraNames, out var calendarData))
		{
			return null;
		}
		List<EraInfo> list = new List<EraInfo>();
		int num = 9999;
		int latestJapaneseEra = Interop.Globalization.GetLatestJapaneseEra();
		for (int num2 = latestJapaneseEra; num2 >= 0; num2--)
		{
			if (!GetJapaneseEraStartDate(num2, out var dateTime))
			{
				return null;
			}
			if (dateTime < s_calendarMinValue)
			{
				break;
			}
			list.Add(new EraInfo(num2, dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Year - 1, 1, num - dateTime.Year + 1, calendarData[num2], GetAbbreviatedEraName(calendarData, num2), ""));
			num = dateTime.Year;
		}
		if (!CalendarData.EnumCalendarInfo("ja", CalendarId.JAPAN, CalendarDataType.AbbrevEraNames, out var calendarData2))
		{
			calendarData2 = s_abbreviatedEnglishEraNames;
		}
		if (calendarData2[^1].Length == 0 || calendarData2[^1][0] > '\u007f')
		{
			calendarData2 = s_abbreviatedEnglishEraNames;
		}
		int num3 = ((calendarData2 == s_abbreviatedEnglishEraNames) ? (list.Count - 1) : (calendarData2.Length - 1));
		for (int i = 0; i < list.Count; i++)
		{
			list[i].era = list.Count - i;
			if (num3 < calendarData2.Length)
			{
				list[i].englishEraName = calendarData2[num3];
			}
			num3--;
		}
		return list.ToArray();
	}

	private static string GetAbbreviatedEraName(string[] eraNames, int eraIndex)
	{
		return eraNames[eraIndex].Substring(0, 1);
	}

	private static bool GetJapaneseEraStartDate(int era, out DateTime dateTime)
	{
		dateTime = default(DateTime);
		int startYear;
		int startMonth;
		int startDay;
		bool japaneseEraStartDate = Interop.Globalization.GetJapaneseEraStartDate(era, out startYear, out startMonth, out startDay);
		if (japaneseEraStartDate)
		{
			dateTime = new DateTime(startYear, startMonth, startDay);
		}
		return japaneseEraStartDate;
	}

	private static EraInfo[] NlsGetJapaneseEras()
	{
		int num = 0;
		EraInfo[] array = null;
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Nls\\Calendars\\Japanese\\Eras");
			if (registryKey == null)
			{
				return null;
			}
			string[] valueNames = registryKey.GetValueNames();
			if (valueNames != null && valueNames.Length != 0)
			{
				array = new EraInfo[valueNames.Length];
				for (int i = 0; i < valueNames.Length; i++)
				{
					EraInfo eraFromValue = GetEraFromValue(valueNames[i], registryKey.GetValue(valueNames[i])?.ToString());
					if (eraFromValue != null)
					{
						array[num] = eraFromValue;
						num++;
					}
				}
			}
		}
		catch (SecurityException)
		{
			return null;
		}
		catch (IOException)
		{
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
		if (num < 4)
		{
			return null;
		}
		Array.Resize(ref array, num);
		Array.Sort(array, CompareEraRanges);
		for (int j = 0; j < array.Length; j++)
		{
			array[j].era = array.Length - j;
			if (j == 0)
			{
				array[0].maxEraYear = 9999 - array[0].yearOffset;
			}
			else
			{
				array[j].maxEraYear = array[j - 1].yearOffset + 1 - array[j].yearOffset;
			}
		}
		return array;
	}

	private static int CompareEraRanges(EraInfo a, EraInfo b)
	{
		return b.ticks.CompareTo(a.ticks);
	}

	private static EraInfo GetEraFromValue(string value, string data)
	{
		if (value == null || data == null)
		{
			return null;
		}
		if (value.Length != 10)
		{
			return null;
		}
		ReadOnlySpan<char> readOnlySpan = value.AsSpan();
		if (!int.TryParse(readOnlySpan.Slice(0, 4), NumberStyles.None, NumberFormatInfo.InvariantInfo, out var result) || !int.TryParse(readOnlySpan.Slice(5, 2), NumberStyles.None, NumberFormatInfo.InvariantInfo, out var result2) || !int.TryParse(readOnlySpan.Slice(8, 2), NumberStyles.None, NumberFormatInfo.InvariantInfo, out var result3))
		{
			return null;
		}
		string[] array = data.Split('_');
		if (array.Length != 4)
		{
			return null;
		}
		if (array[0].Length == 0 || array[1].Length == 0 || array[2].Length == 0 || array[3].Length == 0)
		{
			return null;
		}
		return new EraInfo(0, result, result2, result3, result - 1, 1, 0, array[0], array[1], array[3]);
	}
}
