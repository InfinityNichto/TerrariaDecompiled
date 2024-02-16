using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Globalization;

public sealed class DateTimeFormatInfo : IFormatProvider, ICloneable
{
	internal sealed class TokenHashValue
	{
		internal string tokenString;

		internal TokenType tokenType;

		internal int tokenValue;

		internal TokenHashValue(string tokenString, TokenType tokenType, int tokenValue)
		{
			this.tokenString = tokenString;
			this.tokenType = tokenType;
			this.tokenValue = tokenValue;
		}
	}

	private static volatile DateTimeFormatInfo s_invariantInfo;

	private readonly CultureData _cultureData;

	private string _name;

	private string _langName;

	private CompareInfo _compareInfo;

	private CultureInfo _cultureInfo;

	private string amDesignator;

	private string pmDesignator;

	private string dateSeparator;

	private string generalShortTimePattern;

	private string generalLongTimePattern;

	private string timeSeparator;

	private string monthDayPattern;

	private string dateTimeOffsetPattern;

	private Calendar calendar;

	private int firstDayOfWeek = -1;

	private int calendarWeekRule = -1;

	private string fullDateTimePattern;

	private string[] abbreviatedDayNames;

	private string[] m_superShortDayNames;

	private string[] dayNames;

	private string[] abbreviatedMonthNames;

	private string[] monthNames;

	private string[] genitiveMonthNames;

	private string[] m_genitiveAbbreviatedMonthNames;

	private string[] leapYearMonthNames;

	private string longDatePattern;

	private string shortDatePattern;

	private string yearMonthPattern;

	private string longTimePattern;

	private string shortTimePattern;

	private string[] allYearMonthPatterns;

	private string[] allShortDatePatterns;

	private string[] allLongDatePatterns;

	private string[] allShortTimePatterns;

	private string[] allLongTimePatterns;

	private string[] m_eraNames;

	private string[] m_abbrevEraNames;

	private string[] m_abbrevEnglishEraNames;

	private CalendarId[] optionalCalendars;

	internal bool _isReadOnly;

	private DateTimeFormatFlags formatFlags = DateTimeFormatFlags.NotInitialized;

	private string _decimalSeparator;

	private string _fullTimeSpanPositivePattern;

	private string _fullTimeSpanNegativePattern;

	private TokenHashValue[] _dtfiTokenHash;

	private static volatile DateTimeFormatInfo s_jajpDTFI;

	private static volatile DateTimeFormatInfo s_zhtwDTFI;

	private string CultureName => _name ?? (_name = _cultureData.CultureName);

	private CultureInfo Culture => _cultureInfo ?? (_cultureInfo = CultureInfo.GetCultureInfo(CultureName));

	private string LanguageName => _langName ?? (_langName = _cultureData.TwoLetterISOLanguageName);

	public static DateTimeFormatInfo InvariantInfo
	{
		get
		{
			if (s_invariantInfo == null)
			{
				DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
				dateTimeFormatInfo.Calendar.SetReadOnlyState(readOnly: true);
				dateTimeFormatInfo._isReadOnly = true;
				s_invariantInfo = dateTimeFormatInfo;
			}
			return s_invariantInfo;
		}
	}

	public static DateTimeFormatInfo CurrentInfo
	{
		get
		{
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
			if (!currentCulture._isInherited)
			{
				DateTimeFormatInfo dateTimeInfo = currentCulture._dateTimeInfo;
				if (dateTimeInfo != null)
				{
					return dateTimeInfo;
				}
			}
			return (DateTimeFormatInfo)currentCulture.GetFormat(typeof(DateTimeFormatInfo));
		}
	}

	public string AMDesignator
	{
		get
		{
			if (amDesignator == null)
			{
				amDesignator = _cultureData.AMDesignator;
			}
			return amDesignator;
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			ClearTokenHashTable();
			amDesignator = value;
		}
	}

