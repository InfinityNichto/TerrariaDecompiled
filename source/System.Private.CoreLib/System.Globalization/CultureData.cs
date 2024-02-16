using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Globalization;

internal sealed class CultureData
{
	private enum LocaleStringData : uint
	{
		LocalizedDisplayName = 2u,
		EnglishDisplayName = 114u,
		NativeDisplayName = 115u,
		LocalizedLanguageName = 111u,
		EnglishLanguageName = 4097u,
		NativeLanguageName = 4u,
		LocalizedCountryName = 6u,
		EnglishCountryName = 4098u,
		NativeCountryName = 8u,
		AbbreviatedWindowsLanguageName = 3u,
		ListSeparator = 12u,
		DecimalSeparator = 14u,
		ThousandSeparator = 15u,
		Digits = 19u,
		MonetarySymbol = 20u,
		CurrencyEnglishName = 4103u,
		CurrencyNativeName = 4104u,
		Iso4217MonetarySymbol = 21u,
		MonetaryDecimalSeparator = 22u,
		MonetaryThousandSeparator = 23u,
		AMDesignator = 40u,
		PMDesignator = 41u,
		PositiveSign = 80u,
		NegativeSign = 81u,
		Iso639LanguageTwoLetterName = 89u,
		Iso639LanguageThreeLetterName = 103u,
		Iso639LanguageName = 89u,
		Iso3166CountryName = 90u,
		Iso3166CountryName2 = 104u,
		NaNSymbol = 105u,
		PositiveInfinitySymbol = 106u,
		NegativeInfinitySymbol = 107u,
		ParentName = 109u,
		ConsoleFallbackName = 110u,
		PercentSymbol = 118u,
		PerMilleSymbol = 119u
	}

	private enum LocaleGroupingData : uint
	{
		Digit = 16u,
		Monetary = 24u
	}

	private enum LocaleNumberData : uint
	{
		LanguageId = 1u,
		GeoId = 91u,
		DigitSubstitution = 4116u,
		MeasurementSystem = 13u,
		FractionalDigitsCount = 17u,
		NegativeNumberFormat = 4112u,
		MonetaryFractionalDigitsCount = 25u,
		PositiveMonetaryNumberFormat = 27u,
		NegativeMonetaryNumberFormat = 28u,
		CalendarType = 4105u,
		FirstDayOfWeek = 4108u,
		FirstWeekOfYear = 4109u,
		ReadingLayout = 112u,
		NegativePercentFormat = 116u,
		PositivePercentFormat = 117u,
		OemCodePage = 11u,
		AnsiCodePage = 4100u,
		MacCodePage = 4113u,
		EbcdicCodePage = 4114u
	}

	private struct EnumLocaleData
	{
		public string regionName;

		public string cultureName;
	}

	private struct EnumData
	{
		public List<string> strings;
	}

	private string _sRealName;

	private string _sWindowsName;

	private string _sName;

	private string _sParent;

	private string _sEnglishDisplayName;

	private string _sNativeDisplayName;

	private string _sSpecificCulture;

	private string _sISO639Language;

	private string _sISO639Language2;

	private string _sEnglishLanguage;

	private string _sNativeLanguage;

	private string _sAbbrevLang;

	private string _sConsoleFallbackName;

	private int _iInputLanguageHandle = -1;

	private string _sRegionName;

	private string _sLocalizedCountry;

	private string _sEnglishCountry;

	private string _sNativeCountry;

	private string _sISO3166CountryName;

	private string _sISO3166CountryName2;

	private int _iGeoId = -1;

	private string _sPositiveSign;

	private string _sNegativeSign;

	private int _iDigits;

	private int _iNegativeNumber;

	private int[] _waGrouping;

	private string _sDecimalSeparator;

	private string _sThousandSeparator;

	private string _sNaN;

	private string _sPositiveInfinity;

	private string _sNegativeInfinity;

	private int _iNegativePercent = -1;

	private int _iPositivePercent = -1;

	private string _sPercent;

	private string _sPerMille;

	private string _sCurrency;

	private string _sIntlMonetarySymbol;

	private string _sEnglishCurrency;

	private string _sNativeCurrency;

	private int _iCurrencyDigits;

	private int _iCurrency;

	private int _iNegativeCurrency;

	private int[] _waMonetaryGrouping;

	private string _sMonetaryDecimal;

	private string _sMonetaryThousand;

	private int _iMeasure = -1;

	private string _sListSeparator;

	private string _sAM1159;

	private string _sPM2359;

	private string _sTimeSeparator;

	private volatile string[] _saLongTimes;

	private volatile string[] _saShortTimes;

	private volatile string[] _saDurationFormats;

	private int _iFirstDayOfWeek = -1;

	private int _iFirstWeekOfYear = -1;

	private volatile CalendarId[] _waCalendars;

	private CalendarData[] _calendars;

	private int _iReadingLayout = -1;

	private int _iDefaultAnsiCodePage = -1;

	private int _iDefaultOemCodePage = -1;

	private int _iDefaultMacCodePage = -1;

	private int _iDefaultEbcdicCodePage = -1;

	private int _iLanguage;

	private bool _bUseOverrides;

	private bool _bUseOverridesUserSetting;

	private bool _bNeutral;

	private static volatile Dictionary<string, CultureData> s_cachedRegions;

	private static volatile Dictionary<string, string> s_regionNames;

	private static volatile CultureData s_Invariant;

	private static volatile Dictionary<string, CultureData> s_cachedCultures;

	private static readonly object s_lock = new object();

