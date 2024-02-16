using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Globalization;

public class CultureInfo : IFormatProvider, ICloneable
{
	private bool _isReadOnly;

	private CompareInfo _compareInfo;

	private TextInfo _textInfo;

	internal NumberFormatInfo _numInfo;

	internal DateTimeFormatInfo _dateTimeInfo;

	private Calendar _calendar;

	internal CultureData _cultureData;

	internal bool _isInherited;

	private CultureInfo _consoleFallbackCulture;

	internal string _name;

	private string _nonSortName;

	private string _sortName;

	private static volatile CultureInfo s_userDefaultCulture;

	private static volatile CultureInfo s_userDefaultUICulture;

	private static readonly CultureInfo s_InvariantCultureInfo = new CultureInfo(CultureData.Invariant, isReadOnly: true);

	private static volatile CultureInfo s_DefaultThreadCurrentUICulture;

	private static volatile CultureInfo s_DefaultThreadCurrentCulture;

	[ThreadStatic]
	private static CultureInfo s_currentThreadCulture;

	[ThreadStatic]
	private static CultureInfo s_currentThreadUICulture;

	private static AsyncLocal<CultureInfo> s_asyncLocalCurrentCulture;

	private static AsyncLocal<CultureInfo> s_asyncLocalCurrentUICulture;

	private static volatile Dictionary<string, CultureInfo> s_cachedCulturesByName;

	private static volatile Dictionary<int, CultureInfo> s_cachedCulturesByLcid;

	private CultureInfo _parent;

	internal const int LOCALE_NEUTRAL = 0;

	private const int LOCALE_USER_DEFAULT = 1024;

	private const int LOCALE_SYSTEM_DEFAULT = 2048;

	internal const int LOCALE_CUSTOM_UNSPECIFIED = 4096;

	internal const int LOCALE_CUSTOM_DEFAULT = 3072;

	internal const int LOCALE_INVARIANT = 127;