	public Calendar Calendar
	{
		get
		{
			return calendar;
		}
		[MemberNotNull("calendar")]
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value == calendar)
			{
				return;
			}
			for (int i = 0; i < OptionalCalendars.Length; i++)
			{
				if (OptionalCalendars[i] == value.ID)
				{
					if (calendar != null)
					{
						m_eraNames = null;
						m_abbrevEraNames = null;
						m_abbrevEnglishEraNames = null;
						monthDayPattern = null;
						dayNames = null;
						abbreviatedDayNames = null;
						m_superShortDayNames = null;
						monthNames = null;
						abbreviatedMonthNames = null;
						genitiveMonthNames = null;
						m_genitiveAbbreviatedMonthNames = null;
						leapYearMonthNames = null;
						formatFlags = DateTimeFormatFlags.NotInitialized;
						allShortDatePatterns = null;
						allLongDatePatterns = null;
						allYearMonthPatterns = null;
						dateTimeOffsetPattern = null;
						longDatePattern = null;
						shortDatePattern = null;
						yearMonthPattern = null;
						fullDateTimePattern = null;
						generalShortTimePattern = null;
						generalLongTimePattern = null;
						dateSeparator = null;
						ClearTokenHashTable();
					}
					calendar = value;
					InitializeOverridableProperties(_cultureData, calendar.ID);
					return;
				}
			}
			throw new ArgumentOutOfRangeException("value", value, SR.Argument_InvalidCalendar);
		}
	}

	private CalendarId[] OptionalCalendars => optionalCalendars ?? (optionalCalendars = _cultureData.CalendarIds);

	internal string[] EraNames => m_eraNames ?? (m_eraNames = _cultureData.EraNames(Calendar.ID));

	internal string[] AbbreviatedEraNames => m_abbrevEraNames ?? (m_abbrevEraNames = _cultureData.AbbrevEraNames(Calendar.ID));

	internal string[] AbbreviatedEnglishEraNames
	{
		get
		{
			if (m_abbrevEnglishEraNames == null)
			{
				m_abbrevEnglishEraNames = _cultureData.AbbreviatedEnglishEraNames(Calendar.ID);
			}
			return m_abbrevEnglishEraNames;
		}
	}

	public string DateSeparator
	{
		get
		{
			if (dateSeparator == null)
			{
				dateSeparator = _cultureData.DateSeparator(Calendar.ID);
			}
			return dateSeparator;
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			ClearTokenHashTable();
			dateSeparator = value;
		}
	}

	public DayOfWeek FirstDayOfWeek
	{
		get
		{
			if (firstDayOfWeek == -1)
			{
				firstDayOfWeek = _cultureData.FirstDayOfWeek;
			}
			return (DayOfWeek)firstDayOfWeek;
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value < DayOfWeek.Sunday || value > DayOfWeek.Saturday)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, DayOfWeek.Sunday, DayOfWeek.Saturday));
			}
			firstDayOfWeek = (int)value;
		}
	}

	public CalendarWeekRule CalendarWeekRule
	{
		get
		{
			if (calendarWeekRule == -1)
			{
				calendarWeekRule = _cultureData.CalendarWeekRule;
			}
			return (CalendarWeekRule)calendarWeekRule;
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value < CalendarWeekRule.FirstDay || value > CalendarWeekRule.FirstFourDayWeek)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, CalendarWeekRule.FirstDay, CalendarWeekRule.FirstFourDayWeek));
			}
			calendarWeekRule = (int)value;
		}
	}

	public string FullDateTimePattern
	{
		get
		{
			return fullDateTimePattern ?? (fullDateTimePattern = LongDatePattern + " " + LongTimePattern);
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			fullDateTimePattern = value;
		}
	}

	public string LongDatePattern
	{
		get
		{
			return longDatePattern ?? (longDatePattern = UnclonedLongDatePatterns[0]);
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			longDatePattern = value;
			OnLongDatePatternChanged();
		}
	}

	public string LongTimePattern
	{
		get
		{
			return longTimePattern ?? (longTimePattern = UnclonedLongTimePatterns[0]);
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			longTimePattern = value;
			OnLongTimePatternChanged();
		}
	}

	public string MonthDayPattern
	{
		get
		{
			if (monthDayPattern == null)
			{
				monthDayPattern = _cultureData.MonthDay(Calendar.ID);
			}
			return monthDayPattern;
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			monthDayPattern = value;
		}
	}

	public string PMDesignator
	{
		get
		{
			if (pmDesignator == null)
			{
				pmDesignator = _cultureData.PMDesignator;
			}
			return pmDesignator;
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			ClearTokenHashTable();
			pmDesignator = value;
		}
	}

	public string RFC1123Pattern => "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

	public string ShortDatePattern
	{
		get
		{
			return shortDatePattern ?? (shortDatePattern = UnclonedShortDatePatterns[0]);
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			shortDatePattern = value;
			OnShortDatePatternChanged();
		}
	}

	public string ShortTimePattern
	{
		get
		{
			return shortTimePattern ?? (shortTimePattern = UnclonedShortTimePatterns[0]);
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			shortTimePattern = value;
			OnShortTimePatternChanged();
		}
	}

	public string SortableDateTimePattern => "yyyy'-'MM'-'dd'T'HH':'mm':'ss";

	internal string GeneralShortTimePattern => generalShortTimePattern ?? (generalShortTimePattern = ShortDatePattern + " " + ShortTimePattern);

	internal string GeneralLongTimePattern => generalLongTimePattern ?? (generalLongTimePattern = ShortDatePattern + " " + LongTimePattern);

	internal string DateTimeOffsetPattern
	{
		get
		{
			if (dateTimeOffsetPattern == null)
			{
				bool flag = false;
				bool flag2 = false;
				char c = '\'';
				int num = 0;
				while (!flag && num < LongTimePattern.Length)
				{
					switch (LongTimePattern[num])
					{
					case 'z':
						flag = !flag2;
						break;
					case '"':
					case '\'':
						if (flag2 && c == LongTimePattern[num])
						{
							flag2 = false;
						}
						else if (!flag2)
						{
							c = LongTimePattern[num];
							flag2 = true;
						}
						break;
					case '%':
					case '\\':
						num++;
						break;
					}
					num++;
				}
				dateTimeOffsetPattern = (flag ? (ShortDatePattern + " " + LongTimePattern) : (ShortDatePattern + " " + LongTimePattern + " zzz"));
			}
			return dateTimeOffsetPattern;
		}
	}

	public string TimeSeparator
	{
		get
		{
			if (timeSeparator == null)
			{
				timeSeparator = _cultureData.TimeSeparator;
			}
			return timeSeparator;
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			ClearTokenHashTable();
			timeSeparator = value;
		}
	}

	public string UniversalSortableDateTimePattern => "yyyy'-'MM'-'dd HH':'mm':'ss'Z'";

	public string YearMonthPattern
	{
		get
		{
			return yearMonthPattern ?? (yearMonthPattern = UnclonedYearMonthPatterns[0]);
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			yearMonthPattern = value;
			OnYearMonthPatternChanged();
		}
	}

	public string[] AbbreviatedDayNames
	{
		get
		{
			return (string[])InternalGetAbbreviatedDayOfWeekNames().Clone();
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length != 7)
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 7), "value");
			}
			CheckNullValue(value, value.Length);
			ClearTokenHashTable();
			abbreviatedDayNames = value;
		}
	}

	public string[] ShortestDayNames
	{
		get
		{
			return (string[])InternalGetSuperShortDayNames().Clone();
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length != 7)
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 7), "value");
			}
			CheckNullValue(value, value.Length);
			m_superShortDayNames = value;
		}
	}

	public string[] DayNames
	{
		get
		{
			return (string[])InternalGetDayOfWeekNames().Clone();
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length != 7)
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 7), "value");
			}
			CheckNullValue(value, value.Length);
			ClearTokenHashTable();
			dayNames = value;
		}
	}

	public string[] AbbreviatedMonthNames
	{
		get
		{
			return (string[])InternalGetAbbreviatedMonthNames().Clone();
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length != 13)
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), "value");
			}
			CheckNullValue(value, value.Length - 1);
			ClearTokenHashTable();
			abbreviatedMonthNames = value;
		}
	}

	public string[] MonthNames
	{
		get
		{
			return (string[])InternalGetMonthNames().Clone();
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length != 13)
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), "value");
			}
			CheckNullValue(value, value.Length - 1);
			monthNames = value;
			ClearTokenHashTable();
		}
	}

	internal bool HasSpacesInMonthNames => (FormatFlags & DateTimeFormatFlags.UseSpacesInMonthNames) != 0;

	internal bool HasSpacesInDayNames => (FormatFlags & DateTimeFormatFlags.UseSpacesInDayNames) != 0;

	private string[] AllYearMonthPatterns => GetMergedPatterns(UnclonedYearMonthPatterns, YearMonthPattern);

	private string[] AllShortDatePatterns => GetMergedPatterns(UnclonedShortDatePatterns, ShortDatePattern);

	private string[] AllShortTimePatterns => GetMergedPatterns(UnclonedShortTimePatterns, ShortTimePattern);

	private string[] AllLongDatePatterns => GetMergedPatterns(UnclonedLongDatePatterns, LongDatePattern);

	private string[] AllLongTimePatterns => GetMergedPatterns(UnclonedLongTimePatterns, LongTimePattern);

	private string[] UnclonedYearMonthPatterns
	{
		get
		{
			if (allYearMonthPatterns == null)
			{
				allYearMonthPatterns = _cultureData.YearMonths(Calendar.ID);
			}
			return allYearMonthPatterns;
		}
	}

	private string[] UnclonedShortDatePatterns
	{
		get
		{
			if (allShortDatePatterns == null)
			{
				allShortDatePatterns = _cultureData.ShortDates(Calendar.ID);
			}
			return allShortDatePatterns;
		}
	}

	private string[] UnclonedLongDatePatterns
	{
		get
		{
			if (allLongDatePatterns == null)
			{
				allLongDatePatterns = _cultureData.LongDates(Calendar.ID);
			}
			return allLongDatePatterns;
		}
	}

	private string[] UnclonedShortTimePatterns
	{
		get
		{
			if (allShortTimePatterns == null)
			{
				allShortTimePatterns = _cultureData.ShortTimes;
			}
			return allShortTimePatterns;
		}
	}

	private string[] UnclonedLongTimePatterns
	{
		get
		{
			if (allLongTimePatterns == null)
			{
				allLongTimePatterns = _cultureData.LongTimes;
			}
			return allLongTimePatterns;
		}
	}

	public bool IsReadOnly => _isReadOnly;

	public string NativeCalendarName => _cultureData.CalendarName(Calendar.ID);

	public string[] AbbreviatedMonthGenitiveNames
	{
		get
		{
			return (string[])InternalGetGenitiveMonthNames(abbreviated: true).Clone();
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length != 13)
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), "value");
			}
			CheckNullValue(value, value.Length - 1);
			ClearTokenHashTable();
			m_genitiveAbbreviatedMonthNames = value;
		}
	}

	public string[] MonthGenitiveNames
	{
		get
		{
			return (string[])InternalGetGenitiveMonthNames(abbreviated: false).Clone();
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length != 13)
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), "value");
			}
			CheckNullValue(value, value.Length - 1);
			genitiveMonthNames = value;
			ClearTokenHashTable();
		}
	}

	internal string DecimalSeparator => _decimalSeparator ?? (_decimalSeparator = new NumberFormatInfo(_cultureData.UseUserOverride ? CultureData.GetCultureData(_cultureData.CultureName, useUserOverride: false) : _cultureData).NumberDecimalSeparator);

	internal string FullTimeSpanPositivePattern => _fullTimeSpanPositivePattern ?? (_fullTimeSpanPositivePattern = "d':'h':'mm':'ss'" + DecimalSeparator + "'FFFFFFF");

	internal string FullTimeSpanNegativePattern => _fullTimeSpanNegativePattern ?? (_fullTimeSpanNegativePattern = "'-'" + FullTimeSpanPositivePattern);

	internal CompareInfo CompareInfo => _compareInfo ?? (_compareInfo = System.Globalization.CompareInfo.GetCompareInfo(_cultureData.SortName));

	internal DateTimeFormatFlags FormatFlags
	{
		get
		{
			if (formatFlags == DateTimeFormatFlags.NotInitialized)
			{
				return InitializeFormatFlags();
			}
			return formatFlags;
		}
	}

	internal bool HasForceTwoDigitYears
	{
		get
		{
			CalendarId iD = calendar.ID;
			if (iD - 3 <= CalendarId.GREGORIAN)
			{
				return true;
			}
			return false;
		}
	}

	internal bool HasYearMonthAdjustment => (FormatFlags & DateTimeFormatFlags.UseHebrewRule) != 0;

	private string[] InternalGetAbbreviatedDayOfWeekNames()
	{
		return abbreviatedDayNames ?? InternalGetAbbreviatedDayOfWeekNamesCore();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private string[] InternalGetAbbreviatedDayOfWeekNamesCore()
	{
		abbreviatedDayNames = _cultureData.AbbreviatedDayNames(Calendar.ID);
		return abbreviatedDayNames;
	}

	private string[] InternalGetSuperShortDayNames()
	{
		return m_superShortDayNames ?? InternalGetSuperShortDayNamesCore();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private string[] InternalGetSuperShortDayNamesCore()
	{
		m_superShortDayNames = _cultureData.SuperShortDayNames(Calendar.ID);
		return m_superShortDayNames;
	}

	private string[] InternalGetDayOfWeekNames()
	{
		return dayNames ?? InternalGetDayOfWeekNamesCore();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private string[] InternalGetDayOfWeekNamesCore()
	{
		dayNames = _cultureData.DayNames(Calendar.ID);
		return dayNames;
	}

	private string[] InternalGetAbbreviatedMonthNames()
	{
		return abbreviatedMonthNames ?? InternalGetAbbreviatedMonthNamesCore();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private string[] InternalGetAbbreviatedMonthNamesCore()
	{
		abbreviatedMonthNames = _cultureData.AbbreviatedMonthNames(Calendar.ID);
		return abbreviatedMonthNames;
	}

	private string[] InternalGetMonthNames()
	{
		return monthNames ?? internalGetMonthNamesCore();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private string[] internalGetMonthNamesCore()
	{
		monthNames = _cultureData.MonthNames(Calendar.ID);
		return monthNames;
	}

	public DateTimeFormatInfo()
		: this(CultureInfo.InvariantCulture._cultureData, GregorianCalendar.GetDefaultInstance())
	{
	}

	internal DateTimeFormatInfo(CultureData cultureData, Calendar cal)
	{
		_cultureData = cultureData;
		calendar = cal;
		InitializeOverridableProperties(cultureData, calendar.ID);
	}

	private void InitializeOverridableProperties(CultureData cultureData, CalendarId calendarId)
	{
		if (firstDayOfWeek == -1)
		{
			firstDayOfWeek = cultureData.FirstDayOfWeek;
		}
		if (calendarWeekRule == -1)
		{
			calendarWeekRule = cultureData.CalendarWeekRule;
		}
		if (amDesignator == null)
		{
			amDesignator = cultureData.AMDesignator;
		}
		if (pmDesignator == null)
		{
			pmDesignator = cultureData.PMDesignator;
		}
		if (timeSeparator == null)
		{
			timeSeparator = cultureData.TimeSeparator;
		}
		if (dateSeparator == null)
		{
			dateSeparator = cultureData.DateSeparator(calendarId);
		}
		allLongTimePatterns = _cultureData.LongTimes;
		allShortTimePatterns = _cultureData.ShortTimes;
		allLongDatePatterns = cultureData.LongDates(calendarId);
		allShortDatePatterns = cultureData.ShortDates(calendarId);
		allYearMonthPatterns = cultureData.YearMonths(calendarId);
	}

	public static DateTimeFormatInfo GetInstance(IFormatProvider? provider)
	{
		if (provider != null)
		{
			if (!(provider is CultureInfo { _isInherited: false } cultureInfo))
			{
				if (!(provider is DateTimeFormatInfo result))
				{
					if (!(provider.GetFormat(typeof(DateTimeFormatInfo)) is DateTimeFormatInfo result2))
					{
						return CurrentInfo;
					}
					return result2;
				}
				return result;
			}
			return cultureInfo.DateTimeFormat;
		}
		return CurrentInfo;
	}

	public object? GetFormat(Type? formatType)
	{
		if (!(formatType == typeof(DateTimeFormatInfo)))
		{
			return null;
		}
		return this;
	}

	public object Clone()
	{
		DateTimeFormatInfo dateTimeFormatInfo = (DateTimeFormatInfo)MemberwiseClone();
		dateTimeFormatInfo.calendar = (Calendar)Calendar.Clone();
		dateTimeFormatInfo._isReadOnly = false;
		return dateTimeFormatInfo;
	}

	public int GetEra(string eraName)
	{
		if (eraName == null)
		{
			throw new ArgumentNullException("eraName");
		}
		if (eraName.Length == 0)
		{
			return -1;
		}
		for (int i = 0; i < EraNames.Length; i++)
		{
			if (m_eraNames[i].Length > 0 && Culture.CompareInfo.Compare(eraName, m_eraNames[i], CompareOptions.IgnoreCase) == 0)
			{
				return i + 1;
			}
		}
		for (int j = 0; j < AbbreviatedEraNames.Length; j++)
		{
			if (Culture.CompareInfo.Compare(eraName, m_abbrevEraNames[j], CompareOptions.IgnoreCase) == 0)
			{
				return j + 1;
			}
		}
		for (int k = 0; k < AbbreviatedEnglishEraNames.Length; k++)
		{
			if (System.Globalization.CompareInfo.Invariant.Compare(eraName, m_abbrevEnglishEraNames[k], CompareOptions.IgnoreCase) == 0)
			{
				return k + 1;
			}
		}
		return -1;
	}

	public string GetEraName(int era)
	{
		if (era == 0)
		{
			era = Calendar.CurrentEraValue;
		}
		if (--era < EraNames.Length && era >= 0)
		{
			return m_eraNames[era];
		}
		throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
	}

	public string GetAbbreviatedEraName(int era)
	{
		if (AbbreviatedEraNames.Length == 0)
		{
			return GetEraName(era);
		}
		if (era == 0)
		{
			era = Calendar.CurrentEraValue;
		}
		if (--era < m_abbrevEraNames.Length && era >= 0)
		{
			return m_abbrevEraNames[era];
		}
		throw new ArgumentOutOfRangeException("era", era, SR.ArgumentOutOfRange_InvalidEraValue);
	}

	private void OnLongDatePatternChanged()
	{
		ClearTokenHashTable();
		fullDateTimePattern = null;
	}

	private void OnLongTimePatternChanged()
	{
		ClearTokenHashTable();
		fullDateTimePattern = null;
		generalLongTimePattern = null;
		dateTimeOffsetPattern = null;
	}

	private void OnShortDatePatternChanged()
	{
		ClearTokenHashTable();
		generalLongTimePattern = null;
		generalShortTimePattern = null;
		dateTimeOffsetPattern = null;
	}

	private void OnShortTimePatternChanged()
	{
		ClearTokenHashTable();
		generalShortTimePattern = null;
	}

	private void OnYearMonthPatternChanged()
	{
		ClearTokenHashTable();
	}

	private static void CheckNullValue(string[] values, int length)
	{
		for (int i = 0; i < length; i++)
		{
			if (values[i] == null)
			{
				throw new ArgumentNullException("value", SR.ArgumentNull_ArrayValue);
			}
		}
	}

	internal string InternalGetMonthName(int month, MonthNameStyles style, bool abbreviated)
	{
		string[] array = style switch
		{
			MonthNameStyles.Genitive => InternalGetGenitiveMonthNames(abbreviated), 
			MonthNameStyles.LeapYear => InternalGetLeapYearMonthNames(), 
			_ => abbreviated ? InternalGetAbbreviatedMonthNames() : InternalGetMonthNames(), 
		};
		if (month < 1 || month > array.Length)
		{
			throw new ArgumentOutOfRangeException("month", month, SR.Format(SR.ArgumentOutOfRange_Range, 1, array.Length));
		}
		return array[month - 1];
	}

	private string[] InternalGetGenitiveMonthNames(bool abbreviated)
	{
		if (abbreviated)
		{
			if (m_genitiveAbbreviatedMonthNames == null)
			{
				m_genitiveAbbreviatedMonthNames = _cultureData.AbbreviatedGenitiveMonthNames(Calendar.ID);
			}
			return m_genitiveAbbreviatedMonthNames;
		}
		if (genitiveMonthNames == null)
		{
			genitiveMonthNames = _cultureData.GenitiveMonthNames(Calendar.ID);
		}
		return genitiveMonthNames;
	}

	internal string[] InternalGetLeapYearMonthNames()
	{
		if (leapYearMonthNames == null)
		{
			leapYearMonthNames = _cultureData.LeapYearMonthNames(Calendar.ID);
		}
		return leapYearMonthNames;
	}

	public string GetAbbreviatedDayName(DayOfWeek dayofweek)
	{
		if (dayofweek < DayOfWeek.Sunday || dayofweek > DayOfWeek.Saturday)
		{
			throw new ArgumentOutOfRangeException("dayofweek", dayofweek, SR.Format(SR.ArgumentOutOfRange_Range, DayOfWeek.Sunday, DayOfWeek.Saturday));
		}
		return InternalGetAbbreviatedDayOfWeekNames()[(int)dayofweek];
	}

	public string GetShortestDayName(DayOfWeek dayOfWeek)
	{
		if (dayOfWeek < DayOfWeek.Sunday || dayOfWeek > DayOfWeek.Saturday)
		{
			throw new ArgumentOutOfRangeException("dayOfWeek", dayOfWeek, SR.Format(SR.ArgumentOutOfRange_Range, DayOfWeek.Sunday, DayOfWeek.Saturday));
		}
		return InternalGetSuperShortDayNames()[(int)dayOfWeek];
	}

	private static string[] GetCombinedPatterns(string[] patterns1, string[] patterns2, string connectString)
	{
		string[] array = new string[patterns1.Length * patterns2.Length];
		int num = 0;
		for (int i = 0; i < patterns1.Length; i++)
		{
			for (int j = 0; j < patterns2.Length; j++)
			{
				array[num++] = patterns1[i] + connectString + patterns2[j];
			}
		}
		return array;
	}

	public string[] GetAllDateTimePatterns()
	{
		List<string> list = new List<string>(132);
		for (int i = 0; i < DateTimeFormat.allStandardFormats.Length; i++)
		{
			string[] allDateTimePatterns = GetAllDateTimePatterns(DateTimeFormat.allStandardFormats[i]);
			for (int j = 0; j < allDateTimePatterns.Length; j++)
			{
				list.Add(allDateTimePatterns[j]);
			}
		}
		return list.ToArray();
	}

	public string[] GetAllDateTimePatterns(char format)
	{
		switch (format)
		{
		case 'd':
			return AllShortDatePatterns;
		case 'D':
			return AllLongDatePatterns;
		case 'f':
			return GetCombinedPatterns(AllLongDatePatterns, AllShortTimePatterns, " ");
		case 'F':
		case 'U':
			return GetCombinedPatterns(AllLongDatePatterns, AllLongTimePatterns, " ");
		case 'g':
			return GetCombinedPatterns(AllShortDatePatterns, AllShortTimePatterns, " ");
		case 'G':
			return GetCombinedPatterns(AllShortDatePatterns, AllLongTimePatterns, " ");
		case 'M':
		case 'm':
			return new string[1] { MonthDayPattern };
		case 'O':
		case 'o':
			return new string[1] { "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK" };
		case 'R':
		case 'r':
			return new string[1] { "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
		case 's':
			return new string[1] { "yyyy'-'MM'-'dd'T'HH':'mm':'ss" };
		case 't':
			return AllShortTimePatterns;
		case 'T':
			return AllLongTimePatterns;
		case 'u':
			return new string[1] { UniversalSortableDateTimePattern };
		case 'Y':
		case 'y':
			return AllYearMonthPatterns;
		default:
			throw new ArgumentException(SR.Format(SR.Format_BadFormatSpecifier, format), "format");
		}
	}

	public string GetDayName(DayOfWeek dayofweek)
	{
		if (dayofweek < DayOfWeek.Sunday || dayofweek > DayOfWeek.Saturday)
		{
			throw new ArgumentOutOfRangeException("dayofweek", dayofweek, SR.Format(SR.ArgumentOutOfRange_Range, DayOfWeek.Sunday, DayOfWeek.Saturday));
		}
		return InternalGetDayOfWeekNames()[(int)dayofweek];
	}

	public string GetAbbreviatedMonthName(int month)
	{
		if (month < 1 || month > 13)
		{
			throw new ArgumentOutOfRangeException("month", month, SR.Format(SR.ArgumentOutOfRange_Range, 1, 13));
		}
		return InternalGetAbbreviatedMonthNames()[month - 1];
	}

	public string GetMonthName(int month)
	{
		if (month < 1 || month > 13)
		{
			throw new ArgumentOutOfRangeException("month", month, SR.Format(SR.ArgumentOutOfRange_Range, 1, 13));
		}
		return InternalGetMonthNames()[month - 1];
	}

	private static string[] GetMergedPatterns(string[] patterns, string defaultPattern)
	{
		if (defaultPattern == patterns[0])
		{
			return (string[])patterns.Clone();
		}
		int i;
		for (i = 0; i < patterns.Length && !(defaultPattern == patterns[i]); i++)
		{
		}
		string[] array;
		if (i < patterns.Length)
		{
			array = (string[])patterns.Clone();
			array[i] = array[0];
		}
		else
		{
			array = new string[patterns.Length + 1];
			Array.Copy(patterns, 0, array, 1, patterns.Length);
		}
		array[0] = defaultPattern;
		return array;
	}

	public static DateTimeFormatInfo ReadOnly(DateTimeFormatInfo dtfi)
	{
		if (dtfi == null)
		{
			throw new ArgumentNullException("dtfi");
		}
		if (dtfi.IsReadOnly)
		{
			return dtfi;
		}
		DateTimeFormatInfo dateTimeFormatInfo = (DateTimeFormatInfo)dtfi.MemberwiseClone();
		dateTimeFormatInfo.calendar = System.Globalization.Calendar.ReadOnly(dtfi.Calendar);
		dateTimeFormatInfo._isReadOnly = true;
		return dateTimeFormatInfo;
	}

	public void SetAllDateTimePatterns(string[] patterns, char format)
	{
		if (IsReadOnly)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
		}
		if (patterns == null)
		{
			throw new ArgumentNullException("patterns");
		}
		if (patterns.Length == 0)
		{
			throw new ArgumentException(SR.Arg_ArrayZeroError, "patterns");
		}
		for (int i = 0; i < patterns.Length; i++)
		{
			if (patterns[i] == null)
			{
				throw new ArgumentNullException("patterns[" + i + "]", SR.ArgumentNull_ArrayValue);
			}
		}
		switch (format)
		{
		case 'd':
			allShortDatePatterns = patterns;
			shortDatePattern = allShortDatePatterns[0];
			OnShortDatePatternChanged();
			break;
		case 'D':
			allLongDatePatterns = patterns;
			longDatePattern = allLongDatePatterns[0];
			OnLongDatePatternChanged();
			break;
		case 't':
			allShortTimePatterns = patterns;
			shortTimePattern = allShortTimePatterns[0];
			OnShortTimePatternChanged();
			break;
		case 'T':
			allLongTimePatterns = patterns;
			longTimePattern = allLongTimePatterns[0];
			OnLongTimePatternChanged();
			break;
		case 'Y':
		case 'y':
			allYearMonthPatterns = patterns;
			yearMonthPattern = allYearMonthPatterns[0];
			OnYearMonthPatternChanged();
			break;
		default:
			throw new ArgumentException(SR.Format(SR.Format_BadFormatSpecifier, format), "format");
		}
	}

	internal static void ValidateStyles(DateTimeStyles style, bool styles = false)
	{
		if (((uint)style & 0xFFFFFF00u) != 0 || (style & (DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal)) == (DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal) || (style & (DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal | DateTimeStyles.RoundtripKind)) > DateTimeStyles.RoundtripKind)
		{
			ThrowInvalid(style, styles);
		}
		static void ThrowInvalid(DateTimeStyles style, bool styles)
		{
			string message = ((((uint)style & 0xFFFFFF00u) != 0) ? SR.Argument_InvalidDateTimeStyles : (((style & (DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal)) == (DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal)) ? SR.Argument_ConflictingDateTimeStyles : SR.Argument_ConflictingDateTimeRoundtripStyles));
			throw new ArgumentException(message, styles ? "styles" : "style");
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private DateTimeFormatFlags InitializeFormatFlags()
	{
		formatFlags = (DateTimeFormatFlags)(DateTimeFormatInfoScanner.GetFormatFlagGenitiveMonth(MonthNames, InternalGetGenitiveMonthNames(abbreviated: false), AbbreviatedMonthNames, InternalGetGenitiveMonthNames(abbreviated: true)) | DateTimeFormatInfoScanner.GetFormatFlagUseSpaceInMonthNames(MonthNames, InternalGetGenitiveMonthNames(abbreviated: false), AbbreviatedMonthNames, InternalGetGenitiveMonthNames(abbreviated: true)) | DateTimeFormatInfoScanner.GetFormatFlagUseSpaceInDayNames(DayNames, AbbreviatedDayNames) | DateTimeFormatInfoScanner.GetFormatFlagUseHebrewCalendar((int)Calendar.ID));
		return formatFlags;
	}

	internal bool YearMonthAdjustment(ref int year, ref int month, bool parsedMonthName)
	{
		if ((FormatFlags & DateTimeFormatFlags.UseHebrewRule) != 0)
		{
			if (year < 1000)
			{
				year += 5000;
			}
			if (year < Calendar.GetYear(Calendar.MinSupportedDateTime) || year > Calendar.GetYear(Calendar.MaxSupportedDateTime))
			{
				return false;
			}
			if (parsedMonthName && !Calendar.IsLeapYear(year))
			{
				if (month >= 8)
				{
					month--;
				}
				else if (month == 7)
				{
					return false;
				}
			}
		}
		return true;
	}

	internal static DateTimeFormatInfo GetJapaneseCalendarDTFI()
	{
		DateTimeFormatInfo dateTimeFormat = s_jajpDTFI;
		if (dateTimeFormat == null)
		{
			dateTimeFormat = new CultureInfo("ja-JP", useUserOverride: false).DateTimeFormat;
			dateTimeFormat.Calendar = JapaneseCalendar.GetDefaultInstance();
			s_jajpDTFI = dateTimeFormat;
		}
		return dateTimeFormat;
	}

	internal static DateTimeFormatInfo GetTaiwanCalendarDTFI()
	{
		DateTimeFormatInfo dateTimeFormat = s_zhtwDTFI;
		if (dateTimeFormat == null)
		{
			dateTimeFormat = new CultureInfo("zh-TW", useUserOverride: false).DateTimeFormat;
			dateTimeFormat.Calendar = TaiwanCalendar.GetDefaultInstance();
			s_zhtwDTFI = dateTimeFormat;
		}
		return dateTimeFormat;
	}

	private void ClearTokenHashTable()
	{
		_dtfiTokenHash = null;
		formatFlags = DateTimeFormatFlags.NotInitialized;
	}

	internal TokenHashValue[] CreateTokenHashTable()
	{
		TokenHashValue[] array = _dtfiTokenHash;
		if (array == null)
		{
			array = new TokenHashValue[199];
			string text = TimeSeparator.Trim();
			if ("," != text)
			{
				InsertHash(array, ",", TokenType.IgnorableSymbol, 0);
			}
			if ("." != text)
			{
				InsertHash(array, ".", TokenType.IgnorableSymbol, 0);
			}
			if ("시" != text && "時" != text && "时" != text)
			{
				InsertHash(array, TimeSeparator, TokenType.SEP_Time, 0);
			}
			if (_name == "fr-CA")
			{
				InsertHash(array, " h", TokenType.SEP_HourSuff, 0);
				InsertHash(array, " h ", TokenType.SEP_HourSuff, 0);
				InsertHash(array, " min", TokenType.SEP_MinuteSuff, 0);
				InsertHash(array, " min ", TokenType.SEP_MinuteSuff, 0);
				InsertHash(array, " s", TokenType.SEP_SecondSuff, 0);
				InsertHash(array, " s ", TokenType.SEP_SecondSuff, 0);
			}
			InsertHash(array, AMDesignator, (TokenType)1027, 0);
			InsertHash(array, PMDesignator, (TokenType)1284, 1);
			if (LanguageName.Equals("sq"))
			{
				InsertHash(array, "." + AMDesignator, (TokenType)1027, 0);
				InsertHash(array, "." + PMDesignator, (TokenType)1284, 1);
			}
			InsertHash(array, "年", TokenType.SEP_YearSuff, 0);
			InsertHash(array, "년", TokenType.SEP_YearSuff, 0);
			InsertHash(array, "月", TokenType.SEP_MonthSuff, 0);
			InsertHash(array, "월", TokenType.SEP_MonthSuff, 0);
			InsertHash(array, "日", TokenType.SEP_DaySuff, 0);
			InsertHash(array, "일", TokenType.SEP_DaySuff, 0);
			InsertHash(array, "時", TokenType.SEP_HourSuff, 0);
			InsertHash(array, "时", TokenType.SEP_HourSuff, 0);
			InsertHash(array, "分", TokenType.SEP_MinuteSuff, 0);
			InsertHash(array, "秒", TokenType.SEP_SecondSuff, 0);
			if (!LocalAppContextSwitches.EnforceLegacyJapaneseDateParsing && Calendar.ID == CalendarId.JAPAN)
			{
				InsertHash(array, "元", TokenType.YearNumberToken, 1);
				InsertHash(array, "(", TokenType.IgnorableSymbol, 0);
				InsertHash(array, ")", TokenType.IgnorableSymbol, 0);
			}
			if (LanguageName.Equals("ko"))
			{
				InsertHash(array, "시", TokenType.SEP_HourSuff, 0);
				InsertHash(array, "분", TokenType.SEP_MinuteSuff, 0);
				InsertHash(array, "초", TokenType.SEP_SecondSuff, 0);
			}
			if (LanguageName.Equals("ky"))
			{
				InsertHash(array, "-", TokenType.IgnorableSymbol, 0);
			}
			else
			{
				InsertHash(array, "-", TokenType.SEP_DateOrOffset, 0);
			}
			DateTimeFormatInfoScanner dateTimeFormatInfoScanner = new DateTimeFormatInfoScanner();
			string[] dateWordsOfDTFI = dateTimeFormatInfoScanner.GetDateWordsOfDTFI(this);
			_ = FormatFlags;
			bool flag = false;
			if (dateWordsOfDTFI != null)
			{
				for (int i = 0; i < dateWordsOfDTFI.Length; i++)
				{
					switch (dateWordsOfDTFI[i][0])
					{
					case '\ue000':
					{
						ReadOnlySpan<char> monthPostfix = dateWordsOfDTFI[i].AsSpan(1);
						AddMonthNames(array, monthPostfix);
						break;
					}
					case '\ue001':
					{
						string text2 = dateWordsOfDTFI[i].Substring(1);
						InsertHash(array, text2, TokenType.IgnorableSymbol, 0);
						if (DateSeparator.Trim().Equals(text2))
						{
							flag = true;
						}
						break;
					}
					default:
						InsertHash(array, dateWordsOfDTFI[i], TokenType.DateWordToken, 0);
						if (LanguageName.Equals("eu"))
						{
							InsertHash(array, "." + dateWordsOfDTFI[i], TokenType.DateWordToken, 0);
						}
						break;
					}
				}
			}
			if (!flag)
			{
				InsertHash(array, DateSeparator, TokenType.SEP_Date, 0);
			}
			AddMonthNames(array);
			for (int j = 1; j <= 13; j++)
			{
				InsertHash(array, GetAbbreviatedMonthName(j), TokenType.MonthToken, j);
			}
			if ((FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0)
			{
				string[] array2 = InternalGetGenitiveMonthNames(abbreviated: false);
				string[] array3 = InternalGetGenitiveMonthNames(abbreviated: true);
				for (int k = 1; k <= 13; k++)
				{
					InsertHash(array, array2[k - 1], TokenType.MonthToken, k);
					InsertHash(array, array3[k - 1], TokenType.MonthToken, k);
				}
			}
			if ((FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != 0)
			{
				for (int l = 1; l <= 13; l++)
				{
					string str = InternalGetMonthName(l, MonthNameStyles.LeapYear, abbreviated: false);
					InsertHash(array, str, TokenType.MonthToken, l);
				}
			}
			for (int m = 0; m < 7; m++)
			{
				string dayName = GetDayName((DayOfWeek)m);
				InsertHash(array, dayName, TokenType.DayOfWeekToken, m);
				dayName = GetAbbreviatedDayName((DayOfWeek)m);
				InsertHash(array, dayName, TokenType.DayOfWeekToken, m);
			}
			int[] eras = calendar.Eras;
			for (int n = 1; n <= eras.Length; n++)
			{
				InsertHash(array, GetEraName(n), TokenType.EraToken, n);
				InsertHash(array, GetAbbreviatedEraName(n), TokenType.EraToken, n);
			}
			if (!GlobalizationMode.Invariant)
			{
				if (LanguageName.Equals("ja"))
				{
					for (int num = 0; num < 7; num++)
					{
						string str2 = "(" + GetAbbreviatedDayName((DayOfWeek)num) + ")";
						InsertHash(array, str2, TokenType.DayOfWeekToken, num);
					}
					if (Calendar.GetType() != typeof(JapaneseCalendar))
					{
						DateTimeFormatInfo japaneseCalendarDTFI = GetJapaneseCalendarDTFI();
						for (int num2 = 1; num2 <= japaneseCalendarDTFI.Calendar.Eras.Length; num2++)
						{
							InsertHash(array, japaneseCalendarDTFI.GetEraName(num2), TokenType.JapaneseEraToken, num2);
							InsertHash(array, japaneseCalendarDTFI.GetAbbreviatedEraName(num2), TokenType.JapaneseEraToken, num2);
							InsertHash(array, japaneseCalendarDTFI.AbbreviatedEnglishEraNames[num2 - 1], TokenType.JapaneseEraToken, num2);
						}
					}
				}
				else if (CultureName.Equals("zh-TW"))
				{
					DateTimeFormatInfo taiwanCalendarDTFI = GetTaiwanCalendarDTFI();
					for (int num3 = 1; num3 <= taiwanCalendarDTFI.Calendar.Eras.Length; num3++)
					{
						if (taiwanCalendarDTFI.GetEraName(num3).Length > 0)
						{
							InsertHash(array, taiwanCalendarDTFI.GetEraName(num3), TokenType.TEraToken, num3);
						}
					}
				}
			}
			InsertHash(array, InvariantInfo.AMDesignator, (TokenType)1027, 0);
			InsertHash(array, InvariantInfo.PMDesignator, (TokenType)1284, 1);
			for (int num4 = 1; num4 <= 12; num4++)
			{
				string monthName = InvariantInfo.GetMonthName(num4);
				InsertHash(array, monthName, TokenType.MonthToken, num4);
				monthName = InvariantInfo.GetAbbreviatedMonthName(num4);
				InsertHash(array, monthName, TokenType.MonthToken, num4);
			}
			for (int num5 = 0; num5 < 7; num5++)
			{
				string dayName2 = InvariantInfo.GetDayName((DayOfWeek)num5);
				InsertHash(array, dayName2, TokenType.DayOfWeekToken, num5);
				dayName2 = InvariantInfo.GetAbbreviatedDayName((DayOfWeek)num5);
				InsertHash(array, dayName2, TokenType.DayOfWeekToken, num5);
			}
			for (int num6 = 0; num6 < AbbreviatedEnglishEraNames.Length; num6++)
			{
				InsertHash(array, AbbreviatedEnglishEraNames[num6], TokenType.EraToken, num6 + 1);
			}
			InsertHash(array, "T", TokenType.SEP_LocalTimeMark, 0);
			InsertHash(array, "GMT", TokenType.TimeZoneToken, 0);
			InsertHash(array, "Z", TokenType.TimeZoneToken, 0);
			InsertHash(array, "/", TokenType.SEP_Date, 0);
			InsertHash(array, ":", TokenType.SEP_Time, 0);
			_dtfiTokenHash = array;
		}
		return array;
	}

	private void AddMonthNames(TokenHashValue[] temp, ReadOnlySpan<char> monthPostfix = default(ReadOnlySpan<char>))
	{
		for (int i = 1; i <= 13; i++)
		{
			string monthName = GetMonthName(i);
			if (monthName.Length > 0)
			{
				if (!monthPostfix.IsEmpty)
				{
					InsertHash(temp, monthName + monthPostfix, TokenType.MonthToken, i);
				}
				else
				{
					InsertHash(temp, monthName, TokenType.MonthToken, i);
				}
			}
			monthName = GetAbbreviatedMonthName(i);
			InsertHash(temp, monthName, TokenType.MonthToken, i);
		}
	}

	private static bool TryParseHebrewNumber(ref __DTString str, out bool badFormat, out int number)
	{
		number = -1;
		badFormat = false;
		int index = str.Index;
		if (!HebrewNumber.IsDigit(str.Value[index]))
		{
			return false;
		}
		HebrewNumberParsingContext context = new HebrewNumberParsingContext(0);
		HebrewNumberParsingState hebrewNumberParsingState;
		do
		{
			hebrewNumberParsingState = HebrewNumber.ParseByChar(str.Value[index++], ref context);
			if ((uint)hebrewNumberParsingState <= 1u)
			{
				return false;
			}
		}
		while (index < str.Value.Length && hebrewNumberParsingState != HebrewNumberParsingState.FoundEndOfHebrewNumber);
		if (hebrewNumberParsingState != HebrewNumberParsingState.FoundEndOfHebrewNumber)
		{
			return false;
		}
		str.Advance(index - str.Index);
		number = context.result;
		return true;
	}

	private static bool IsHebrewChar(char ch)
	{
		if (ch >= '\u0590')
		{
			return ch <= '\u05ff';
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsAllowedJapaneseTokenFollowedByNonSpaceLetter(string tokenString, char nextCh)
	{
		if (!LocalAppContextSwitches.EnforceLegacyJapaneseDateParsing && Calendar.ID == CalendarId.JAPAN && (nextCh == "元"[0] || (tokenString == "元" && nextCh == "年"[0])))
		{
			return true;
		}
		return false;
	}

	internal bool Tokenize(TokenType TokenMask, out TokenType tokenType, out int tokenValue, ref __DTString str)
	{
		tokenType = TokenType.UnknownToken;
		tokenValue = 0;
		char c = str.m_current;
		bool flag = char.IsLetter(c);
		if (flag)
		{
			c = Culture.TextInfo.ToLower(c);
			if (!GlobalizationMode.Invariant && IsHebrewChar(c) && TokenMask == TokenType.RegularTokenMask && TryParseHebrewNumber(ref str, out var badFormat, out tokenValue))
			{
				if (badFormat)
				{
					tokenType = TokenType.UnknownToken;
					return false;
				}
				tokenType = TokenType.HebrewNumber;
				return true;
			}
		}
		int num = c % 199;
		int num2 = 1 + c % 197;
		int num3 = str.Length - str.Index;
		int num4 = 0;
		TokenHashValue[] array = _dtfiTokenHash ?? CreateTokenHashTable();
		do
		{
			TokenHashValue tokenHashValue = array[num];
			if (tokenHashValue == null)
			{
				break;
			}
			if ((tokenHashValue.tokenType & TokenMask) > (TokenType)0 && tokenHashValue.tokenString.Length <= num3)
			{
				bool flag2 = true;
				if (flag)
				{
					int num5 = str.Index + tokenHashValue.tokenString.Length;
					if (num5 > str.Length)
					{
						flag2 = false;
					}
					else if (num5 < str.Length)
					{
						char c2 = str.Value[num5];
						flag2 = !char.IsLetter(c2) || IsAllowedJapaneseTokenFollowedByNonSpaceLetter(tokenHashValue.tokenString, c2);
					}
				}
				if (flag2 && ((tokenHashValue.tokenString.Length == 1 && str.Value[str.Index] == tokenHashValue.tokenString[0]) || Culture.CompareInfo.Compare(str.Value.Slice(str.Index, tokenHashValue.tokenString.Length), tokenHashValue.tokenString, CompareOptions.IgnoreCase) == 0))
				{
					tokenType = tokenHashValue.tokenType & TokenMask;
					tokenValue = tokenHashValue.tokenValue;
					str.Advance(tokenHashValue.tokenString.Length);
					return true;
				}
				if ((tokenHashValue.tokenType == TokenType.MonthToken && HasSpacesInMonthNames) || (tokenHashValue.tokenType == TokenType.DayOfWeekToken && HasSpacesInDayNames))
				{
					int matchLength = 0;
					if (str.MatchSpecifiedWords(tokenHashValue.tokenString, checkWordBoundary: true, ref matchLength))
					{
						tokenType = tokenHashValue.tokenType & TokenMask;
						tokenValue = tokenHashValue.tokenValue;
						str.Advance(matchLength);
						return true;
					}
				}
			}
			num4++;
			num += num2;
			if (num >= 199)
			{
				num -= 199;
			}
		}
		while (num4 < 199);
		return false;
	}

	private void InsertAtCurrentHashNode(TokenHashValue[] hashTable, string str, char ch, TokenType tokenType, int tokenValue, int pos, int hashcode, int hashProbe)
	{
		TokenHashValue tokenHashValue = hashTable[hashcode];
		hashTable[hashcode] = new TokenHashValue(str, tokenType, tokenValue);
		while (++pos < 199)
		{
			hashcode += hashProbe;
			if (hashcode >= 199)
			{
				hashcode -= 199;
			}
			TokenHashValue tokenHashValue2 = hashTable[hashcode];
			if (tokenHashValue2 == null || Culture.TextInfo.ToLower(tokenHashValue2.tokenString[0]) == ch)
			{
				hashTable[hashcode] = tokenHashValue;
				if (tokenHashValue2 == null)
				{
					break;
				}
				tokenHashValue = tokenHashValue2;
			}
		}
	}

	private void InsertHash(TokenHashValue[] hashTable, string str, TokenType tokenType, int tokenValue)
	{
		if (string.IsNullOrEmpty(str))
		{
			return;
		}
		int num = 0;
		if (char.IsWhiteSpace(str[0]) || char.IsWhiteSpace(str[^1]))
		{
			str = str.Trim();
			if (str.Length == 0)
			{
				return;
			}
		}
		char c = Culture.TextInfo.ToLower(str[0]);
		int num2 = c % 199;
		int num3 = 1 + c % 197;
		do
		{
			TokenHashValue tokenHashValue = hashTable[num2];
			if (tokenHashValue == null)
			{
				hashTable[num2] = new TokenHashValue(str, tokenType, tokenValue);
				break;
			}
			if (str.Length >= tokenHashValue.tokenString.Length && CompareStringIgnoreCaseOptimized(str, 0, tokenHashValue.tokenString.Length, tokenHashValue.tokenString, 0, tokenHashValue.tokenString.Length))
			{
				if (str.Length > tokenHashValue.tokenString.Length)
				{
					InsertAtCurrentHashNode(hashTable, str, c, tokenType, tokenValue, num, num2, num3);
					break;
				}
				int tokenType2 = (int)tokenHashValue.tokenType;
				if (((tokenType2 & 0xFF) == 0 && (tokenType & TokenType.RegularTokenMask) != 0) || ((tokenType2 & 0xFF00) == 0 && (tokenType & TokenType.SeparatorTokenMask) != 0))
				{
					tokenHashValue.tokenType |= tokenType;
					if (tokenValue != 0)
					{
						tokenHashValue.tokenValue = tokenValue;
					}
				}
				break;
			}
			num++;
			num2 += num3;
			if (num2 >= 199)
			{
				num2 -= 199;
			}
		}
		while (num < 199);
	}

	private bool CompareStringIgnoreCaseOptimized(string string1, int offset1, int length1, string string2, int offset2, int length2)
	{
		if (length1 == 1 && length2 == 1 && string1[offset1] == string2[offset2])
		{
			return true;
		}
		return Culture.CompareInfo.Compare(string1, offset1, length1, string2, offset2, length2, CompareOptions.IgnoreCase) == 0;
	}
}
