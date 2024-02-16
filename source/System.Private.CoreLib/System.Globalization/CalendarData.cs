using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Globalization;

internal sealed class CalendarData
{
	private struct IcuEnumCalendarsData
	{
		public List<string> Results;

		public bool DisallowDuplicates;
	}

	private struct EnumData
	{
		public string userOverride;

		public List<string> strings;
	}

	public struct NlsEnumCalendarsData
	{
		public int userOverride;

		public List<int> calendars;
	}

	internal string sNativeName;

	internal string[] saShortDates;

	internal string[] saYearMonths;

	internal string[] saLongDates;

	internal string sMonthDay;

	internal string[] saEraNames;

	internal string[] saAbbrevEraNames;

	internal string[] saAbbrevEnglishEraNames;

	internal string[] saDayNames;

	internal string[] saAbbrevDayNames;

	internal string[] saSuperShortDayNames;

	internal string[] saMonthNames;

	internal string[] saAbbrevMonthNames;

	internal string[] saMonthGenitiveNames;

	internal string[] saAbbrevMonthGenitiveNames;

	internal string[] saLeapYearMonthNames;

	internal int iTwoDigitYearMax = 2029;

	private int iCurrentEra;

	internal bool bUseUserOverrides;

	internal static readonly CalendarData Invariant = CreateInvariant();

	private CalendarData()
	{
	}