	public static CultureInfo CurrentCulture
	{
		get
		{
			return s_currentThreadCulture ?? s_DefaultThreadCurrentCulture ?? s_userDefaultCulture ?? InitializeUserDefaultCulture();
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (s_asyncLocalCurrentCulture == null)
			{
				Interlocked.CompareExchange(ref s_asyncLocalCurrentCulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentCulture), null);
			}
			s_asyncLocalCurrentCulture.Value = value;
		}
	}

	public static CultureInfo CurrentUICulture
	{
		get
		{
			return s_currentThreadUICulture ?? s_DefaultThreadCurrentUICulture ?? UserDefaultUICulture;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyCultureName(value, throwException: true);
			if (s_asyncLocalCurrentUICulture == null)
			{
				Interlocked.CompareExchange(ref s_asyncLocalCurrentUICulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentUICulture), null);
			}
			s_asyncLocalCurrentUICulture.Value = value;
		}
	}

	internal static CultureInfo UserDefaultUICulture => s_userDefaultUICulture ?? InitializeUserDefaultUICulture();

	public static CultureInfo InstalledUICulture => s_userDefaultCulture ?? InitializeUserDefaultCulture();

	public static CultureInfo? DefaultThreadCurrentCulture
	{
		get
		{
			return s_DefaultThreadCurrentCulture;
		}
		set
		{
			s_DefaultThreadCurrentCulture = value;
		}
	}

	public static CultureInfo? DefaultThreadCurrentUICulture
	{
		get
		{
			return s_DefaultThreadCurrentUICulture;
		}
		set
		{
			if (value != null)
			{
				VerifyCultureName(value, throwException: true);
			}
			s_DefaultThreadCurrentUICulture = value;
		}
	}

	public static CultureInfo InvariantCulture => s_InvariantCultureInfo;

	public virtual CultureInfo Parent
	{
		get
		{
			if (_parent == null)
			{
				string text = _cultureData.ParentName;
				if (text == "zh")
				{
					if (_name.Length == 5 && _name[2] == '-')
					{
						if ((_name[3] == 'C' && _name[4] == 'N') || (_name[3] == 'S' && _name[4] == 'G'))
						{
							text = "zh-Hans";
						}
						else if ((_name[3] == 'H' && _name[4] == 'K') || (_name[3] == 'M' && _name[4] == 'O') || (_name[3] == 'T' && _name[4] == 'W'))
						{
							text = "zh-Hant";
						}
					}
					else if (_name.Length > 8 && MemoryExtensions.Equals(_name.AsSpan(2, 4), "-Han", StringComparison.Ordinal) && _name[7] == '-')
					{
						if (_name[6] == 't')
						{
							text = "zh-Hant";
						}
						else if (_name[6] == 's')
						{
							text = "zh-Hans";
						}
					}
				}
				Interlocked.CompareExchange(value: (!string.IsNullOrEmpty(text)) ? (CreateCultureInfoNoThrow(text, _cultureData.UseUserOverride) ?? InvariantCulture) : InvariantCulture, location1: ref _parent, comparand: null);
			}
			return _parent;
		}
	}

	public virtual int LCID => _cultureData.LCID;

	public virtual int KeyboardLayoutId => _cultureData.KeyboardLayoutId;

	public virtual string Name
	{
		get
		{
			string text = _nonSortName;
			if (text == null)
			{
				string obj = _cultureData.Name ?? string.Empty;
				string text2 = obj;
				_nonSortName = obj;
				text = text2;
			}
			return text;
		}
	}

	internal string SortName => _sortName ?? (_sortName = _cultureData.SortName);

	public string IetfLanguageTag
	{
		get
		{
			string name = Name;
			if (!(name == "zh-CHT"))
			{
				if (name == "zh-CHS")
				{
					return "zh-Hans";
				}
				return Name;
			}
			return "zh-Hant";
		}
	}

	public virtual string DisplayName => _cultureData.DisplayName;

	public virtual string NativeName => _cultureData.NativeName;

	public virtual string EnglishName => _cultureData.EnglishName;

	public virtual string TwoLetterISOLanguageName => _cultureData.TwoLetterISOLanguageName;

	public virtual string ThreeLetterISOLanguageName => _cultureData.ThreeLetterISOLanguageName;

	public virtual string ThreeLetterWindowsLanguageName => _cultureData.ThreeLetterWindowsLanguageName;

	public virtual CompareInfo CompareInfo => _compareInfo ?? (_compareInfo = (UseUserOverride ? GetCultureInfo(_name).CompareInfo : new CompareInfo(this)));

	public virtual TextInfo TextInfo
	{
		get
		{
			if (_textInfo == null)
			{
				TextInfo textInfo = new TextInfo(_cultureData);
				textInfo.SetReadOnlyState(_isReadOnly);
				_textInfo = textInfo;
			}
			return _textInfo;
		}
	}

	public virtual bool IsNeutralCulture => _cultureData.IsNeutralCulture;

	public CultureTypes CultureTypes
	{
		get
		{
			CultureTypes cultureTypes = (_cultureData.IsNeutralCulture ? CultureTypes.NeutralCultures : CultureTypes.SpecificCultures);
			_ = CultureData.IsWin32Installed;
			cultureTypes |= CultureTypes.InstalledWin32Cultures;
			if (_cultureData.IsSupplementalCustomCulture)
			{
				cultureTypes |= CultureTypes.UserCustomCulture;
			}
			if (_cultureData.IsReplacementCulture)
			{
				cultureTypes |= CultureTypes.ReplacementCultures;
			}
			return cultureTypes;
		}
	}

	public virtual NumberFormatInfo NumberFormat
	{
		get
		{
			if (_numInfo == null)
			{
				NumberFormatInfo numberFormatInfo = new NumberFormatInfo(_cultureData);
				numberFormatInfo._isReadOnly = _isReadOnly;
				Interlocked.CompareExchange(ref _numInfo, numberFormatInfo, null);
			}
			return _numInfo;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_numInfo = value;
		}
	}

	public virtual DateTimeFormatInfo DateTimeFormat
	{
		get
		{
			if (_dateTimeInfo == null)
			{
				DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo(_cultureData, Calendar);
				dateTimeFormatInfo._isReadOnly = _isReadOnly;
				Interlocked.CompareExchange(ref _dateTimeInfo, dateTimeFormatInfo, null);
			}
			return _dateTimeInfo;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_dateTimeInfo = value;
		}
	}

	public virtual Calendar Calendar
	{
		get
		{
			if (_calendar == null)
			{
				Calendar defaultCalendar = _cultureData.DefaultCalendar;
				Interlocked.MemoryBarrier();
				defaultCalendar.SetReadOnlyState(_isReadOnly);
				_calendar = defaultCalendar;
			}
			return _calendar;
		}
	}

	public virtual Calendar[] OptionalCalendars
	{
		get
		{
			if (GlobalizationMode.Invariant)
			{
				return new GregorianCalendar[1]
				{
					new GregorianCalendar()
				};
			}
			CalendarId[] calendarIds = _cultureData.CalendarIds;
			Calendar[] array = new Calendar[calendarIds.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = GetCalendarInstance(calendarIds[i]);
			}
			return array;
		}
	}

	public bool UseUserOverride => _cultureData.UseUserOverride;

	public bool IsReadOnly => _isReadOnly;

	internal bool HasInvariantCultureName => Name == InvariantCulture.Name;

	private static Dictionary<string, CultureInfo> CachedCulturesByName
	{
		get
		{
			Dictionary<string, CultureInfo> dictionary = s_cachedCulturesByName;
			if (dictionary == null)
			{
				dictionary = new Dictionary<string, CultureInfo>();
				dictionary = Interlocked.CompareExchange(ref s_cachedCulturesByName, dictionary, null) ?? dictionary;
			}
			return dictionary;
		}
	}

	private static Dictionary<int, CultureInfo> CachedCulturesByLcid
	{
		get
		{
			Dictionary<int, CultureInfo> dictionary = s_cachedCulturesByLcid;
			if (dictionary == null)
			{
				dictionary = new Dictionary<int, CultureInfo>();
				dictionary = Interlocked.CompareExchange(ref s_cachedCulturesByLcid, dictionary, null) ?? dictionary;
			}
			return dictionary;
		}
	}

	internal static string? UserDefaultLocaleName { get; set; } = GetUserDefaultLocaleName();


	private static void AsyncLocalSetCurrentCulture(AsyncLocalValueChangedArgs<CultureInfo> args)
	{
		s_currentThreadCulture = args.CurrentValue;
	}

	private static void AsyncLocalSetCurrentUICulture(AsyncLocalValueChangedArgs<CultureInfo> args)
	{
		s_currentThreadUICulture = args.CurrentValue;
	}

	private static CultureInfo InitializeUserDefaultCulture()
	{
		Interlocked.CompareExchange(ref s_userDefaultCulture, GetUserDefaultCulture(), null);
		return s_userDefaultCulture;
	}

	private static CultureInfo InitializeUserDefaultUICulture()
	{
		Interlocked.CompareExchange(ref s_userDefaultUICulture, GetUserDefaultUICulture(), null);
		return s_userDefaultUICulture;
	}

	private static string GetCultureNotSupportedExceptionMessage()
	{
		if (!GlobalizationMode.Invariant)
		{
			return SR.Argument_CultureNotSupported;
		}
		return SR.Argument_CultureNotSupportedInInvariantMode;
	}

	public CultureInfo(string name)
		: this(name, useUserOverride: true)
	{
	}

	public CultureInfo(string name, bool useUserOverride)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		CultureData cultureData = CultureData.GetCultureData(name, useUserOverride);
		if (cultureData == null)
		{
			throw new CultureNotFoundException("name", name, GetCultureNotSupportedExceptionMessage());
		}
		_cultureData = cultureData;
		_name = _cultureData.CultureName;
		_isInherited = GetType() != typeof(CultureInfo);
	}

	private CultureInfo(CultureData cultureData, bool isReadOnly = false)
	{
		_cultureData = cultureData;
		_name = cultureData.CultureName;
		_isReadOnly = isReadOnly;
	}

	private static CultureInfo CreateCultureInfoNoThrow(string name, bool useUserOverride)
	{
		CultureData cultureData = CultureData.GetCultureData(name, useUserOverride);
		if (cultureData == null)
		{
			return null;
		}
		return new CultureInfo(cultureData);
	}

	public CultureInfo(int culture)
		: this(culture, useUserOverride: true)
	{
	}

	public CultureInfo(int culture, bool useUserOverride)
	{
		if (culture < 0)
		{
			throw new ArgumentOutOfRangeException("culture", SR.ArgumentOutOfRange_NeedPosNum);
		}
		switch (culture)
		{
		case 0:
		case 1024:
		case 2048:
		case 3072:
		case 4096:
			throw new CultureNotFoundException("culture", culture, SR.Argument_CultureNotSupported);
		}
		_cultureData = CultureData.GetCultureData(culture, useUserOverride);
		_isInherited = GetType() != typeof(CultureInfo);
		_name = _cultureData.CultureName;
	}

	internal CultureInfo(string cultureName, string textAndCompareCultureName)
	{
		if (cultureName == null)
		{
			throw new ArgumentNullException("cultureName", SR.ArgumentNull_String);
		}
		CultureData cultureData = CultureData.GetCultureData(cultureName, useUserOverride: false) ?? throw new CultureNotFoundException("cultureName", cultureName, GetCultureNotSupportedExceptionMessage());
		_cultureData = cultureData;
		_name = _cultureData.CultureName;
		CultureInfo cultureInfo = GetCultureInfo(textAndCompareCultureName);
		_compareInfo = cultureInfo.CompareInfo;
		_textInfo = cultureInfo.TextInfo;
	}

	private static CultureInfo GetCultureByName(string name)
	{
		try
		{
			return new CultureInfo(name)
			{
				_isReadOnly = true
			};
		}
		catch (ArgumentException)
		{
			return InvariantCulture;
		}
	}

	public static CultureInfo CreateSpecificCulture(string name)
	{
		CultureInfo cultureInfo;
		try
		{
			cultureInfo = new CultureInfo(name);
		}
		catch (ArgumentException)
		{
			cultureInfo = null;
			for (int i = 0; i < name.Length; i++)
			{
				if ('-' == name[i])
				{
					try
					{
						cultureInfo = new CultureInfo(name.Substring(0, i));
					}
					catch (ArgumentException)
					{
						throw;
					}
					break;
				}
			}
			if (cultureInfo == null)
			{
				throw;
			}
		}
		if (!cultureInfo.IsNeutralCulture)
		{
			return cultureInfo;
		}
		return new CultureInfo(cultureInfo._cultureData.SpecificCultureName);
	}

	internal static bool VerifyCultureName(string cultureName, bool throwException)
	{
		foreach (char c in cultureName)
		{
			if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
			{
				if (throwException)
				{
					throw new ArgumentException(SR.Format(SR.Argument_InvalidResourceCultureName, cultureName));
				}
				return false;
			}
		}
		return true;
	}

	internal static bool VerifyCultureName(CultureInfo culture, bool throwException)
	{
		if (!culture._isInherited)
		{
			return true;
		}
		return VerifyCultureName(culture.Name, throwException);
	}

	public static CultureInfo[] GetCultures(CultureTypes types)
	{
		if ((types & CultureTypes.UserCustomCulture) == CultureTypes.UserCustomCulture)
		{
			types |= CultureTypes.ReplacementCultures;
		}
		return CultureData.GetCultures(types);
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (this == value)
		{
			return true;
		}
		if (value is CultureInfo cultureInfo)
		{
			if (Name.Equals(cultureInfo.Name))
			{
				return CompareInfo.Equals(cultureInfo.CompareInfo);
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Name.GetHashCode() + CompareInfo.GetHashCode();
	}

	public override string ToString()
	{
		return _name;
	}

	public virtual object? GetFormat(Type? formatType)
	{
		if (formatType == typeof(NumberFormatInfo))
		{
			return NumberFormat;
		}
		if (formatType == typeof(DateTimeFormatInfo))
		{
			return DateTimeFormat;
		}
		return null;
	}

	public void ClearCachedData()
	{
		UserDefaultLocaleName = GetUserDefaultLocaleName();
		s_userDefaultCulture = GetUserDefaultCulture();
		s_userDefaultUICulture = GetUserDefaultUICulture();
		RegionInfo.s_currentRegionInfo = null;
		TimeZone.ResetTimeZone();
		TimeZoneInfo.ClearCachedData();
		s_cachedCulturesByLcid = null;
		s_cachedCulturesByName = null;
		CultureData.ClearCachedData();
	}

	internal static Calendar GetCalendarInstance(CalendarId calType)
	{
		if (calType == CalendarId.GREGORIAN)
		{
			return new GregorianCalendar();
		}
		return GetCalendarInstanceRare(calType);
	}

	internal static Calendar GetCalendarInstanceRare(CalendarId calType)
	{
		switch (calType)
		{
		case CalendarId.GREGORIAN_US:
		case CalendarId.GREGORIAN_ME_FRENCH:
		case CalendarId.GREGORIAN_ARABIC:
		case CalendarId.GREGORIAN_XLIT_ENGLISH:
		case CalendarId.GREGORIAN_XLIT_FRENCH:
			return new GregorianCalendar((GregorianCalendarTypes)calType);
		case CalendarId.TAIWAN:
			return new TaiwanCalendar();
		case CalendarId.JAPAN:
			return new JapaneseCalendar();
		case CalendarId.KOREA:
			return new KoreanCalendar();
		case CalendarId.THAI:
			return new ThaiBuddhistCalendar();
		case CalendarId.HIJRI:
			return new HijriCalendar();
		case CalendarId.HEBREW:
			return new HebrewCalendar();
		case CalendarId.UMALQURA:
			return new UmAlQuraCalendar();
		case CalendarId.PERSIAN:
			return new PersianCalendar();
		default:
			return new GregorianCalendar();
		}
	}

	public CultureInfo GetConsoleFallbackUICulture()
	{
		CultureInfo cultureInfo = _consoleFallbackCulture;
		if (cultureInfo == null)
		{
			cultureInfo = CreateSpecificCulture(_cultureData.SCONSOLEFALLBACKNAME);
			cultureInfo._isReadOnly = true;
			_consoleFallbackCulture = cultureInfo;
		}
		return cultureInfo;
	}

	public virtual object Clone()
	{
		CultureInfo cultureInfo = (CultureInfo)MemberwiseClone();
		cultureInfo._isReadOnly = false;
		if (!_isInherited)
		{
			if (_dateTimeInfo != null)
			{
				cultureInfo._dateTimeInfo = (DateTimeFormatInfo)_dateTimeInfo.Clone();
			}
			if (_numInfo != null)
			{
				cultureInfo._numInfo = (NumberFormatInfo)_numInfo.Clone();
			}
		}
		else
		{
			cultureInfo.DateTimeFormat = (DateTimeFormatInfo)DateTimeFormat.Clone();
			cultureInfo.NumberFormat = (NumberFormatInfo)NumberFormat.Clone();
		}
		if (_textInfo != null)
		{
			cultureInfo._textInfo = (TextInfo)_textInfo.Clone();
		}
		if (_dateTimeInfo != null && _dateTimeInfo.Calendar == _calendar)
		{
			cultureInfo._calendar = cultureInfo.DateTimeFormat.Calendar;
		}
		else if (_calendar != null)
		{
			cultureInfo._calendar = (Calendar)_calendar.Clone();
		}
		return cultureInfo;
	}

	public static CultureInfo ReadOnly(CultureInfo ci)
	{
		if (ci == null)
		{
			throw new ArgumentNullException("ci");
		}
		if (ci.IsReadOnly)
		{
			return ci;
		}
		CultureInfo cultureInfo = (CultureInfo)ci.MemberwiseClone();
		if (!ci.IsNeutralCulture)
		{
			if (!ci._isInherited)
			{
				if (ci._dateTimeInfo != null)
				{
					cultureInfo._dateTimeInfo = DateTimeFormatInfo.ReadOnly(ci._dateTimeInfo);
				}
				if (ci._numInfo != null)
				{
					cultureInfo._numInfo = NumberFormatInfo.ReadOnly(ci._numInfo);
				}
			}
			else
			{
				cultureInfo.DateTimeFormat = DateTimeFormatInfo.ReadOnly(ci.DateTimeFormat);
				cultureInfo.NumberFormat = NumberFormatInfo.ReadOnly(ci.NumberFormat);
			}
		}
		if (ci._textInfo != null)
		{
			cultureInfo._textInfo = System.Globalization.TextInfo.ReadOnly(ci._textInfo);
		}
		if (ci._calendar != null)
		{
			cultureInfo._calendar = System.Globalization.Calendar.ReadOnly(ci._calendar);
		}
		cultureInfo._isReadOnly = true;
		return cultureInfo;
	}

	private void VerifyWritable()
	{
		if (_isReadOnly)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
		}
	}

	public static CultureInfo GetCultureInfo(int culture)
	{
		if (culture <= 0)
		{
			throw new ArgumentOutOfRangeException("culture", SR.ArgumentOutOfRange_NeedPosNum);
		}
		Dictionary<int, CultureInfo> cachedCulturesByLcid = CachedCulturesByLcid;
		CultureInfo value;
		lock (cachedCulturesByLcid)
		{
			if (cachedCulturesByLcid.TryGetValue(culture, out value))
			{
				return value;
			}
		}
		try
		{
			value = new CultureInfo(culture, useUserOverride: false)
			{
				_isReadOnly = true
			};
		}
		catch (ArgumentException)
		{
			throw new CultureNotFoundException("culture", culture, GetCultureNotSupportedExceptionMessage());
		}
		lock (cachedCulturesByLcid)
		{
			cachedCulturesByLcid[culture] = value;
			return value;
		}
	}

	public static CultureInfo GetCultureInfo(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		name = CultureData.AnsiToLower(name);
		Dictionary<string, CultureInfo> cachedCulturesByName = CachedCulturesByName;
		CultureInfo value;
		lock (cachedCulturesByName)
		{
			if (cachedCulturesByName.TryGetValue(name, out value))
			{
				return value;
			}
		}
		value = CreateCultureInfoNoThrow(name, useUserOverride: false) ?? throw new CultureNotFoundException("name", name, GetCultureNotSupportedExceptionMessage());
		value._isReadOnly = true;
		name = CultureData.AnsiToLower(value._name);
		lock (cachedCulturesByName)
		{
			cachedCulturesByName[name] = value;
			return value;
		}
	}

	public static CultureInfo GetCultureInfo(string name, string altName)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (altName == null)
		{
			throw new ArgumentNullException("altName");
		}
		name = CultureData.AnsiToLower(name);
		altName = CultureData.AnsiToLower(altName);
		string key = name + "\ufffd" + altName;
		Dictionary<string, CultureInfo> cachedCulturesByName = CachedCulturesByName;
		CultureInfo value;
		lock (cachedCulturesByName)
		{
			if (cachedCulturesByName.TryGetValue(key, out value))
			{
				return value;
			}
		}
		try
		{
			value = new CultureInfo(name, altName)
			{
				_isReadOnly = true
			};
			value.TextInfo.SetReadOnlyState(readOnly: true);
		}
		catch (ArgumentException)
		{
			throw new CultureNotFoundException("name/altName", SR.Format(SR.Argument_OneOfCulturesNotSupported, name, altName));
		}
		lock (cachedCulturesByName)
		{
			cachedCulturesByName[key] = value;
			return value;
		}
	}

	public static CultureInfo GetCultureInfo(string name, bool predefinedOnly)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (predefinedOnly && !GlobalizationMode.Invariant && (GlobalizationMode.UseNls ? (!CultureData.NlsIsEnsurePredefinedLocaleName(name)) : (!CultureData.IcuIsEnsurePredefinedLocaleName(name))))
		{
			throw new CultureNotFoundException("name", name, SR.Format(SR.Argument_InvalidPredefinedCultureName, name));
		}
		return GetCultureInfo(name);
	}

	public static CultureInfo GetCultureInfoByIetfLanguageTag(string name)
	{
		if (name == "zh-CHT" || name == "zh-CHS")
		{
			throw new CultureNotFoundException("name", SR.Format(SR.Argument_CultureIetfNotSupported, name));
		}
		CultureInfo cultureInfo = GetCultureInfo(name);
		if (cultureInfo.LCID > 65535 || cultureInfo.LCID == 1034)
		{
			throw new CultureNotFoundException("name", SR.Format(SR.Argument_CultureIetfNotSupported, name));
		}
		return cultureInfo;
	}

	internal static CultureInfo GetUserDefaultCulture()
	{
		if (GlobalizationMode.Invariant)
		{
			return InvariantCulture;
		}
		string userDefaultLocaleName = UserDefaultLocaleName;
		if (userDefaultLocaleName == null)
		{
			return InvariantCulture;
		}
		return GetCultureByName(userDefaultLocaleName);
	}

	private unsafe static CultureInfo GetUserDefaultUICulture()
	{
		if (GlobalizationMode.Invariant)
		{
			return InvariantCulture;
		}
		uint num = 0u;
		uint num2 = 0u;
		if (Interop.Kernel32.GetUserPreferredUILanguages(8u, &num, null, &num2) != 0)
		{
			Span<char> span = ((num2 > 256) ? ((Span<char>)new char[num2]) : stackalloc char[(int)num2]);
			Span<char> span2 = span;
			fixed (char* pwszLanguagesBuffer = span2)
			{
				if (Interop.Kernel32.GetUserPreferredUILanguages(8u, &num, pwszLanguagesBuffer, &num2) != 0)
				{
					return GetCultureByName(span2.ToString());
				}
			}
		}
		return InitializeUserDefaultCulture();
	}

	private static string GetUserDefaultLocaleName()
	{
		string text;
		if (!GlobalizationMode.Invariant)
		{
			text = CultureData.GetLocaleInfoEx(null, 92u);
			if (text == null)
			{
				return CultureData.GetLocaleInfoEx("!x-sys-default-locale", 92u);
			}
		}
		else
		{
			text = InvariantCulture.Name;
		}
		return text;
	}
}