	private static Dictionary<string, string> RegionNames
	{
		get
		{
			object obj = s_regionNames;
			if (obj == null)
			{
				obj = new Dictionary<string, string>(257, StringComparer.OrdinalIgnoreCase)
				{
					{ "001", "en-001" },
					{ "029", "en-029" },
					{ "150", "en-150" },
					{ "419", "es-419" },
					{ "AD", "ca-AD" },
					{ "AE", "ar-AE" },
					{ "AF", "prs-AF" },
					{ "AG", "en-AG" },
					{ "AI", "en-AI" },
					{ "AL", "sq-AL" },
					{ "AM", "hy-AM" },
					{ "AO", "pt-AO" },
					{ "AQ", "en-A" },
					{ "AR", "es-AR" },
					{ "AS", "en-AS" },
					{ "AT", "de-AT" },
					{ "AU", "en-AU" },
					{ "AW", "nl-AW" },
					{ "AX", "sv-AX" },
					{ "AZ", "az-Cyrl-AZ" },
					{ "BA", "bs-Latn-BA" },
					{ "BB", "en-BB" },
					{ "BD", "bn-BD" },
					{ "BE", "nl-BE" },
					{ "BF", "fr-BF" },
					{ "BG", "bg-BG" },
					{ "BH", "ar-BH" },
					{ "BI", "rn-BI" },
					{ "BJ", "fr-BJ" },
					{ "BL", "fr-BL" },
					{ "BM", "en-BM" },
					{ "BN", "ms-BN" },
					{ "BO", "es-BO" },
					{ "BQ", "nl-BQ" },
					{ "BR", "pt-BR" },
					{ "BS", "en-BS" },
					{ "BT", "dz-BT" },
					{ "BV", "nb-B" },
					{ "BW", "en-BW" },
					{ "BY", "be-BY" },
					{ "BZ", "en-BZ" },
					{ "CA", "en-CA" },
					{ "CC", "en-CC" },
					{ "CD", "fr-CD" },
					{ "CF", "sg-CF" },
					{ "CG", "fr-CG" },
					{ "CH", "it-CH" },
					{ "CI", "fr-CI" },
					{ "CK", "en-CK" },
					{ "CL", "es-CL" },
					{ "CM", "fr-C" },
					{ "CN", "zh-CN" },
					{ "CO", "es-CO" },
					{ "CR", "es-CR" },
					{ "CS", "sr-Cyrl-CS" },
					{ "CU", "es-CU" },
					{ "CV", "pt-CV" },
					{ "CW", "nl-CW" },
					{ "CX", "en-CX" },
					{ "CY", "el-CY" },
					{ "CZ", "cs-CZ" },
					{ "DE", "de-DE" },
					{ "DJ", "fr-DJ" },
					{ "DK", "da-DK" },
					{ "DM", "en-DM" },
					{ "DO", "es-DO" },
					{ "DZ", "ar-DZ" },
					{ "EC", "es-EC" },
					{ "EE", "et-EE" },
					{ "EG", "ar-EG" },
					{ "ER", "tig-ER" },
					{ "ES", "es-ES" },
					{ "ET", "am-ET" },
					{ "FI", "fi-FI" },
					{ "FJ", "en-FJ" },
					{ "FK", "en-FK" },
					{ "FM", "en-FM" },
					{ "FO", "fo-FO" },
					{ "FR", "fr-FR" },
					{ "GA", "fr-GA" },
					{ "GB", "en-GB" },
					{ "GD", "en-GD" },
					{ "GE", "ka-GE" },
					{ "GF", "fr-GF" },
					{ "GG", "en-GG" },
					{ "GH", "en-GH" },
					{ "GI", "en-GI" },
					{ "GL", "kl-GL" },
					{ "GM", "en-GM" },
					{ "GN", "fr-GN" },
					{ "GP", "fr-GP" },
					{ "GQ", "es-GQ" },
					{ "GR", "el-GR" },
					{ "GS", "en-G" },
					{ "GT", "es-GT" },
					{ "GU", "en-GU" },
					{ "GW", "pt-GW" },
					{ "GY", "en-GY" },
					{ "HK", "zh-HK" },
					{ "HM", "en-H" },
					{ "HN", "es-HN" },
					{ "HR", "hr-HR" },
					{ "HT", "fr-HT" },
					{ "HU", "hu-HU" },
					{ "ID", "id-ID" },
					{ "IE", "en-IE" },
					{ "IL", "he-IL" },
					{ "IM", "gv-IM" },
					{ "IN", "hi-IN" },
					{ "IO", "en-IO" },
					{ "IQ", "ar-IQ" },
					{ "IR", "fa-IR" },
					{ "IS", "is-IS" },
					{ "IT", "it-IT" },
					{ "IV", "" },
					{ "JE", "en-JE" },
					{ "JM", "en-JM" },
					{ "JO", "ar-JO" },
					{ "JP", "ja-JP" },
					{ "KE", "sw-KE" },
					{ "KG", "ky-KG" },
					{ "KH", "km-KH" },
					{ "KI", "en-KI" },
					{ "KM", "ar-KM" },
					{ "KN", "en-KN" },
					{ "KP", "ko-KP" },
					{ "KR", "ko-KR" },
					{ "KW", "ar-KW" },
					{ "KY", "en-KY" },
					{ "KZ", "kk-KZ" },
					{ "LA", "lo-LA" },
					{ "LB", "ar-LB" },
					{ "LC", "en-LC" },
					{ "LI", "de-LI" },
					{ "LK", "si-LK" },
					{ "LR", "en-LR" },
					{ "LS", "st-LS" },
					{ "LT", "lt-LT" },
					{ "LU", "lb-LU" },
					{ "LV", "lv-LV" },
					{ "LY", "ar-LY" },
					{ "MA", "ar-MA" },
					{ "MC", "fr-MC" },
					{ "MD", "ro-MD" },
					{ "ME", "sr-Latn-ME" },
					{ "MF", "fr-MF" },
					{ "MG", "mg-MG" },
					{ "MH", "en-MH" },
					{ "MK", "mk-MK" },
					{ "ML", "fr-ML" },
					{ "MM", "my-MM" },
					{ "MN", "mn-MN" },
					{ "MO", "zh-MO" },
					{ "MP", "en-MP" },
					{ "MQ", "fr-MQ" },
					{ "MR", "ar-MR" },
					{ "MS", "en-MS" },
					{ "MT", "mt-MT" },
					{ "MU", "en-MU" },
					{ "MV", "dv-MV" },
					{ "MW", "en-MW" },
					{ "MX", "es-MX" },
					{ "MY", "ms-MY" },
					{ "MZ", "pt-MZ" },
					{ "NA", "en-NA" },
					{ "NC", "fr-NC" },
					{ "NE", "fr-NE" },
					{ "NF", "en-NF" },
					{ "NG", "ig-NG" },
					{ "NI", "es-NI" },
					{ "NL", "nl-NL" },
					{ "NO", "nn-NO" },
					{ "NP", "ne-NP" },
					{ "NR", "en-NR" },
					{ "NU", "en-NU" },
					{ "NZ", "en-NZ" },
					{ "OM", "ar-OM" },
					{ "PA", "es-PA" },
					{ "PE", "es-PE" },
					{ "PF", "fr-PF" },
					{ "PG", "en-PG" },
					{ "PH", "en-PH" },
					{ "PK", "ur-PK" },
					{ "PL", "pl-PL" },
					{ "PM", "fr-PM" },
					{ "PN", "en-PN" },
					{ "PR", "es-PR" },
					{ "PS", "ar-PS" },
					{ "PT", "pt-PT" },
					{ "PW", "en-PW" },
					{ "PY", "es-PY" },
					{ "QA", "ar-QA" },
					{ "RE", "fr-RE" },
					{ "RO", "ro-RO" },
					{ "RS", "sr-Latn-RS" },
					{ "RU", "ru-RU" },
					{ "RW", "rw-RW" },
					{ "SA", "ar-SA" },
					{ "SB", "en-SB" },
					{ "SC", "fr-SC" },
					{ "SD", "ar-SD" },
					{ "SE", "sv-SE" },
					{ "SG", "zh-SG" },
					{ "SH", "en-SH" },
					{ "SI", "sl-SI" },
					{ "SJ", "nb-SJ" },
					{ "SK", "sk-SK" },
					{ "SL", "en-SL" },
					{ "SM", "it-SM" },
					{ "SN", "wo-SN" },
					{ "SO", "so-SO" },
					{ "SR", "nl-SR" },
					{ "SS", "en-SS" },
					{ "ST", "pt-ST" },
					{ "SV", "es-SV" },
					{ "SX", "nl-SX" },
					{ "SY", "ar-SY" },
					{ "SZ", "ss-SZ" },
					{ "TC", "en-TC" },
					{ "TD", "fr-TD" },
					{ "TF", "fr-T" },
					{ "TG", "fr-TG" },
					{ "TH", "th-TH" },
					{ "TJ", "tg-Cyrl-TJ" },
					{ "TK", "en-TK" },
					{ "TL", "pt-TL" },
					{ "TM", "tk-TM" },
					{ "TN", "ar-TN" },
					{ "TO", "to-TO" },
					{ "TR", "tr-TR" },
					{ "TT", "en-TT" },
					{ "TV", "en-TV" },
					{ "TW", "zh-TW" },
					{ "TZ", "sw-TZ" },
					{ "UA", "uk-UA" },
					{ "UG", "sw-UG" },
					{ "UM", "en-UM" },
					{ "US", "en-US" },
					{ "UY", "es-UY" },
					{ "UZ", "uz-Cyrl-UZ" },
					{ "VA", "it-VA" },
					{ "VC", "en-VC" },
					{ "VE", "es-VE" },
					{ "VG", "en-VG" },
					{ "VI", "en-VI" },
					{ "VN", "vi-VN" },
					{ "VU", "fr-VU" },
					{ "WF", "fr-WF" },
					{ "WS", "en-WS" },
					{ "XK", "sq-XK" },
					{ "YE", "ar-YE" },
					{ "YT", "fr-YT" },
					{ "ZA", "af-ZA" },
					{ "ZM", "en-ZM" },
					{ "ZW", "en-ZW" }
				};
				s_regionNames = (Dictionary<string, string>)obj;
			}
			return (Dictionary<string, string>)obj;
		}
	}

	internal static CultureData Invariant => s_Invariant ?? (s_Invariant = CreateCultureWithInvariantData());

	internal string CultureName
	{
		get
		{
			string sName = _sName;
			if (sName == "zh-CHS" || sName == "zh-CHT")
			{
				return _sName;
			}
			return _sRealName;
		}
	}

	internal bool UseUserOverride => _bUseOverridesUserSetting;

	internal string Name => _sName ?? string.Empty;

	internal string ParentName => _sParent ?? (_sParent = GetLocaleInfoCore(_sRealName, LocaleStringData.ParentName));

	internal string DisplayName
	{
		get
		{
			string text = NativeName;
			if (!GlobalizationMode.Invariant && Name.Length > 0)
			{
				try
				{
					text = GetLanguageDisplayNameCore(Name.Equals("zh-CHT", StringComparison.OrdinalIgnoreCase) ? "zh-Hant" : (Name.Equals("zh-CHS", StringComparison.OrdinalIgnoreCase) ? "zh-Hans" : Name));
				}
				catch
				{
				}
				if (string.IsNullOrEmpty(text) && IsNeutralCulture)
				{
					text = LocalizedLanguageName;
				}
			}
			return text;
		}
	}

	internal string EnglishName
	{
		get
		{
			string text = _sEnglishDisplayName;
			if (text == null && !GlobalizationMode.Invariant)
			{
				if (IsNeutralCulture)
				{
					text = GetLocaleInfoCore(LocaleStringData.EnglishDisplayName);
					if (string.IsNullOrEmpty(text))
					{
						text = EnglishLanguageName;
					}
					string sName = _sName;
					if (sName == "zh-CHS" || sName == "zh-CHT")
					{
						text += " Legacy";
					}
				}
				else
				{
					text = GetLocaleInfoCore(LocaleStringData.EnglishDisplayName);
					if (string.IsNullOrEmpty(text))
					{
						text = ((EnglishLanguageName[^1] != ')') ? (EnglishLanguageName + " (" + EnglishCountryName + ")") : string.Concat(EnglishLanguageName.AsSpan(0, _sEnglishLanguage.Length - 1), ", ", EnglishCountryName, ")"));
					}
				}
				_sEnglishDisplayName = text;
			}
			return text;
		}
	}

	internal string NativeName
	{
		get
		{
			string text = _sNativeDisplayName;
			if (text == null && !GlobalizationMode.Invariant)
			{
				if (IsNeutralCulture)
				{
					text = GetLocaleInfoCore(LocaleStringData.NativeDisplayName);
					if (string.IsNullOrEmpty(text))
					{
						text = NativeLanguageName;
					}
					string sName = _sName;
					if (!(sName == "zh-CHS"))
					{
						if (sName == "zh-CHT")
						{
							text += " 舊版";
						}
					}
					else
					{
						text += " 旧版";
					}
				}
				else
				{
					text = GetLocaleInfoCore(LocaleStringData.NativeDisplayName);
					if (string.IsNullOrEmpty(text))
					{
						text = NativeLanguageName + " (" + NativeCountryName + ")";
					}
				}
				_sNativeDisplayName = text;
			}
			return text;
		}
	}

	internal string SpecificCultureName => _sSpecificCulture;

	internal string TwoLetterISOLanguageName => _sISO639Language ?? (_sISO639Language = GetLocaleInfoCore(LocaleStringData.Iso639LanguageTwoLetterName));

	internal string ThreeLetterISOLanguageName => _sISO639Language2 ?? (_sISO639Language2 = GetLocaleInfoCore(LocaleStringData.Iso639LanguageThreeLetterName));

	internal string ThreeLetterWindowsLanguageName => _sAbbrevLang ?? (_sAbbrevLang = (GlobalizationMode.UseNls ? NlsGetThreeLetterWindowsLanguageName(_sRealName) : IcuGetThreeLetterWindowsLanguageName(_sRealName)));

	private string LocalizedLanguageName
	{
		get
		{
			string result = NativeLanguageName;
			if (!GlobalizationMode.Invariant && Name.Length > 0 && (!GlobalizationMode.UseNls || CultureInfo.UserDefaultUICulture?.Name == CultureInfo.CurrentUICulture.Name))
			{
				result = GetLocaleInfoCore(LocaleStringData.LocalizedLanguageName, CultureInfo.CurrentUICulture.Name);
			}
			return result;
		}
	}