	private static CalendarData CreateInvariant()
	{
		CalendarData calendarData = new CalendarData();
		calendarData.sNativeName = "Gregorian Calendar";
		calendarData.iTwoDigitYearMax = 2029;
		calendarData.iCurrentEra = 1;
		calendarData.saShortDates = new string[2] { "MM/dd/yyyy", "yyyy-MM-dd" };
		calendarData.saLongDates = new string[1] { "dddd, dd MMMM yyyy" };
		calendarData.saYearMonths = new string[1] { "yyyy MMMM" };
		calendarData.sMonthDay = "MMMM dd";
		calendarData.saEraNames = new string[1] { "A.D." };
		calendarData.saAbbrevEraNames = new string[1] { "AD" };
		calendarData.saAbbrevEnglishEraNames = new string[1] { "AD" };
		calendarData.saDayNames = new string[7] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
		calendarData.saAbbrevDayNames = new string[7] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
		calendarData.saSuperShortDayNames = new string[7] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };
		calendarData.saMonthNames = new string[13]
		{
			"January",
			"February",
			"March",
			"April",
			"May",
			"June",
			"July",
			"August",
			"September",
			"October",
			"November",
			"December",
			string.Empty
		};
		calendarData.saAbbrevMonthNames = new string[13]
		{
			"Jan",
			"Feb",
			"Mar",
			"Apr",
			"May",
			"Jun",
			"Jul",
			"Aug",
			"Sep",
			"Oct",
			"Nov",
			"Dec",
			string.Empty
		};
		calendarData.saMonthGenitiveNames = calendarData.saMonthNames;
		calendarData.saAbbrevMonthGenitiveNames = calendarData.saAbbrevMonthNames;
		calendarData.saLeapYearMonthNames = calendarData.saMonthNames;
		calendarData.bUseUserOverrides = false;
		return calendarData;
	}

	internal CalendarData(string localeName, CalendarId calendarId, bool bUseUserOverrides)
	{
		this.bUseUserOverrides = bUseUserOverrides;
		if (!LoadCalendarDataFromSystemCore(localeName, calendarId))
		{
			if (sNativeName == null)
			{
				sNativeName = string.Empty;
			}
			if (saShortDates == null)
			{
				saShortDates = Invariant.saShortDates;
			}
			if (saYearMonths == null)
			{
				saYearMonths = Invariant.saYearMonths;
			}
			if (saLongDates == null)
			{
				saLongDates = Invariant.saLongDates;
			}
			if (sMonthDay == null)
			{
				sMonthDay = Invariant.sMonthDay;
			}
			if (saEraNames == null)
			{
				saEraNames = Invariant.saEraNames;
			}
			if (saAbbrevEraNames == null)
			{
				saAbbrevEraNames = Invariant.saAbbrevEraNames;
			}
			if (saAbbrevEnglishEraNames == null)
			{
				saAbbrevEnglishEraNames = Invariant.saAbbrevEnglishEraNames;
			}
			if (saDayNames == null)
			{
				saDayNames = Invariant.saDayNames;
			}
			if (saAbbrevDayNames == null)
			{
				saAbbrevDayNames = Invariant.saAbbrevDayNames;
			}
			if (saSuperShortDayNames == null)
			{
				saSuperShortDayNames = Invariant.saSuperShortDayNames;
			}
			if (saMonthNames == null)
			{
				saMonthNames = Invariant.saMonthNames;
			}
			if (saAbbrevMonthNames == null)
			{
				saAbbrevMonthNames = Invariant.saAbbrevMonthNames;
			}
		}
		if (calendarId == CalendarId.TAIWAN)
		{
			if (SystemSupportsTaiwaneseCalendar())
			{
				sNativeName = "中華民國曆";
			}
			else
			{
				sNativeName = string.Empty;
			}
		}
		if (saMonthGenitiveNames == null || saMonthGenitiveNames.Length == 0 || string.IsNullOrEmpty(saMonthGenitiveNames[0]))
		{
			saMonthGenitiveNames = saMonthNames;
		}
		if (saAbbrevMonthGenitiveNames == null || saAbbrevMonthGenitiveNames.Length == 0 || string.IsNullOrEmpty(saAbbrevMonthGenitiveNames[0]))
		{
			saAbbrevMonthGenitiveNames = saAbbrevMonthNames;
		}
		if (saLeapYearMonthNames == null || saLeapYearMonthNames.Length == 0 || string.IsNullOrEmpty(saLeapYearMonthNames[0]))
		{
			saLeapYearMonthNames = saMonthNames;
		}
		InitializeEraNames(localeName, calendarId);
		InitializeAbbreviatedEraNames(localeName, calendarId);
		if (calendarId == CalendarId.JAPAN)
		{
			saAbbrevEnglishEraNames = JapaneseCalendar.EnglishEraNames();
		}
		else
		{
			saAbbrevEnglishEraNames = new string[1] { "" };
		}
		iCurrentEra = saEraNames.Length;
	}

	private void InitializeEraNames(string localeName, CalendarId calendarId)
	{
		switch (calendarId)
		{
		case CalendarId.GREGORIAN:
			if (saEraNames == null || saEraNames.Length == 0 || string.IsNullOrEmpty(saEraNames[0]))
			{
				saEraNames = new string[1] { "A.D." };
			}
			break;
		case CalendarId.GREGORIAN_US:
		case CalendarId.JULIAN:
			saEraNames = new string[1] { "A.D." };
			break;
		case CalendarId.HEBREW:
			saEraNames = new string[1] { "C.E." };
			break;
		case CalendarId.HIJRI:
		case CalendarId.UMALQURA:
			if (localeName == "dv-MV")
			{
				saEraNames = new string[1] { "ހ\u07a8ޖ\u07b0ރ\u07a9" };
			}
			else
			{
				saEraNames = new string[1] { "بعد الهجرة" };
			}
			break;
		case CalendarId.GREGORIAN_ARABIC:
		case CalendarId.GREGORIAN_XLIT_ENGLISH:
		case CalendarId.GREGORIAN_XLIT_FRENCH:
			saEraNames = new string[1] { "م" };
			break;
		case CalendarId.GREGORIAN_ME_FRENCH:
			saEraNames = new string[1] { "ap. J.-C." };
			break;
		case CalendarId.TAIWAN:
			if (SystemSupportsTaiwaneseCalendar())
			{
				saEraNames = new string[1] { "中華民國" };
			}
			else
			{
				saEraNames = new string[1] { string.Empty };
			}
			break;
		case CalendarId.KOREA:
			saEraNames = new string[1] { "단기" };
			break;
		case CalendarId.THAI:
			saEraNames = new string[1] { "พ.ศ." };
			break;
		case CalendarId.JAPAN:
		case CalendarId.JAPANESELUNISOLAR:
			saEraNames = JapaneseCalendar.EraNames();
			break;
		case CalendarId.PERSIAN:
			if (saEraNames == null || saEraNames.Length == 0 || string.IsNullOrEmpty(saEraNames[0]))
			{
				saEraNames = new string[1] { "ه.ش" };
			}
			break;
		default:
			saEraNames = Invariant.saEraNames;
			break;
		}
	}

	private void InitializeAbbreviatedEraNames(string localeName, CalendarId calendarId)
	{
		switch (calendarId)
		{
		case CalendarId.GREGORIAN:
			if (saAbbrevEraNames == null || saAbbrevEraNames.Length == 0 || string.IsNullOrEmpty(saAbbrevEraNames[0]))
			{
				saAbbrevEraNames = new string[1] { "AD" };
			}
			break;
		case CalendarId.GREGORIAN_US:
		case CalendarId.JULIAN:
			saAbbrevEraNames = new string[1] { "AD" };
			break;
		case CalendarId.JAPAN:
		case CalendarId.JAPANESELUNISOLAR:
			saAbbrevEraNames = JapaneseCalendar.AbbrevEraNames();
			break;
		case CalendarId.HIJRI:
		case CalendarId.UMALQURA:
			if (localeName == "dv-MV")
			{
				saAbbrevEraNames = new string[1] { "ހ." };
			}
			else
			{
				saAbbrevEraNames = new string[1] { "هـ" };
			}
			break;
		case CalendarId.TAIWAN:
			saAbbrevEraNames = new string[1];
			if (saEraNames[0].Length == 4)
			{
				saAbbrevEraNames[0] = saEraNames[0].Substring(2, 2);
			}
			else
			{
				saAbbrevEraNames[0] = saEraNames[0];
			}
			break;
		case CalendarId.PERSIAN:
			if (saAbbrevEraNames == null || saAbbrevEraNames.Length == 0 || string.IsNullOrEmpty(saAbbrevEraNames[0]))
			{
				saAbbrevEraNames = saEraNames;
			}
			break;
		default:
			saAbbrevEraNames = saEraNames;
			break;
		}
	}

	internal static int GetCalendarCurrentEra(Calendar calendar)
	{
		if (GlobalizationMode.Invariant)
		{
			return Invariant.iCurrentEra;
		}
		CalendarId baseCalendarID = calendar.BaseCalendarID;
		string name = CalendarIdToCultureName(baseCalendarID);
		return CultureInfo.GetCultureInfo(name)._cultureData.GetCalendar(baseCalendarID).iCurrentEra;
	}

	private static string CalendarIdToCultureName(CalendarId calendarId)
	{
		switch (calendarId)
		{
		case CalendarId.GREGORIAN_US:
			return "fa-IR";
		case CalendarId.JAPAN:
			return "ja-JP";
		case CalendarId.TAIWAN:
			return "zh-TW";
		case CalendarId.KOREA:
			return "ko-KR";
		case CalendarId.HIJRI:
		case CalendarId.GREGORIAN_ARABIC:
		case CalendarId.UMALQURA:
			return "ar-SA";
		case CalendarId.THAI:
			return "th-TH";
		case CalendarId.HEBREW:
			return "he-IL";
		case CalendarId.GREGORIAN_ME_FRENCH:
			return "ar-DZ";
		case CalendarId.GREGORIAN_XLIT_ENGLISH:
		case CalendarId.GREGORIAN_XLIT_FRENCH:
			return "ar-IQ";
		default:
			return "en-US";
		}
	}

	private static bool SystemSupportsTaiwaneseCalendar()
	{
		if (!GlobalizationMode.UseNls)
		{
			return IcuSystemSupportsTaiwaneseCalendar();
		}
		return NlsSystemSupportsTaiwaneseCalendar();
	}

	private bool IcuLoadCalendarDataFromSystem(string localeName, CalendarId calendarId)
	{
		bool flag = true;
		flag &= GetCalendarInfo(localeName, calendarId, CalendarDataType.NativeName, out sNativeName);
		flag &= GetCalendarInfo(localeName, calendarId, CalendarDataType.MonthDay, out sMonthDay);
		if (sMonthDay != null)
		{
			sMonthDay = NormalizeDatePattern(sMonthDay);
		}
		flag &= EnumDatePatterns(localeName, calendarId, CalendarDataType.ShortDates, out saShortDates);
		flag &= EnumDatePatterns(localeName, calendarId, CalendarDataType.LongDates, out saLongDates);
		flag &= EnumDatePatterns(localeName, calendarId, CalendarDataType.YearMonths, out saYearMonths);
		flag &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.DayNames, out saDayNames);
		flag &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.AbbrevDayNames, out saAbbrevDayNames);
		flag &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.SuperShortDayNames, out saSuperShortDayNames);
		string leapHebrewMonthName = null;
		flag &= EnumMonthNames(localeName, calendarId, CalendarDataType.MonthNames, out saMonthNames, ref leapHebrewMonthName);
		if (leapHebrewMonthName != null)
		{
			saLeapYearMonthNames = (string[])saMonthNames.Clone();
			saLeapYearMonthNames[6] = leapHebrewMonthName;
			saMonthNames[5] = saMonthNames[6];
			saMonthNames[6] = leapHebrewMonthName;
		}
		flag &= EnumMonthNames(localeName, calendarId, CalendarDataType.AbbrevMonthNames, out saAbbrevMonthNames, ref leapHebrewMonthName);
		flag &= EnumMonthNames(localeName, calendarId, CalendarDataType.MonthGenitiveNames, out saMonthGenitiveNames, ref leapHebrewMonthName);
		flag &= EnumMonthNames(localeName, calendarId, CalendarDataType.AbbrevMonthGenitiveNames, out saAbbrevMonthGenitiveNames, ref leapHebrewMonthName);
		flag &= EnumEraNames(localeName, calendarId, CalendarDataType.EraNames, out saEraNames);
		return flag & EnumEraNames(localeName, calendarId, CalendarDataType.AbbrevEraNames, out saAbbrevEraNames);
	}

	internal static int IcuGetTwoDigitYearMax(CalendarId calendarId)
	{
		return -1;
	}

	internal static int IcuGetCalendars(string localeName, CalendarId[] calendars)
	{
		int num = Interop.Globalization.GetCalendars(localeName, calendars, calendars.Length);
		if (num == 0 && calendars.Length != 0)
		{
			calendars[0] = CalendarId.GREGORIAN;
			num = 1;
		}
		return num;
	}

	private static bool IcuSystemSupportsTaiwaneseCalendar()
	{
		return true;
	}

	private unsafe static bool GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, out string calendarString)
	{
		return Interop.CallStringMethod(delegate(Span<char> buffer, string locale, CalendarId id, CalendarDataType type)
		{
			fixed (char* result = buffer)
			{
				return Interop.Globalization.GetCalendarInfo(locale, id, type, result, buffer.Length);
			}
		}, localeName, calendarId, dataType, out calendarString);
	}

	private static bool EnumDatePatterns(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[] datePatterns)
	{
		datePatterns = null;
		IcuEnumCalendarsData callbackContext = default(IcuEnumCalendarsData);
		callbackContext.Results = new List<string>();
		callbackContext.DisallowDuplicates = true;
		bool flag = EnumCalendarInfo(localeName, calendarId, dataType, ref callbackContext);
		if (flag)
		{
			List<string> results = callbackContext.Results;
			for (int i = 0; i < results.Count; i++)
			{
				results[i] = NormalizeDatePattern(results[i]);
			}
			if (dataType == CalendarDataType.ShortDates)
			{
				FixDefaultShortDatePattern(results);
			}
			datePatterns = results.ToArray();
		}
		return flag;
	}

	private static void FixDefaultShortDatePattern(List<string> shortDatePatterns)
	{
		if (shortDatePatterns.Count == 0)
		{
			return;
		}
		string text = shortDatePatterns[0];
		if (text.Length > 100)
		{
			return;
		}
		Span<char> span = stackalloc char[text.Length + 2];
		int i;
		for (i = 0; i < text.Length; i++)
		{
			if (text[i] == '\'')
			{
				do
				{
					span[i] = text[i];
					i++;
				}
				while (i < text.Length && text[i] != '\'');
				if (i >= text.Length)
				{
					return;
				}
			}
			else if (text[i] == 'y')
			{
				span[i] = 'y';
				break;
			}
			span[i] = text[i];
		}
		if (i >= text.Length - 1 || text[i + 1] != 'y' || (i + 2 < text.Length && text[i + 2] == 'y'))
		{
			return;
		}
		span[i + 1] = 'y';
		span[i + 2] = 'y';
		span[i + 3] = 'y';
		for (i += 2; i < text.Length; i++)
		{
			span[i + 2] = text[i];
		}
		shortDatePatterns[0] = span.ToString();
		for (int j = 1; j < shortDatePatterns.Count; j++)
		{
			if (shortDatePatterns[j] == shortDatePatterns[0])
			{
				shortDatePatterns[j] = text;
				return;
			}
		}
		shortDatePatterns.Add(text);
	}

	private static string NormalizeDatePattern(string input)
	{
		StringBuilder stringBuilder = StringBuilderCache.Acquire(input.Length);
		int index = 0;
		while (index < input.Length)
		{
			switch (input[index])
			{
			case '\'':
				stringBuilder.Append(input[index++]);
				while (index < input.Length)
				{
					char c = input[index++];
					stringBuilder.Append(c);
					if (c == '\'')
					{
						break;
					}
				}
				break;
			case 'E':
			case 'c':
			case 'e':
				NormalizeDayOfWeek(input, stringBuilder, ref index);
				break;
			case 'L':
			case 'M':
			{
				int num = CountOccurrences(input, input[index], ref index);
				if (num > 4)
				{
					num = 3;
				}
				stringBuilder.Append('M', num);
				break;
			}
			case 'G':
			{
				int num = CountOccurrences(input, 'G', ref index);
				stringBuilder.Append('g');
				break;
			}
			case 'y':
			{
				int num = CountOccurrences(input, 'y', ref index);
				if (num == 1)
				{
					num = 4;
				}
				stringBuilder.Append('y', num);
				break;
			}
			default:
				stringBuilder.Append(input[index++]);
				break;
			}
		}
		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	private static void NormalizeDayOfWeek(string input, StringBuilder destination, ref int index)
	{
		char value = input[index];
		int val = CountOccurrences(input, value, ref index);
		val = Math.Max(val, 3);
		if (val > 4)
		{
			val = 3;
		}
		destination.Append('d', val);
	}

	private static int CountOccurrences(string input, char value, ref int index)
	{
		int num = index;
		while (index < input.Length && input[index] == value)
		{
			index++;
		}
		return index - num;
	}

	private static bool EnumMonthNames(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[] monthNames, ref string leapHebrewMonthName)
	{
		monthNames = null;
		IcuEnumCalendarsData callbackContext = default(IcuEnumCalendarsData);
		callbackContext.Results = new List<string>();
		bool flag = EnumCalendarInfo(localeName, calendarId, dataType, ref callbackContext);
		if (flag)
		{
			if (callbackContext.Results.Count == 12)
			{
				callbackContext.Results.Add(string.Empty);
			}
			if (callbackContext.Results.Count > 13)
			{
				if (calendarId == CalendarId.HEBREW)
				{
					leapHebrewMonthName = callbackContext.Results[13];
				}
				callbackContext.Results.RemoveRange(13, callbackContext.Results.Count - 13);
			}
			monthNames = callbackContext.Results.ToArray();
		}
		return flag;
	}

	private static bool EnumEraNames(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[] eraNames)
	{
		bool result = EnumCalendarInfo(localeName, calendarId, dataType, out eraNames);
		if (calendarId != CalendarId.JAPAN && calendarId != CalendarId.JAPANESELUNISOLAR)
		{
			string[] obj = eraNames;
			if (obj != null && obj.Length != 0)
			{
				string[] array = new string[1] { eraNames[eraNames.Length - 1] };
				eraNames = array;
			}
		}
		return result;
	}

	internal static bool EnumCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[] calendarData)
	{
		calendarData = null;
		IcuEnumCalendarsData callbackContext = default(IcuEnumCalendarsData);
		callbackContext.Results = new List<string>();
		bool flag = EnumCalendarInfo(localeName, calendarId, dataType, ref callbackContext);
		if (flag)
		{
			calendarData = callbackContext.Results.ToArray();
		}
		return flag;
	}

	private unsafe static bool EnumCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, ref IcuEnumCalendarsData callbackContext)
	{
		return Interop.Globalization.EnumCalendarInfo((delegate* unmanaged<char*, IntPtr, void>)(delegate*<char*, IntPtr, void>)(&EnumCalendarInfoCallback), localeName, calendarId, dataType, (IntPtr)Unsafe.AsPointer(ref callbackContext));
	}

	[UnmanagedCallersOnly]
	private unsafe static void EnumCalendarInfoCallback(char* calendarStringPtr, IntPtr context)
	{
		try
		{
			ReadOnlySpan<char> strA = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(calendarStringPtr);
			ref IcuEnumCalendarsData reference = ref Unsafe.As<byte, IcuEnumCalendarsData>(ref *(byte*)(void*)context);
			if (reference.DisallowDuplicates)
			{
				foreach (string result in reference.Results)
				{
					if (string.CompareOrdinal(strA, result) == 0)
					{
						return;
					}
				}
			}
			reference.Results.Add(strA.ToString());
		}
		catch (Exception)
		{
		}
	}

	internal static int NlsGetTwoDigitYearMax(CalendarId calendarId)
	{
		if (!GlobalizationMode.Invariant)
		{
			if (!CallGetCalendarInfoEx((string)null, calendarId, 48u, out int data))
			{
				return -1;
			}
			return data;
		}
		return Invariant.iTwoDigitYearMax;
	}

	private static bool NlsSystemSupportsTaiwaneseCalendar()
	{
		string data;
		return CallGetCalendarInfoEx("zh-TW", CalendarId.TAIWAN, 2u, out data);
	}

	private static bool CallGetCalendarInfoEx(string localeName, CalendarId calendar, uint calType, out int data)
	{
		return Interop.Kernel32.GetCalendarInfoEx(localeName, (uint)calendar, IntPtr.Zero, calType | 0x20000000u, IntPtr.Zero, 0, out data) != 0;
	}

	private unsafe static bool CallGetCalendarInfoEx(string localeName, CalendarId calendar, uint calType, out string data)
	{
		char* ptr = stackalloc char[80];
		int num = Interop.Kernel32.GetCalendarInfoEx(localeName, (uint)calendar, IntPtr.Zero, calType, (IntPtr)ptr, 80, IntPtr.Zero);
		if (num > 0)
		{
			if (ptr[num - 1] == '\0')
			{
				num--;
			}
			data = new string(ptr, 0, num);
			return true;
		}
		data = "";
		return false;
	}

	[UnmanagedCallersOnly]
	private unsafe static Interop.BOOL EnumCalendarInfoCallback(char* lpCalendarInfoString, uint calendar, IntPtr pReserved, void* lParam)
	{
		ref EnumData reference = ref Unsafe.As<byte, EnumData>(ref *(byte*)lParam);
		try
		{
			string text = new string(lpCalendarInfoString);
			if (reference.userOverride != text)
			{
				reference.strings.Add(text);
			}
			return Interop.BOOL.TRUE;
		}
		catch (Exception)
		{
			return Interop.BOOL.FALSE;
		}
	}

	[UnmanagedCallersOnly]
	private unsafe static Interop.BOOL EnumCalendarsCallback(char* lpCalendarInfoString, uint calendar, IntPtr reserved, void* lParam)
	{
		ref NlsEnumCalendarsData reference = ref Unsafe.As<byte, NlsEnumCalendarsData>(ref *(byte*)lParam);
		try
		{
			if (reference.userOverride != calendar)
			{
				reference.calendars.Add((int)calendar);
			}
			return Interop.BOOL.TRUE;
		}
		catch (Exception)
		{
			return Interop.BOOL.FALSE;
		}
	}

	private bool LoadCalendarDataFromSystemCore(string localeName, CalendarId calendarId)
	{
		if (GlobalizationMode.UseNls)
		{
			return NlsLoadCalendarDataFromSystem(localeName, calendarId);
		}
		bool flag = IcuLoadCalendarDataFromSystem(localeName, calendarId);
		if (flag && bUseUserOverrides)
		{
			NormalizeCalendarId(ref calendarId, ref localeName);
			flag &= CallGetCalendarInfoEx(localeName, calendarId, 48u, out iTwoDigitYearMax);
			CalendarId calendarId2 = (CalendarId)CultureData.GetLocaleInfoExInt(localeName, 4105u);
			if (calendarId2 == calendarId)
			{
				string value = CultureData.ReescapeWin32String(CultureData.GetLocaleInfoEx(localeName, 31u));
				string value2 = CultureData.ReescapeWin32String(CultureData.GetLocaleInfoEx(localeName, 32u));
				InsertOrSwapOverride(value, ref saShortDates);
				InsertOrSwapOverride(value2, ref saLongDates);
			}
		}
		return flag;
	}

	private void InsertOrSwapOverride(string value, ref string[] destination)
	{
		if (value == null)
		{
			return;
		}
		for (int i = 0; i < destination.Length; i++)
		{
			if (destination[i] == value)
			{
				if (i > 0)
				{
					string text = destination[0];
					destination[0] = value;
					destination[i] = text;
				}
				return;
			}
		}
		string[] array = new string[destination.Length + 1];
		array[0] = value;
		Array.Copy(destination, 0, array, 1, destination.Length);
		destination = array;
	}

	private bool NlsLoadCalendarDataFromSystem(string localeName, CalendarId calendarId)
	{
		bool flag = true;
		uint num = ((!bUseUserOverrides) ? 2147483648u : 0u);
		NormalizeCalendarId(ref calendarId, ref localeName);
		flag &= CallGetCalendarInfoEx(localeName, calendarId, 0x30u | num, out iTwoDigitYearMax);
		flag &= CallGetCalendarInfoEx(localeName, calendarId, 2u, out sNativeName);
		flag &= CallGetCalendarInfoEx(localeName, calendarId, 56u, out sMonthDay);
		flag &= CallEnumCalendarInfo(localeName, calendarId, 5u, 0x1Fu | num, out saShortDates);
		flag &= CallEnumCalendarInfo(localeName, calendarId, 6u, 0x20u | num, out saLongDates);
		flag &= CallEnumCalendarInfo(localeName, calendarId, 47u, 4102u, out saYearMonths);
		flag &= GetCalendarDayInfo(localeName, calendarId, 13u, out saDayNames);
		flag &= GetCalendarDayInfo(localeName, calendarId, 20u, out saAbbrevDayNames);
		flag &= GetCalendarMonthInfo(localeName, calendarId, 21u, out saMonthNames);
		flag &= GetCalendarMonthInfo(localeName, calendarId, 34u, out saAbbrevMonthNames);
		GetCalendarDayInfo(localeName, calendarId, 55u, out saSuperShortDayNames);
		if (calendarId == CalendarId.GREGORIAN)
		{
			GetCalendarMonthInfo(localeName, calendarId, 268435477u, out saMonthGenitiveNames);
			GetCalendarMonthInfo(localeName, calendarId, 268435490u, out saAbbrevMonthGenitiveNames);
		}
		CallEnumCalendarInfo(localeName, calendarId, 4u, 0u, out saEraNames);
		CallEnumCalendarInfo(localeName, calendarId, 57u, 0u, out saAbbrevEraNames);
		saShortDates = CultureData.ReescapeWin32Strings(saShortDates);
		saLongDates = CultureData.ReescapeWin32Strings(saLongDates);
		saYearMonths = CultureData.ReescapeWin32Strings(saYearMonths);
		sMonthDay = CultureData.ReescapeWin32String(sMonthDay);
		return flag;
	}

	private static void NormalizeCalendarId(ref CalendarId calendarId, ref string localeName)
	{
		switch (calendarId)
		{
		case CalendarId.JAPANESELUNISOLAR:
			calendarId = CalendarId.JAPAN;
			break;
		case CalendarId.JULIAN:
		case CalendarId.CHINESELUNISOLAR:
		case CalendarId.SAKA:
		case CalendarId.LUNAR_ETO_CHN:
		case CalendarId.LUNAR_ETO_KOR:
		case CalendarId.LUNAR_ETO_ROKUYOU:
		case CalendarId.KOREANLUNISOLAR:
		case CalendarId.TAIWANLUNISOLAR:
			calendarId = CalendarId.GREGORIAN_US;
			break;
		}
		CheckSpecialCalendar(ref calendarId, ref localeName);
	}

	private static void CheckSpecialCalendar(ref CalendarId calendar, ref string localeName)
	{
		switch (calendar)
		{
		case CalendarId.GREGORIAN_US:
		{
			if (!CallGetCalendarInfoEx(localeName, calendar, 2u, out string data))
			{
				localeName = "fa-IR";
				if (!CallGetCalendarInfoEx(localeName, calendar, 2u, out data))
				{
					localeName = "en-US";
					calendar = CalendarId.GREGORIAN;
				}
			}
			break;
		}
		case CalendarId.TAIWAN:
			if (!NlsSystemSupportsTaiwaneseCalendar())
			{
				calendar = CalendarId.GREGORIAN;
			}
			break;
		}
	}

	private unsafe static bool CallEnumCalendarInfo(string localeName, CalendarId calendar, uint calType, uint lcType, out string[] data)
	{
		EnumData value = default(EnumData);
		value.userOverride = null;
		value.strings = new List<string>();
		if (lcType != 0 && (lcType & 0x80000000u) == 0)
		{
			CalendarId calendarId = (CalendarId)CultureData.GetLocaleInfoExInt(localeName, 4105u);
			if (calendarId == calendar)
			{
				string localeInfoEx = CultureData.GetLocaleInfoEx(localeName, lcType);
				if (localeInfoEx != null)
				{
					value.userOverride = localeInfoEx;
					value.strings.Add(localeInfoEx);
				}
			}
		}
		Interop.Kernel32.EnumCalendarInfoExEx((delegate* unmanaged<char*, uint, IntPtr, void*, Interop.BOOL>)(delegate*<char*, uint, IntPtr, void*, Interop.BOOL>)(&EnumCalendarInfoCallback), localeName, (uint)calendar, null, calType, Unsafe.AsPointer(ref value));
		if (value.strings.Count == 0)
		{
			data = null;
			return false;
		}
		string[] array = value.strings.ToArray();
		if (calType == 57 || calType == 4)
		{
			Array.Reverse(array, 0, array.Length);
		}
		data = array;
		return true;
	}

	private static bool GetCalendarDayInfo(string localeName, CalendarId calendar, uint calType, out string[] outputStrings)
	{
		bool flag = true;
		string[] array = new string[7];
		int num = 0;
		while (num < 7)
		{
			flag &= CallGetCalendarInfoEx(localeName, calendar, calType, out array[num]);
			if (num == 0)
			{
				calType -= 7;
			}
			num++;
			calType++;
		}
		outputStrings = array;
		return flag;
	}

	private static bool GetCalendarMonthInfo(string localeName, CalendarId calendar, uint calType, out string[] outputStrings)
	{
		string[] array = new string[13];
		int num = 0;
		while (num < 13)
		{
			if (!CallGetCalendarInfoEx(localeName, calendar, calType, out array[num]))
			{
				array[num] = "";
			}
			num++;
			calType++;
		}
		outputStrings = array;
		return true;
	}

	internal static int GetCalendarsCore(string localeName, bool useUserOverride, CalendarId[] calendars)
	{
		if (GlobalizationMode.UseNls)
		{
			return NlsGetCalendars(localeName, useUserOverride, calendars);
		}
		int num = IcuGetCalendars(localeName, calendars);
		if (useUserOverride)
		{
			int localeInfoExInt = CultureData.GetLocaleInfoExInt(localeName, 4105u);
			if (localeInfoExInt != 0 && (uint)(ushort)localeInfoExInt != (uint)calendars[0])
			{
				CalendarId calendarId = (CalendarId)localeInfoExInt;
				for (int i = 1; i < calendars.Length; i++)
				{
					if (calendars[i] == calendarId)
					{
						CalendarId calendarId2 = calendars[0];
						calendars[0] = calendarId;
						calendars[i] = calendarId2;
						return num;
					}
				}
				num = ((num < calendars.Length) ? (num + 1) : num);
				Span<CalendarId> span = stackalloc CalendarId[num];
				span[0] = calendarId;
				calendars.AsSpan(0, num - 1).CopyTo(span.Slice(1));
				span.CopyTo(calendars);
			}
		}
		return num;
	}

	private unsafe static int NlsGetCalendars(string localeName, bool useUserOverride, CalendarId[] calendars)
	{
		NlsEnumCalendarsData value = default(NlsEnumCalendarsData);
		value.userOverride = 0;
		value.calendars = new List<int>();
		if (useUserOverride)
		{
			int localeInfoExInt = CultureData.GetLocaleInfoExInt(localeName, 4105u);
			if (localeInfoExInt != 0)
			{
				value.userOverride = localeInfoExInt;
				value.calendars.Add(localeInfoExInt);
			}
		}
		Interop.Kernel32.EnumCalendarInfoExEx((delegate* unmanaged<char*, uint, IntPtr, void*, Interop.BOOL>)(delegate*<char*, uint, IntPtr, void*, Interop.BOOL>)(&EnumCalendarsCallback), localeName, uint.MaxValue, null, 1u, Unsafe.AsPointer(ref value));
		for (int i = 0; i < Math.Min(calendars.Length, value.calendars.Count); i++)
		{
			calendars[i] = (CalendarId)value.calendars[i];
		}
		return value.calendars.Count;
	}
}