	private string EnglishLanguageName => _sEnglishLanguage ?? (_sEnglishLanguage = GetLocaleInfoCore(LocaleStringData.EnglishLanguageName));

	private string NativeLanguageName => _sNativeLanguage ?? (_sNativeLanguage = GetLocaleInfoCore(LocaleStringData.NativeLanguageName));

	internal string RegionName => _sRegionName ?? (_sRegionName = GetLocaleInfoCore(LocaleStringData.Iso3166CountryName));

	internal int GeoId
	{
		get
		{
			if (_iGeoId == -1 && !GlobalizationMode.Invariant)
			{
				_iGeoId = (GlobalizationMode.UseNls ? NlsGetLocaleInfo(LocaleNumberData.GeoId) : IcuGetGeoId(_sRealName));
			}
			return _iGeoId;
		}
	}

	internal string LocalizedCountryName
	{
		get
		{
			string text = _sLocalizedCountry;
			if (text == null && !GlobalizationMode.Invariant)
			{
				try
				{
					text = (GlobalizationMode.UseNls ? NlsGetRegionDisplayName() : IcuGetRegionDisplayName());
				}
				catch
				{
				}
				if (text == null)
				{
					text = NativeCountryName;
				}
				_sLocalizedCountry = text;
			}
			return text;
		}
	}

	internal string EnglishCountryName => _sEnglishCountry ?? (_sEnglishCountry = GetLocaleInfoCore(LocaleStringData.EnglishCountryName));

	internal string NativeCountryName => _sNativeCountry ?? (_sNativeCountry = GetLocaleInfoCore(LocaleStringData.NativeCountryName));

	internal string TwoLetterISOCountryName => _sISO3166CountryName ?? (_sISO3166CountryName = GetLocaleInfoCore(LocaleStringData.Iso3166CountryName));

	internal string ThreeLetterISOCountryName => _sISO3166CountryName2 ?? (_sISO3166CountryName2 = GetLocaleInfoCore(LocaleStringData.Iso3166CountryName2));

	internal int KeyboardLayoutId
	{
		get
		{
			if (_iInputLanguageHandle == -1)
			{
				if (IsSupplementalCustomCulture)
				{
					_iInputLanguageHandle = 1033;
				}
				else
				{
					_iInputLanguageHandle = LCID;
				}
			}
			return _iInputLanguageHandle;
		}
	}

	internal string SCONSOLEFALLBACKNAME => _sConsoleFallbackName ?? (_sConsoleFallbackName = (GlobalizationMode.UseNls ? NlsGetConsoleFallbackName(_sRealName) : IcuGetConsoleFallbackName(_sRealName)));

	internal int[] NumberGroupSizes => _waGrouping ?? (_waGrouping = GetLocaleInfoCoreUserOverride(LocaleGroupingData.Digit));

	private string NaNSymbol => _sNaN ?? (_sNaN = GetLocaleInfoCore(LocaleStringData.NaNSymbol));

	private string PositiveInfinitySymbol => _sPositiveInfinity ?? (_sPositiveInfinity = GetLocaleInfoCore(LocaleStringData.PositiveInfinitySymbol));

	private string NegativeInfinitySymbol => _sNegativeInfinity ?? (_sNegativeInfinity = GetLocaleInfoCore(LocaleStringData.NegativeInfinitySymbol));

	private int PercentNegativePattern
	{
		get
		{
			if (_iNegativePercent == -1)
			{
				_iNegativePercent = GetLocaleInfoCore(LocaleNumberData.NegativePercentFormat);
			}
			return _iNegativePercent;
		}
	}

	private int PercentPositivePattern
	{
		get
		{
			if (_iPositivePercent == -1)
			{
				_iPositivePercent = GetLocaleInfoCore(LocaleNumberData.PositivePercentFormat);
			}
			return _iPositivePercent;
		}
	}

	private string PercentSymbol => _sPercent ?? (_sPercent = GetLocaleInfoCore(LocaleStringData.PercentSymbol));

	private string PerMilleSymbol => _sPerMille ?? (_sPerMille = GetLocaleInfoCore(LocaleStringData.PerMilleSymbol));

	internal string CurrencySymbol => _sCurrency ?? (_sCurrency = GetLocaleInfoCoreUserOverride(LocaleStringData.MonetarySymbol));

	internal string ISOCurrencySymbol => _sIntlMonetarySymbol ?? (_sIntlMonetarySymbol = GetLocaleInfoCore(LocaleStringData.Iso4217MonetarySymbol));

	internal string CurrencyEnglishName => _sEnglishCurrency ?? (_sEnglishCurrency = GetLocaleInfoCore(LocaleStringData.CurrencyEnglishName));

	internal string CurrencyNativeName => _sNativeCurrency ?? (_sNativeCurrency = GetLocaleInfoCore(LocaleStringData.CurrencyNativeName));

	internal int[] CurrencyGroupSizes => _waMonetaryGrouping ?? (_waMonetaryGrouping = GetLocaleInfoCoreUserOverride(LocaleGroupingData.Monetary));

	internal int MeasurementSystem
	{
		get
		{
			if (_iMeasure == -1)
			{
				_iMeasure = GetLocaleInfoCoreUserOverride(LocaleNumberData.MeasurementSystem);
			}
			return _iMeasure;
		}
	}

	internal string ListSeparator => _sListSeparator ?? (_sListSeparator = (ShouldUseUserOverrideNlsData ? NlsGetLocaleInfo(LocaleStringData.ListSeparator) : IcuGetListSeparator(_sWindowsName)));

	internal string AMDesignator => _sAM1159 ?? (_sAM1159 = GetLocaleInfoCoreUserOverride(LocaleStringData.AMDesignator));

	internal string PMDesignator => _sPM2359 ?? (_sPM2359 = GetLocaleInfoCoreUserOverride(LocaleStringData.PMDesignator));

	internal string[] LongTimes
	{
		get
		{
			if (_saLongTimes == null && !GlobalizationMode.Invariant)
			{
				string[] timeFormatsCore = GetTimeFormatsCore(shortFormat: false);
				if (timeFormatsCore == null || timeFormatsCore.Length == 0)
				{
					_saLongTimes = Invariant._saLongTimes;
				}
				else
				{
					_saLongTimes = timeFormatsCore;
				}
			}
			return _saLongTimes;
		}
	}

	internal string[] ShortTimes
	{
		get
		{
			if (_saShortTimes == null && !GlobalizationMode.Invariant)
			{
				string[] array = GetTimeFormatsCore(shortFormat: true);
				if (array == null || array.Length == 0)
				{
					array = DeriveShortTimesFromLong();
				}
				_saShortTimes = array;
			}
			return _saShortTimes;
		}
	}

	internal int FirstDayOfWeek
	{
		get
		{
			if (_iFirstDayOfWeek == -1 && !GlobalizationMode.Invariant)
			{
				_iFirstDayOfWeek = (ShouldUseUserOverrideNlsData ? NlsGetFirstDayOfWeek() : IcuGetLocaleInfo(LocaleNumberData.FirstDayOfWeek));
			}
			return _iFirstDayOfWeek;
		}
	}

	internal int CalendarWeekRule
	{
		get
		{
			if (_iFirstWeekOfYear == -1)
			{
				_iFirstWeekOfYear = GetLocaleInfoCoreUserOverride(LocaleNumberData.FirstWeekOfYear);
			}
			return _iFirstWeekOfYear;
		}
	}

	internal CalendarId[] CalendarIds
	{
		get
		{
			if (_waCalendars == null && !GlobalizationMode.Invariant)
			{
				CalendarId[] array = new CalendarId[23];
				int num = CalendarData.GetCalendarsCore(_sWindowsName, _bUseOverrides, array);
				if (num == 0)
				{
					_waCalendars = Invariant._waCalendars;
				}
				else
				{
					if (_sWindowsName == "zh-TW")
					{
						bool flag = false;
						for (int i = 0; i < num; i++)
						{
							if (array[i] == CalendarId.TAIWAN)
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							num++;
							Array.Copy(array, 1, array, 2, 21);
							array[1] = CalendarId.TAIWAN;
						}
					}
					CalendarId[] array2 = new CalendarId[num];
					Array.Copy(array, array2, num);
					_waCalendars = array2;
				}
			}
			return _waCalendars;
		}
	}

	internal bool IsRightToLeft => ReadingLayout == 1;

	private int ReadingLayout
	{
		get
		{
			if (_iReadingLayout == -1 && !GlobalizationMode.Invariant)
			{
				_iReadingLayout = GetLocaleInfoCore(LocaleNumberData.ReadingLayout);
			}
			return _iReadingLayout;
		}
	}

	internal string TextInfoName => _sRealName;

	internal string SortName => _sRealName;

	internal bool IsSupplementalCustomCulture => IsCustomCultureId(LCID);

	internal int ANSICodePage
	{
		get
		{
			if (_iDefaultAnsiCodePage == -1 && !GlobalizationMode.Invariant)
			{
				_iDefaultAnsiCodePage = GetAnsiCodePage(_sRealName);
			}
			return _iDefaultAnsiCodePage;
		}
	}

	internal int OEMCodePage
	{
		get
		{
			if (_iDefaultOemCodePage == -1 && !GlobalizationMode.Invariant)
			{
				_iDefaultOemCodePage = GetOemCodePage(_sRealName);
			}
			return _iDefaultOemCodePage;
		}
	}

	internal int MacCodePage
	{
		get
		{
			if (_iDefaultMacCodePage == -1 && !GlobalizationMode.Invariant)
			{
				_iDefaultMacCodePage = GetMacCodePage(_sRealName);
			}
			return _iDefaultMacCodePage;
		}
	}

	internal int EBCDICCodePage
	{
		get
		{
			if (_iDefaultEbcdicCodePage == -1 && !GlobalizationMode.Invariant)
			{
				_iDefaultEbcdicCodePage = GetEbcdicCodePage(_sRealName);
			}
			return _iDefaultEbcdicCodePage;
		}
	}

	internal int LCID
	{
		get
		{
			if (_iLanguage == 0 && !GlobalizationMode.Invariant)
			{
				_iLanguage = (GlobalizationMode.UseNls ? NlsLocaleNameToLCID(_sRealName) : IcuLocaleNameToLCID(_sRealName));
			}
			return _iLanguage;
		}
	}

	internal bool IsNeutralCulture => _bNeutral;

	internal bool IsInvariantCulture => string.IsNullOrEmpty(Name);

	internal bool IsReplacementCulture
	{
		get
		{
			if (!GlobalizationMode.UseNls)
			{
				return false;
			}
			return NlsIsReplacementCulture;
		}
	}

	internal Calendar DefaultCalendar
	{
		get
		{
			if (GlobalizationMode.Invariant)
			{
				return new GregorianCalendar();
			}
			CalendarId calendarId = (CalendarId)GetLocaleInfoCore(LocaleNumberData.CalendarType);
			if (calendarId == CalendarId.UNINITIALIZED_VALUE)
			{
				calendarId = CalendarIds[0];
			}
			return CultureInfo.GetCalendarInstance(calendarId);
		}
	}

	internal string TimeSeparator
	{
		get
		{
			if (_sTimeSeparator == null && !GlobalizationMode.Invariant)
			{
				if (_sName == "fr-CA")
				{
					_sTimeSeparator = ":";
				}
				else
				{
					string text = (ShouldUseUserOverrideNlsData ? NlsGetTimeFormatString() : IcuGetTimeFormatString());
					if (string.IsNullOrEmpty(text))
					{
						text = LongTimes[0];
					}
					_sTimeSeparator = GetTimeSeparator(text);
				}
			}
			return _sTimeSeparator;
		}
	}

	internal unsafe bool NlsIsReplacementCulture
	{
		get
		{
			EnumData value = default(EnumData);
			value.strings = new List<string>();
			Interop.Kernel32.EnumSystemLocalesEx((delegate* unmanaged<char*, uint, void*, Interop.BOOL>)(delegate*<char*, uint, void*, Interop.BOOL>)(&EnumAllSystemLocalesProc), 8u, Unsafe.AsPointer(ref value), IntPtr.Zero);
			for (int i = 0; i < value.strings.Count; i++)
			{
				if (string.Equals(value.strings[i], _sWindowsName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}
	}

	internal static bool IsWin32Installed => true;

	private bool ShouldUseUserOverrideNlsData
	{
		get
		{
			if (!GlobalizationMode.UseNls)
			{
				return _bUseOverrides;
			}
			return true;
		}
	}

	internal static CultureData GetCultureDataForRegion(string cultureName, bool useUserOverride)
	{
		if (string.IsNullOrEmpty(cultureName))
		{
			return Invariant;
		}
		CultureData cultureData = null;
		cultureData = GetCultureData(cultureName, useUserOverride);
		if (cultureData != null && !cultureData.IsNeutralCulture)
		{
			return cultureData;
		}
		CultureData cultureData2 = cultureData;
		string key = AnsiToLower(useUserOverride ? cultureName : (cultureName + "*"));
		Dictionary<string, CultureData> dictionary = s_cachedRegions;
		if (dictionary == null)
		{
			dictionary = new Dictionary<string, CultureData>();
		}
		else
		{
			lock (s_lock)
			{
				dictionary.TryGetValue(key, out cultureData);
			}
			if (cultureData != null)
			{
				return cultureData;
			}
		}
		if ((cultureData == null || cultureData.IsNeutralCulture) && RegionNames.TryGetValue(cultureName, out var value))
		{
			cultureData = GetCultureData(value, useUserOverride);
		}
		if (!GlobalizationMode.Invariant && (cultureData == null || cultureData.IsNeutralCulture))
		{
			cultureData = (GlobalizationMode.UseNls ? NlsGetCultureDataFromRegionName(cultureName) : IcuGetCultureDataFromRegionName(cultureName));
		}
		if (cultureData != null && !cultureData.IsNeutralCulture)
		{
			lock (s_lock)
			{
				dictionary[key] = cultureData;
			}
			s_cachedRegions = dictionary;
		}
		else
		{
			cultureData = cultureData2;
		}
		return cultureData;
	}

	internal static void ClearCachedData()
	{
		s_cachedCultures = null;
		s_cachedRegions = null;
	}

	internal static CultureInfo[] GetCultures(CultureTypes types)
	{
		if (types <= (CultureTypes)0 || ((uint)types & 0xFFFFFF80u) != 0)
		{
			throw new ArgumentOutOfRangeException("types", SR.Format(SR.ArgumentOutOfRange_Range, CultureTypes.NeutralCultures, CultureTypes.FrameworkCultures));
		}
		if ((types & CultureTypes.WindowsOnlyCultures) != 0)
		{
			types &= ~CultureTypes.WindowsOnlyCultures;
		}
		if (GlobalizationMode.Invariant)
		{
			return new CultureInfo[1]
			{
				new CultureInfo("")
			};
		}
		if (!GlobalizationMode.UseNls)
		{
			return IcuEnumCultures(types);
		}
		return NlsEnumCultures(types);
	}

	private static CultureData CreateCultureWithInvariantData()
	{
		CultureData cultureData = new CultureData();
		cultureData._bUseOverrides = false;
		cultureData._bUseOverridesUserSetting = false;
		cultureData._sRealName = "";
		cultureData._sWindowsName = "";
		cultureData._sName = "";
		cultureData._sParent = "";
		cultureData._bNeutral = false;
		cultureData._sEnglishDisplayName = "Invariant Language (Invariant Country)";
		cultureData._sNativeDisplayName = "Invariant Language (Invariant Country)";
		cultureData._sSpecificCulture = "";
		cultureData._sISO639Language = "iv";
		cultureData._sISO639Language2 = "ivl";
		cultureData._sEnglishLanguage = "Invariant Language";
		cultureData._sNativeLanguage = "Invariant Language";
		cultureData._sAbbrevLang = "IVL";
		cultureData._sConsoleFallbackName = "";
		cultureData._iInputLanguageHandle = 127;
		cultureData._sRegionName = "IV";
		cultureData._sEnglishCountry = "Invariant Country";
		cultureData._sNativeCountry = "Invariant Country";
		cultureData._sISO3166CountryName = "IV";
		cultureData._sISO3166CountryName2 = "ivc";
		cultureData._iGeoId = 244;
		cultureData._sPositiveSign = "+";
		cultureData._sNegativeSign = "-";
		cultureData._iDigits = 2;
		cultureData._iNegativeNumber = 1;
		cultureData._waGrouping = new int[1] { 3 };
		cultureData._sDecimalSeparator = ".";
		cultureData._sThousandSeparator = ",";
		cultureData._sNaN = "NaN";
		cultureData._sPositiveInfinity = "Infinity";
		cultureData._sNegativeInfinity = "-Infinity";
		cultureData._iNegativePercent = 0;
		cultureData._iPositivePercent = 0;
		cultureData._sPercent = "%";
		cultureData._sPerMille = "‰";
		cultureData._sCurrency = "¤";
		cultureData._sIntlMonetarySymbol = "XDR";
		cultureData._sEnglishCurrency = "International Monetary Fund";
		cultureData._sNativeCurrency = "International Monetary Fund";
		cultureData._iCurrencyDigits = 2;
		cultureData._iCurrency = 0;
		cultureData._iNegativeCurrency = 0;
		cultureData._waMonetaryGrouping = new int[1] { 3 };
		cultureData._sMonetaryDecimal = ".";
		cultureData._sMonetaryThousand = ",";
		cultureData._iMeasure = 0;
		cultureData._sListSeparator = ",";
		cultureData._sTimeSeparator = ":";
		cultureData._sAM1159 = "AM";
		cultureData._sPM2359 = "PM";
		cultureData._saLongTimes = new string[1] { "HH:mm:ss" };
		cultureData._saShortTimes = new string[4] { "HH:mm", "hh:mm tt", "H:mm", "h:mm tt" };
		cultureData._saDurationFormats = new string[1] { "HH:mm:ss" };
		cultureData._iFirstDayOfWeek = 0;
		cultureData._iFirstWeekOfYear = 0;
		cultureData._waCalendars = new CalendarId[1] { CalendarId.GREGORIAN };
		if (!GlobalizationMode.Invariant)
		{
			cultureData._calendars = new CalendarData[23];
			cultureData._calendars[0] = CalendarData.Invariant;
		}
		cultureData._iReadingLayout = 0;
		cultureData._iLanguage = 127;
		cultureData._iDefaultAnsiCodePage = 1252;
		cultureData._iDefaultOemCodePage = 437;
		cultureData._iDefaultMacCodePage = 10000;
		cultureData._iDefaultEbcdicCodePage = 37;
		if (GlobalizationMode.Invariant)
		{
			cultureData._sLocalizedCountry = cultureData._sNativeCountry;
		}
		return cultureData;
	}

	internal static CultureData GetCultureData(string cultureName, bool useUserOverride)
	{
		if (string.IsNullOrEmpty(cultureName))
		{
			return Invariant;
		}
		if (GlobalizationMode.PredefinedCulturesOnly && (GlobalizationMode.Invariant || (GlobalizationMode.UseNls ? (!NlsIsEnsurePredefinedLocaleName(cultureName)) : (!IcuIsEnsurePredefinedLocaleName(cultureName)))))
		{
			return null;
		}
		string key = AnsiToLower(useUserOverride ? cultureName : (cultureName + "*"));
		Dictionary<string, CultureData> dictionary = s_cachedCultures;
		if (dictionary == null)
		{
			dictionary = new Dictionary<string, CultureData>();
		}
		else
		{
			bool flag;
			CultureData value;
			lock (s_lock)
			{
				flag = dictionary.TryGetValue(key, out value);
			}
			if (flag && value != null)
			{
				return value;
			}
		}
		CultureData cultureData = CreateCultureData(cultureName, useUserOverride);
		if (cultureData == null)
		{
			return null;
		}
		lock (s_lock)
		{
			dictionary[key] = cultureData;
		}
		s_cachedCultures = dictionary;
		return cultureData;
	}

	private static string NormalizeCultureName(string name, out bool isNeutralName)
	{
		isNeutralName = true;
		int i = 0;
		if (name.Length > 85)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidId, "name"));
		}
		Span<char> span = stackalloc char[name.Length];
		bool flag = false;
		for (; i < name.Length && name[i] != '-' && name[i] != '_'; i++)
		{
			if (name[i] >= 'A' && name[i] <= 'Z')
			{
				span[i] = (char)(name[i] + 32);
				flag = true;
			}
			else
			{
				span[i] = name[i];
			}
		}
		if (i < name.Length)
		{
			isNeutralName = false;
		}
		for (; i < name.Length; i++)
		{
			if (name[i] >= 'a' && name[i] <= 'z')
			{
				span[i] = (char)(name[i] - 32);
				flag = true;
			}
			else
			{
				span[i] = name[i];
			}
		}
		if (flag)
		{
			return new string(span);
		}
		return name;
	}

	private static CultureData CreateCultureData(string cultureName, bool useUserOverride)
	{
		if (GlobalizationMode.Invariant)
		{
			if (cultureName.Length > 85 || !CultureInfo.VerifyCultureName(cultureName, throwException: false))
			{
				return null;
			}
			CultureData cultureData = CreateCultureWithInvariantData();
			cultureData._sName = NormalizeCultureName(cultureName, out cultureData._bNeutral);
			cultureData._bUseOverridesUserSetting = useUserOverride;
			cultureData._sRealName = cultureData._sName;
			cultureData._sWindowsName = cultureData._sName;
			cultureData._iLanguage = 4096;
			return cultureData;
		}
		if (cultureName.Length == 1 && (cultureName[0] == 'C' || cultureName[0] == 'c'))
		{
			return Invariant;
		}
		CultureData cultureData2 = new CultureData();
		cultureData2._sRealName = cultureName;
		cultureData2._bUseOverridesUserSetting = useUserOverride;
		if (!cultureData2.InitCultureDataCore() && !cultureData2.InitCompatibilityCultureData())
		{
			return null;
		}
		cultureData2.InitUserOverride(useUserOverride);
		return cultureData2;
	}

	private bool InitCompatibilityCultureData()
	{
		string sRealName = _sRealName;
		string text = AnsiToLower(sRealName);
		string text2;
		string sName;
		if (!(text == "zh-chs"))
		{
			if (!(text == "zh-cht"))
			{
				return false;
			}
			text2 = "zh-Hant";
			sName = "zh-CHT";
		}
		else
		{
			text2 = "zh-Hans";
			sName = "zh-CHS";
		}
		_sRealName = text2;
		if (!InitCultureDataCore())
		{
			return false;
		}
		_sName = sName;
		_sParent = text2;
		return true;
	}

	internal static CultureData GetCultureData(int culture, bool bUseUserOverride)
	{
		string text = null;
		CultureData cultureData = null;
		if (culture == 127)
		{
			return Invariant;
		}
		if (GlobalizationMode.Invariant)
		{
			throw new CultureNotFoundException("culture", culture, SR.Argument_CultureNotSupportedInInvariantMode);
		}
		text = LCIDToLocaleName(culture);
		if (!string.IsNullOrEmpty(text))
		{
			cultureData = GetCultureData(text, bUseUserOverride);
		}
		if (cultureData == null)
		{
			throw new CultureNotFoundException("culture", culture, SR.Argument_CultureNotSupported);
		}
		return cultureData;
	}

	private string GetLanguageDisplayNameCore(string cultureName)
	{
		if (!GlobalizationMode.UseNls)
		{
			return IcuGetLanguageDisplayName(cultureName);
		}
		return NlsGetLanguageDisplayName(cultureName);
	}

	private string[] DeriveShortTimesFromLong()
	{
		string[] longTimes = LongTimes;
		string[] array = new string[longTimes.Length];
		for (int i = 0; i < longTimes.Length; i++)
		{
			array[i] = StripSecondsFromPattern(longTimes[i]);
		}
		return array;
	}

	private static string StripSecondsFromPattern(string time)
	{
		bool flag = false;
		int num = -1;
		for (int i = 0; i < time.Length; i++)
		{
			if (time[i] == '\'')
			{
				flag = !flag;
			}
			else if (time[i] == '\\')
			{
				i++;
			}
			else
			{
				if (flag)
				{
					continue;
				}
				switch (time[i])
				{
				case 's':
				{
					if (i - num <= 4 && i - num > 1 && time[num + 1] != '\'' && time[i - 1] != '\'' && num >= 0)
					{
						i = num + 1;
					}
					bool containsSpace;
					int indexOfNextTokenAfterSeconds = GetIndexOfNextTokenAfterSeconds(time, i, out containsSpace);
					time = string.Concat(str1: (!containsSpace) ? "" : " ", str0: time.AsSpan(0, i), str2: time.AsSpan(indexOfNextTokenAfterSeconds));
					break;
				}
				case 'H':
				case 'h':
				case 'm':
					num = i;
					break;
				}
			}
		}
		return time;
	}

	private static int GetIndexOfNextTokenAfterSeconds(string time, int index, out bool containsSpace)
	{
		bool flag = false;
		containsSpace = false;
		while (index < time.Length)
		{
			switch (time[index])
			{
			case '\'':
				flag = !flag;
				break;
			case '\\':
				index++;
				if (time[index] == ' ')
				{
					containsSpace = true;
				}
				break;
			case ' ':
				containsSpace = true;
				break;
			case 'H':
			case 'h':
			case 'm':
			case 't':
				if (!flag)
				{
					return index;
				}
				break;
			}
			index++;
		}
		containsSpace = false;
		return index;
	}

	internal string[] ShortDates(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saShortDates;
	}

	internal string[] LongDates(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saLongDates;
	}

	internal string[] YearMonths(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saYearMonths;
	}

	internal string[] DayNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saDayNames;
	}

	internal string[] AbbreviatedDayNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saAbbrevDayNames;
	}

	internal string[] SuperShortDayNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saSuperShortDayNames;
	}

	internal string[] MonthNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saMonthNames;
	}

	internal string[] GenitiveMonthNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saMonthGenitiveNames;
	}

	internal string[] AbbreviatedMonthNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saAbbrevMonthNames;
	}

	internal string[] AbbreviatedGenitiveMonthNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saAbbrevMonthGenitiveNames;
	}

	internal string[] LeapYearMonthNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saLeapYearMonthNames;
	}

	internal string MonthDay(CalendarId calendarId)
	{
		return GetCalendar(calendarId).sMonthDay;
	}

	internal string CalendarName(CalendarId calendarId)
	{
		return GetCalendar(calendarId).sNativeName;
	}

	internal CalendarData GetCalendar(CalendarId calendarId)
	{
		if (GlobalizationMode.Invariant)
		{
			return CalendarData.Invariant;
		}
		int num = (int)(calendarId - 1);
		if (_calendars == null)
		{
			_calendars = new CalendarData[23];
		}
		CalendarData calendarData = _calendars[num];
		if (calendarData == null)
		{
			calendarData = new CalendarData(_sWindowsName, calendarId, _bUseOverrides);
			_calendars[num] = calendarData;
		}
		return calendarData;
	}

	internal string[] EraNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saEraNames;
	}

	internal string[] AbbrevEraNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saAbbrevEraNames;
	}

	internal string[] AbbreviatedEnglishEraNames(CalendarId calendarId)
	{
		return GetCalendar(calendarId).saAbbrevEnglishEraNames;
	}

	internal string DateSeparator(CalendarId calendarId)
	{
		if (GlobalizationMode.Invariant)
		{
			return "/";
		}
		if (calendarId == CalendarId.JAPAN && !LocalAppContextSwitches.EnforceLegacyJapaneseDateParsing)
		{
			return "/";
		}
		return GetDateSeparator(ShortDates(calendarId)[0]);
	}

	private static string UnescapeNlsString(string str, int start, int end)
	{
		StringBuilder stringBuilder = null;
		for (int i = start; i < str.Length && i <= end; i++)
		{
			switch (str[i])
			{
			case '\'':
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(str, start, i - start, str.Length);
				}
				break;
			case '\\':
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(str, start, i - start, str.Length);
				}
				i++;
				if (i < str.Length)
				{
					stringBuilder.Append(str[i]);
				}
				break;
			default:
				stringBuilder?.Append(str[i]);
				break;
			}
		}
		if (stringBuilder == null)
		{
			return str.Substring(start, end - start + 1);
		}
		return stringBuilder.ToString();
	}

	private static string GetTimeSeparator(string format)
	{
		return GetSeparator(format, "Hhms");
	}

	private static string GetDateSeparator(string format)
	{
		return GetSeparator(format, "dyM");
	}

	private static string GetSeparator(string format, string timeParts)
	{
		int num = IndexOfTimePart(format, 0, timeParts);
		if (num != -1)
		{
			char c = format[num];
			do
			{
				num++;
			}
			while (num < format.Length && format[num] == c);
			int num2 = num;
			if (num2 < format.Length)
			{
				int num3 = IndexOfTimePart(format, num2, timeParts);
				if (num3 != -1)
				{
					return UnescapeNlsString(format, num2, num3 - 1);
				}
			}
		}
		return string.Empty;
	}

	private static int IndexOfTimePart(string format, int startIndex, string timeParts)
	{
		bool flag = false;
		for (int i = startIndex; i < format.Length; i++)
		{
			if (!flag && timeParts.Contains(format[i]))
			{
				return i;
			}
			switch (format[i])
			{
			case '\\':
				if (i + 1 < format.Length)
				{
					i++;
					char c = format[i];
					if (c != '\'' && c != '\\')
					{
						i--;
					}
				}
				break;
			case '\'':
				flag = !flag;
				break;
			}
		}
		return -1;
	}

	internal static bool IsCustomCultureId(int cultureId)
	{
		if (cultureId != 3072)
		{
			return cultureId == 4096;
		}
		return true;
	}

	internal void GetNFIValues(NumberFormatInfo nfi)
	{
		if (GlobalizationMode.Invariant || IsInvariantCulture)
		{
			nfi._positiveSign = _sPositiveSign;
			nfi._negativeSign = _sNegativeSign;
			nfi._numberGroupSeparator = _sThousandSeparator;
			nfi._numberDecimalSeparator = _sDecimalSeparator;
			nfi._numberDecimalDigits = _iDigits;
			nfi._numberNegativePattern = _iNegativeNumber;
			nfi._currencySymbol = _sCurrency;
			nfi._currencyGroupSeparator = _sMonetaryThousand;
			nfi._currencyDecimalSeparator = _sMonetaryDecimal;
			nfi._currencyDecimalDigits = _iCurrencyDigits;
			nfi._currencyNegativePattern = _iNegativeCurrency;
			nfi._currencyPositivePattern = _iCurrency;
		}
		else
		{
			nfi._positiveSign = GetLocaleInfoCoreUserOverride(LocaleStringData.PositiveSign);
			nfi._negativeSign = GetLocaleInfoCoreUserOverride(LocaleStringData.NegativeSign);
			nfi._numberDecimalSeparator = GetLocaleInfoCoreUserOverride(LocaleStringData.DecimalSeparator);
			nfi._numberGroupSeparator = GetLocaleInfoCoreUserOverride(LocaleStringData.ThousandSeparator);
			nfi._currencyGroupSeparator = GetLocaleInfoCoreUserOverride(LocaleStringData.MonetaryThousandSeparator);
			nfi._currencyDecimalSeparator = GetLocaleInfoCoreUserOverride(LocaleStringData.MonetaryDecimalSeparator);
			nfi._currencySymbol = GetLocaleInfoCoreUserOverride(LocaleStringData.MonetarySymbol);
			nfi._numberDecimalDigits = GetLocaleInfoCoreUserOverride(LocaleNumberData.FractionalDigitsCount);
			nfi._currencyDecimalDigits = GetLocaleInfoCoreUserOverride(LocaleNumberData.MonetaryFractionalDigitsCount);
			nfi._currencyPositivePattern = GetLocaleInfoCoreUserOverride(LocaleNumberData.PositiveMonetaryNumberFormat);
			nfi._currencyNegativePattern = GetLocaleInfoCoreUserOverride(LocaleNumberData.NegativeMonetaryNumberFormat);
			nfi._numberNegativePattern = GetLocaleInfoCoreUserOverride(LocaleNumberData.NegativeNumberFormat);
			string localeInfoCoreUserOverride = GetLocaleInfoCoreUserOverride(LocaleStringData.Digits);
			nfi._nativeDigits = new string[10];
			for (int i = 0; i < nfi._nativeDigits.Length; i++)
			{
				nfi._nativeDigits[i] = char.ToString(localeInfoCoreUserOverride[i]);
			}
			nfi._digitSubstitution = (ShouldUseUserOverrideNlsData ? NlsGetLocaleInfo(LocaleNumberData.DigitSubstitution) : IcuGetDigitSubstitution(_sRealName));
		}
		nfi._numberGroupSizes = NumberGroupSizes;
		nfi._currencyGroupSizes = CurrencyGroupSizes;
		nfi._percentNegativePattern = PercentNegativePattern;
		nfi._percentPositivePattern = PercentPositivePattern;
		nfi._percentSymbol = PercentSymbol;
		nfi._perMilleSymbol = PerMilleSymbol;
		nfi._negativeInfinitySymbol = NegativeInfinitySymbol;
		nfi._positiveInfinitySymbol = PositiveInfinitySymbol;
		nfi._nanSymbol = NaNSymbol;
		nfi._percentDecimalDigits = nfi._numberDecimalDigits;
		nfi._percentDecimalSeparator = nfi._numberDecimalSeparator;
		nfi._percentGroupSizes = nfi._numberGroupSizes;
		nfi._percentGroupSeparator = nfi._numberGroupSeparator;
		if (string.IsNullOrEmpty(nfi._positiveSign))
		{
			nfi._positiveSign = "+";
		}
		if (string.IsNullOrEmpty(nfi._currencyDecimalSeparator))
		{
			nfi._currencyDecimalSeparator = nfi._numberDecimalSeparator;
		}
	}

	internal static string AnsiToLower(string testString)
	{
		return TextInfo.ToLowerAsciiInvariant(testString);
	}

	private int GetLocaleInfoCore(LocaleNumberData type)
	{
		if (GlobalizationMode.Invariant)
		{
			return 0;
		}
		if (!GlobalizationMode.UseNls)
		{
			return IcuGetLocaleInfo(type);
		}
		return NlsGetLocaleInfo(type);
	}

	private int GetLocaleInfoCoreUserOverride(LocaleNumberData type)
	{
		if (GlobalizationMode.Invariant)
		{
			return 0;
		}
		if (!ShouldUseUserOverrideNlsData)
		{
			return IcuGetLocaleInfo(type);
		}
		return NlsGetLocaleInfo(type);
	}

	private string GetLocaleInfoCoreUserOverride(LocaleStringData type)
	{
		if (GlobalizationMode.Invariant)
		{
			return null;
		}
		if (!ShouldUseUserOverrideNlsData)
		{
			return IcuGetLocaleInfo(type);
		}
		return NlsGetLocaleInfo(type);
	}

	private string GetLocaleInfoCore(LocaleStringData type, string uiCultureName = null)
	{
		if (GlobalizationMode.Invariant)
		{
			return null;
		}
		if (!GlobalizationMode.UseNls)
		{
			return IcuGetLocaleInfo(type, uiCultureName);
		}
		return NlsGetLocaleInfo(type);
	}

	private string GetLocaleInfoCore(string localeName, LocaleStringData type, string uiCultureName = null)
	{
		if (GlobalizationMode.Invariant)
		{
			return null;
		}
		if (!GlobalizationMode.UseNls)
		{
			return IcuGetLocaleInfo(localeName, type, uiCultureName);
		}
		return NlsGetLocaleInfo(localeName, type);
	}

	private int[] GetLocaleInfoCoreUserOverride(LocaleGroupingData type)
	{
		if (GlobalizationMode.Invariant)
		{
			return null;
		}
		if (!ShouldUseUserOverrideNlsData)
		{
			return IcuGetLocaleInfo(type);
		}
		return NlsGetLocaleInfo(type);
	}

	private bool InitIcuCultureDataCore()
	{
		string text = _sRealName;
		if (!IsValidCultureName(text, out var indexOfUnderscore))
		{
			return false;
		}
		ReadOnlySpan<char> readOnlySpan = default(ReadOnlySpan<char>);
		if (indexOfUnderscore > 0)
		{
			readOnlySpan = text.AsSpan(indexOfUnderscore + 1);
			text = string.Concat(text.AsSpan(0, indexOfUnderscore), "@collation=", readOnlySpan);
		}
		if (!GetLocaleName(text, out _sWindowsName))
		{
			return false;
		}
		indexOfUnderscore = _sWindowsName.IndexOf("@collation=", StringComparison.Ordinal);
		if (indexOfUnderscore >= 0)
		{
			_sName = string.Concat(_sWindowsName.AsSpan(0, indexOfUnderscore), "_", readOnlySpan);
		}
		else
		{
			_sName = _sWindowsName;
		}
		_sRealName = _sName;
		_iLanguage = LCID;
		if (_iLanguage == 0)
		{
			_iLanguage = 4096;
		}
		_bNeutral = TwoLetterISOCountryName.Length == 0;
		_sSpecificCulture = (_bNeutral ? IcuLocaleData.GetSpecificCultureName(_sRealName) : _sRealName);
		if (indexOfUnderscore > 0 && !_bNeutral && !IsCustomCultureId(_iLanguage))
		{
			_sName = _sWindowsName.Substring(0, indexOfUnderscore);
		}
		return true;
	}

	internal unsafe static bool GetLocaleName(string localeName, out string windowsName)
	{
		char* value = stackalloc char[157];
		if (!Interop.Globalization.GetLocaleName(localeName, value, 157))
		{
			windowsName = null;
			return false;
		}
		windowsName = new string(value);
		return true;
	}

	private string IcuGetLocaleInfo(LocaleStringData type, string uiCultureName = null)
	{
		return IcuGetLocaleInfo(_sWindowsName, type, uiCultureName);
	}

	private unsafe string IcuGetLocaleInfo(string localeName, LocaleStringData type, string uiCultureName = null)
	{
		if (type == LocaleStringData.NegativeInfinitySymbol)
		{
			return IcuGetLocaleInfo(localeName, LocaleStringData.NegativeSign) + IcuGetLocaleInfo(localeName, LocaleStringData.PositiveInfinitySymbol);
		}
		char* value = stackalloc char[100];
		if (!Interop.Globalization.GetLocaleInfoString(localeName, (uint)type, value, 100, uiCultureName))
		{
			return string.Empty;
		}
		return new string(value);
	}

	private int IcuGetLocaleInfo(LocaleNumberData type)
	{
		if (type == LocaleNumberData.CalendarType)
		{
			return 0;
		}
		int value = 0;
		bool localeInfoInt = Interop.Globalization.GetLocaleInfoInt(_sWindowsName, (uint)type, ref value);
		return value;
	}

	private int[] IcuGetLocaleInfo(LocaleGroupingData type)
	{
		int primaryGroupSize = 0;
		int secondaryGroupSize = 0;
		bool localeInfoGroupingSizes = Interop.Globalization.GetLocaleInfoGroupingSizes(_sWindowsName, (uint)type, ref primaryGroupSize, ref secondaryGroupSize);
		if (secondaryGroupSize != 0)
		{
			return new int[2] { primaryGroupSize, secondaryGroupSize };
		}
		return new int[1] { primaryGroupSize };
	}

	private string IcuGetTimeFormatString()
	{
		return IcuGetTimeFormatString(shortFormat: false);
	}

	private unsafe string IcuGetTimeFormatString(bool shortFormat)
	{
		char* ptr = stackalloc char[100];
		if (!Interop.Globalization.GetLocaleTimeFormat(_sWindowsName, shortFormat, ptr, 100))
		{
			return string.Empty;
		}
		ReadOnlySpan<char> span = new ReadOnlySpan<char>(ptr, 100);
		return ConvertIcuTimeFormatString(span.Slice(0, span.IndexOf('\0')));
	}

	private static CultureData IcuGetCultureDataFromRegionName(string regionName)
	{
		return null;
	}

	private string IcuGetLanguageDisplayName(string cultureName)
	{
		return IcuGetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName, CultureInfo.CurrentUICulture.Name);
	}

	private static string IcuGetRegionDisplayName()
	{
		return null;
	}

	internal static bool IcuIsEnsurePredefinedLocaleName(string name)
	{
		return Interop.Globalization.IsPredefinedLocale(name);
	}

	private static string ConvertIcuTimeFormatString(ReadOnlySpan<char> icuFormatString)
	{
		Span<char> span = stackalloc char[157];
		bool flag = false;
		int length = 0;
		for (int i = 0; i < icuFormatString.Length; i++)
		{
			switch (icuFormatString[i])
			{
			case '\'':
				span[length++] = icuFormatString[i++];
				for (; i < icuFormatString.Length; i++)
				{
					char c = icuFormatString[i];
					span[length++] = c;
					if (c == '\'')
					{
						break;
					}
				}
				break;
			case '.':
			case ':':
			case 'H':
			case 'h':
			case 'm':
			case 's':
				span[length++] = icuFormatString[i];
				break;
			case ' ':
			case '\u00a0':
				span[length++] = ' ';
				break;
			case 'a':
				if (!flag)
				{
					flag = true;
					span[length++] = 't';
					span[length++] = 't';
				}
				break;
			}
		}
		return span.Slice(0, length).ToString();
	}

	private static int IcuLocaleNameToLCID(string cultureName)
	{
		int localeDataNumericPart = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.Lcid);
		if (localeDataNumericPart != -1)
		{
			return localeDataNumericPart;
		}
		return 4096;
	}

	private static int IcuGetGeoId(string cultureName)
	{
		int localeDataNumericPart = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.GeoId);
		if (localeDataNumericPart != -1)
		{
			return localeDataNumericPart;
		}
		return Invariant.GeoId;
	}

	private static int IcuGetDigitSubstitution(string cultureName)
	{
		int localeDataNumericPart = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.DigitSubstitutionOrListSeparator);
		if (localeDataNumericPart != -1)
		{
			return (int)((long)localeDataNumericPart & 0xFFFFL);
		}
		return 1;
	}

	private static string IcuGetListSeparator(string cultureName)
	{
		int localeDataNumericPart = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.DigitSubstitutionOrListSeparator);
		if (localeDataNumericPart != -1)
		{
			switch (localeDataNumericPart & 0xFFFF0000u)
			{
			case 0L:
				return ",";
			case 65536L:
				return ";";
			case 131072L:
				return "،";
			case 196608L:
				return "؛";
			case 262144L:
				return ",,";
			}
		}
		return ",";
	}

	private static string IcuGetThreeLetterWindowsLanguageName(string cultureName)
	{
		return IcuLocaleData.GetThreeLetterWindowsLanguageName(cultureName) ?? "ZZZ";
	}

	private static CultureInfo[] IcuEnumCultures(CultureTypes types)
	{
		if ((types & (CultureTypes.NeutralCultures | CultureTypes.SpecificCultures)) == 0)
		{
			return Array.Empty<CultureInfo>();
		}
		int locales = Interop.Globalization.GetLocales(null, 0);
		if (locales <= 0)
		{
			return Array.Empty<CultureInfo>();
		}
		char[] array = new char[locales];
		locales = Interop.Globalization.GetLocales(array, locales);
		if (locales <= 0)
		{
			return Array.Empty<CultureInfo>();
		}
		bool flag = (types & CultureTypes.NeutralCultures) != 0;
		bool flag2 = (types & CultureTypes.SpecificCultures) != 0;
		List<CultureInfo> list = new List<CultureInfo>();
		if (flag)
		{
			list.Add(CultureInfo.InvariantCulture);
		}
		int num;
		for (int i = 0; i < locales; i += num)
		{
			num = array[i++];
			if (i + num <= locales)
			{
				CultureInfo cultureInfo = CultureInfo.GetCultureInfo(new string(array, i, num));
				if ((flag && cultureInfo.IsNeutralCulture) || (flag2 && !cultureInfo.IsNeutralCulture))
				{
					list.Add(cultureInfo);
				}
			}
		}
		return list.ToArray();
	}

	private static string IcuGetConsoleFallbackName(string cultureName)
	{
		return IcuLocaleData.GetConsoleUICulture(cultureName);
	}

	private static bool IsValidCultureName(string subject, out int indexOfUnderscore)
	{
		indexOfUnderscore = -1;
		if (subject.Length == 0)
		{
			return true;
		}
		if (subject.Length == 1 || subject.Length > 85)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < subject.Length; i++)
		{
			char c = subject[i];
			switch (c)
			{
			case '-':
			case '_':
				if (i == 0 || i == subject.Length - 1)
				{
					return false;
				}
				if (subject[i - 1] == '_' || subject[i - 1] == '-')
				{
					return false;
				}
				if (c == '_')
				{
					if (flag)
					{
						return false;
					}
					flag = true;
					indexOfUnderscore = i;
				}
				break;
			default:
				return false;
			case '\0':
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'N':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				break;
			}
		}
		return true;
	}

	internal unsafe static string GetLocaleInfoEx(string localeName, uint field)
	{
		char* ptr = stackalloc char[530];
		int localeInfoEx = GetLocaleInfoEx(localeName, field, ptr, 530);
		if (localeInfoEx > 0)
		{
			return new string(ptr);
		}
		return null;
	}

	internal unsafe static int GetLocaleInfoExInt(string localeName, uint field)
	{
		field |= 0x20000000u;
		int result = 0;
		GetLocaleInfoEx(localeName, field, (char*)(&result), 4);
		return result;
	}

	internal unsafe static int GetLocaleInfoEx(string lpLocaleName, uint lcType, char* lpLCData, int cchData)
	{
		return Interop.Kernel32.GetLocaleInfoEx(lpLocaleName, lcType, lpLCData, cchData);
	}

	private string NlsGetLocaleInfo(LocaleStringData type)
	{
		return NlsGetLocaleInfo(_sWindowsName, type);
	}

	private string NlsGetLocaleInfo(string localeName, LocaleStringData type)
	{
		return GetLocaleInfoFromLCType(localeName, (uint)type, _bUseOverrides);
	}

	private int NlsGetLocaleInfo(LocaleNumberData type)
	{
		uint num = (uint)type;
		if (!_bUseOverrides)
		{
			num |= 0x80000000u;
		}
		return GetLocaleInfoExInt(_sWindowsName, num);
	}

	private int[] NlsGetLocaleInfo(LocaleGroupingData type)
	{
		return ConvertWin32GroupString(GetLocaleInfoFromLCType(_sWindowsName, (uint)type, _bUseOverrides));
	}

	internal static bool NlsIsEnsurePredefinedLocaleName(string name)
	{
		return GetLocaleInfoExInt(name, 125u) != 1;
	}

	private string NlsGetTimeFormatString()
	{
		return ReescapeWin32String(GetLocaleInfoFromLCType(_sWindowsName, 4099u, _bUseOverrides));
	}

	private int NlsGetFirstDayOfWeek()
	{
		int localeInfoExInt = GetLocaleInfoExInt(_sWindowsName, 0x100Cu | ((!_bUseOverrides) ? 2147483648u : 0u));
		return ConvertFirstDayOfWeekMonToSun(localeInfoExInt);
	}

	private unsafe static CultureData NlsGetCultureDataFromRegionName(string regionName)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out EnumLocaleData value);
		value.cultureName = null;
		value.regionName = regionName;
		Interop.Kernel32.EnumSystemLocalesEx((delegate* unmanaged<char*, uint, void*, Interop.BOOL>)(delegate*<char*, uint, void*, Interop.BOOL>)(&EnumSystemLocalesProc), 34u, Unsafe.AsPointer(ref value), IntPtr.Zero);
		if (value.cultureName != null)
		{
			return GetCultureData(value.cultureName, useUserOverride: true);
		}
		return null;
	}

	private string NlsGetLanguageDisplayName(string cultureName)
	{
		CultureInfo userDefaultCulture;
		if (CultureInfo.DefaultThreadCurrentUICulture != null && (userDefaultCulture = CultureInfo.GetUserDefaultCulture()) != null && !CultureInfo.DefaultThreadCurrentUICulture.Name.Equals(userDefaultCulture.Name))
		{
			return NativeName;
		}
		return NlsGetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName);
	}

	private string NlsGetRegionDisplayName()
	{
		if (CultureInfo.CurrentUICulture.Name.Equals(CultureInfo.UserDefaultUICulture.Name))
		{
			return NlsGetLocaleInfo(LocaleStringData.LocalizedCountryName);
		}
		return NativeCountryName;
	}

	private static string GetLocaleInfoFromLCType(string localeName, uint lctype, bool useUserOverride)
	{
		if (!useUserOverride)
		{
			lctype |= 0x80000000u;
		}
		return GetLocaleInfoEx(localeName, lctype) ?? string.Empty;
	}

	[return: NotNullIfNotNull("str")]
	internal static string ReescapeWin32String(string str)
	{
		if (str == null)
		{
			return null;
		}
		StringBuilder stringBuilder = null;
		bool flag = false;
		for (int i = 0; i < str.Length; i++)
		{
			if (str[i] == '\'')
			{
				if (flag)
				{
					if (i + 1 < str.Length && str[i + 1] == '\'')
					{
						if (stringBuilder == null)
						{
							stringBuilder = new StringBuilder(str, 0, i, str.Length * 2);
						}
						stringBuilder.Append("\\'");
						i++;
						continue;
					}
					flag = false;
				}
				else
				{
					flag = true;
				}
			}
			else if (str[i] == '\\')
			{
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(str, 0, i, str.Length * 2);
				}
				stringBuilder.Append("\\\\");
				continue;
			}
			stringBuilder?.Append(str[i]);
		}
		if (stringBuilder == null)
		{
			return str;
		}
		return stringBuilder.ToString();
	}

	[return: NotNullIfNotNull("array")]
	internal static string[] ReescapeWin32Strings(string[] array)
	{
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = ReescapeWin32String(array[i]);
			}
		}
		return array;
	}

	private static int[] ConvertWin32GroupString(string win32Str)
	{
		if (string.IsNullOrEmpty(win32Str))
		{
			return new int[1] { 3 };
		}
		if (win32Str[0] == '0')
		{
			return new int[1];
		}
		int[] array;
		if (win32Str[^1] == '0')
		{
			array = new int[win32Str.Length / 2];
		}
		else
		{
			array = new int[win32Str.Length / 2 + 2];
			array[^1] = 0;
		}
		int num = 0;
		int num2 = 0;
		while (num < win32Str.Length && num2 < array.Length)
		{
			if (win32Str[num] < '1' || win32Str[num] > '9')
			{
				return new int[1] { 3 };
			}
			array[num2] = win32Str[num] - 48;
			num += 2;
			num2++;
		}
		return array;
	}

	private static int ConvertFirstDayOfWeekMonToSun(int iTemp)
	{
		iTemp++;
		if (iTemp > 6)
		{
			iTemp = 0;
		}
		return iTemp;
	}

	[UnmanagedCallersOnly]
	private unsafe static Interop.BOOL EnumSystemLocalesProc(char* lpLocaleString, uint flags, void* contextHandle)
	{
		ref EnumLocaleData reference = ref Unsafe.As<byte, EnumLocaleData>(ref *(byte*)contextHandle);
		try
		{
			string text = new string(lpLocaleString);
			string localeInfoEx = GetLocaleInfoEx(text, 90u);
			if (localeInfoEx != null && localeInfoEx.Equals(reference.regionName, StringComparison.OrdinalIgnoreCase))
			{
				reference.cultureName = text;
				return Interop.BOOL.FALSE;
			}
			return Interop.BOOL.TRUE;
		}
		catch (Exception)
		{
			return Interop.BOOL.FALSE;
		}
	}

	[UnmanagedCallersOnly]
	private unsafe static Interop.BOOL EnumAllSystemLocalesProc(char* lpLocaleString, uint flags, void* contextHandle)
	{
		ref EnumData reference = ref Unsafe.As<byte, EnumData>(ref *(byte*)contextHandle);
		try
		{
			reference.strings.Add(new string(lpLocaleString));
			return Interop.BOOL.TRUE;
		}
		catch (Exception)
		{
			return Interop.BOOL.FALSE;
		}
	}

	[UnmanagedCallersOnly]
	private unsafe static Interop.BOOL EnumTimeCallback(char* lpTimeFormatString, void* lParam)
	{
		ref EnumData reference = ref Unsafe.As<byte, EnumData>(ref *(byte*)lParam);
		try
		{
			reference.strings.Add(new string(lpTimeFormatString));
			return Interop.BOOL.TRUE;
		}
		catch (Exception)
		{
			return Interop.BOOL.FALSE;
		}
	}

	private unsafe static string[] nativeEnumTimeFormats(string localeName, uint dwFlags, bool useUserOverride)
	{
		EnumData value = default(EnumData);
		value.strings = new List<string>();
		Interop.Kernel32.EnumTimeFormatsEx((delegate* unmanaged<char*, void*, Interop.BOOL>)(delegate*<char*, void*, Interop.BOOL>)(&EnumTimeCallback), localeName, dwFlags, Unsafe.AsPointer(ref value));
		if (value.strings.Count > 0)
		{
			string[] array = value.strings.ToArray();
			if (!useUserOverride && value.strings.Count > 1)
			{
				uint lctype = ((dwFlags == 2) ? 121u : 4099u);
				string localeInfoFromLCType = GetLocaleInfoFromLCType(localeName, lctype, useUserOverride);
				if (localeInfoFromLCType != "")
				{
					string text = array[0];
					if (localeInfoFromLCType != text)
					{
						array[0] = array[1];
						array[1] = text;
					}
				}
			}
			return array;
		}
		return null;
	}

	private static int NlsLocaleNameToLCID(string cultureName)
	{
		return Interop.Kernel32.LocaleNameToLCID(cultureName, 134217728u);
	}

	private string NlsGetThreeLetterWindowsLanguageName(string cultureName)
	{
		return NlsGetLocaleInfo(cultureName, LocaleStringData.AbbreviatedWindowsLanguageName);
	}

	private unsafe static CultureInfo[] NlsEnumCultures(CultureTypes types)
	{
		uint num = 0u;
		if ((types & (CultureTypes.InstalledWin32Cultures | CultureTypes.ReplacementCultures | CultureTypes.FrameworkCultures)) != 0)
		{
			num |= 0x30u;
		}
		if ((types & CultureTypes.NeutralCultures) != 0)
		{
			num |= 0x10u;
		}
		if ((types & CultureTypes.SpecificCultures) != 0)
		{
			num |= 0x20u;
		}
		if ((types & CultureTypes.UserCustomCulture) != 0)
		{
			num |= 2u;
		}
		if ((types & CultureTypes.ReplacementCultures) != 0)
		{
			num |= 2u;
		}
		EnumData value = default(EnumData);
		value.strings = new List<string>();
		Interop.Kernel32.EnumSystemLocalesEx((delegate* unmanaged<char*, uint, void*, Interop.BOOL>)(delegate*<char*, uint, void*, Interop.BOOL>)(&EnumAllSystemLocalesProc), num, Unsafe.AsPointer(ref value), IntPtr.Zero);
		CultureInfo[] array = new CultureInfo[value.strings.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new CultureInfo(value.strings[i]);
		}
		return array;
	}

	private string NlsGetConsoleFallbackName(string cultureName)
	{
		return NlsGetLocaleInfo(cultureName, LocaleStringData.ConsoleFallbackName);
	}

	private unsafe bool InitCultureDataCore()
	{
		char* ptr = stackalloc char[85];
		if (!GlobalizationMode.UseNls)
		{
			if (InitIcuCultureDataCore())
			{
				return GetLocaleInfoEx(_sRealName, 92u, ptr, 85) != 0;
			}
			return false;
		}
		string sRealName = _sRealName;
		int localeInfoEx = GetLocaleInfoEx(sRealName, 92u, ptr, 85);
		if (localeInfoEx == 0)
		{
			return false;
		}
		_sRealName = new string(ptr, 0, localeInfoEx - 1);
		sRealName = _sRealName;
		if (GetLocaleInfoEx(sRealName, 536871025u, ptr, 2) == 0)
		{
			return false;
		}
		_bNeutral = *(uint*)ptr != 0;
		_sWindowsName = sRealName;
		if (_bNeutral)
		{
			_sName = sRealName;
			localeInfoEx = Interop.Kernel32.ResolveLocaleName(sRealName, ptr, 85);
			if (localeInfoEx < 1)
			{
				return false;
			}
			_sSpecificCulture = new string(ptr, 0, localeInfoEx - 1);
		}
		else
		{
			_sSpecificCulture = sRealName;
			_sName = sRealName;
			if (GetLocaleInfoEx(sRealName, 536870913u, ptr, 2) == 0)
			{
				return false;
			}
			_iLanguage = *(int*)ptr;
			if (!IsCustomCultureId(_iLanguage))
			{
				int num = sRealName.IndexOf('_');
				if (num > 0)
				{
					_sName = sRealName.Substring(0, num);
				}
			}
		}
		return true;
	}

	private void InitUserOverride(bool useUserOverride)
	{
		_bUseOverrides = useUserOverride && _sWindowsName == CultureInfo.UserDefaultLocaleName;
	}

	internal unsafe static CultureData GetCurrentRegionData()
	{
		Span<char> span = stackalloc char[10];
		int userGeoID = Interop.Kernel32.GetUserGeoID(16);
		if (userGeoID != -1)
		{
			int geoInfo;
			fixed (char* lpGeoData = span)
			{
				geoInfo = Interop.Kernel32.GetGeoInfo(userGeoID, 4, lpGeoData, span.Length, 0);
			}
			if (geoInfo != 0)
			{
				geoInfo -= ((span[geoInfo - 1] == '\0') ? 1 : 0);
				CultureData cultureDataForRegion = GetCultureDataForRegion(span.Slice(0, geoInfo).ToString(), useUserOverride: true);
				if (cultureDataForRegion != null)
				{
					return cultureDataForRegion;
				}
			}
		}
		return CultureInfo.CurrentCulture._cultureData;
	}

	private unsafe static string LCIDToLocaleName(int culture)
	{
		char* ptr = stackalloc char[86];
		int num = Interop.Kernel32.LCIDToLocaleName(culture, ptr, 86, 134217728u);
		if (num > 0)
		{
			return new string(ptr);
		}
		return null;
	}

	private string[] GetTimeFormatsCore(bool shortFormat)
	{
		if (GlobalizationMode.UseNls)
		{
			return ReescapeWin32Strings(nativeEnumTimeFormats(_sWindowsName, shortFormat ? 2u : 0u, _bUseOverrides));
		}
		string text = IcuGetTimeFormatString(shortFormat);
		if (_bUseOverrides)
		{
			string localeInfoFromLCType = GetLocaleInfoFromLCType(_sWindowsName, shortFormat ? 121u : 4099u, useUserOverride: true);
			if (localeInfoFromLCType != text)
			{
				return new string[2] { localeInfoFromLCType, text };
			}
			return new string[1] { localeInfoFromLCType };
		}
		return new string[1] { text };
	}

	private int GetAnsiCodePage(string cultureName)
	{
		return NlsGetLocaleInfo(LocaleNumberData.AnsiCodePage);
	}

	private int GetOemCodePage(string cultureName)
	{
		return NlsGetLocaleInfo(LocaleNumberData.OemCodePage);
	}

	private int GetMacCodePage(string cultureName)
	{
		return NlsGetLocaleInfo(LocaleNumberData.MacCodePage);
	}

	private int GetEbcdicCodePage(string cultureName)
	{
		return NlsGetLocaleInfo(LocaleNumberData.EbcdicCodePage);
	}
}
