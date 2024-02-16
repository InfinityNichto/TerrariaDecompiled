using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel;

public class CultureInfoConverter : TypeConverter
{
	private sealed class CultureComparer : IComparer
	{
		private readonly CultureInfoConverter _converter;

		public CultureComparer(CultureInfoConverter cultureConverter)
		{
			_converter = cultureConverter;
		}

		public int Compare(object item1, object item2)
		{
			if (item1 == null)
			{
				if (item2 == null)
				{
					return 0;
				}
				return -1;
			}
			if (item2 == null)
			{
				return 1;
			}
			string cultureName = _converter.GetCultureName((CultureInfo)item1);
			string cultureName2 = _converter.GetCultureName((CultureInfo)item2);
			CompareInfo compareInfo = CultureInfo.CurrentCulture.CompareInfo;
			return compareInfo.Compare(cultureName, cultureName2, CompareOptions.StringSort);
		}
	}

	private static class CultureInfoMapper
	{
		private static readonly Dictionary<string, string> s_cultureInfoNameMap = CreateMap();

		private static Dictionary<string, string> CreateMap()
		{
			return new Dictionary<string, string>(274)
			{
				{ "Afrikaans", "af" },
				{ "Afrikaans (South Africa)", "af-ZA" },
				{ "Albanian", "sq" },
				{ "Albanian (Albania)", "sq-AL" },
				{ "Alsatian (France)", "gsw-FR" },
				{ "Amharic (Ethiopia)", "am-ET" },
				{ "Arabic", "ar" },
				{ "Arabic (Algeria)", "ar-DZ" },
				{ "Arabic (Bahrain)", "ar-BH" },
				{ "Arabic (Egypt)", "ar-EG" },
				{ "Arabic (Iraq)", "ar-IQ" },
				{ "Arabic (Jordan)", "ar-JO" },
				{ "Arabic (Kuwait)", "ar-KW" },
				{ "Arabic (Lebanon)", "ar-LB" },
				{ "Arabic (Libya)", "ar-LY" },
				{ "Arabic (Morocco)", "ar-MA" },
				{ "Arabic (Oman)", "ar-OM" },
				{ "Arabic (Qatar)", "ar-QA" },
				{ "Arabic (Saudi Arabia)", "ar-SA" },
				{ "Arabic (Syria)", "ar-SY" },
				{ "Arabic (Tunisia)", "ar-TN" },
				{ "Arabic (U.A.E.)", "ar-AE" },
				{ "Arabic (Yemen)", "ar-YE" },
				{ "Armenian", "hy" },
				{ "Armenian (Armenia)", "hy-AM" },
				{ "Assamese (India)", "as-IN" },
				{ "Azeri", "az" },
				{ "Azeri (Cyrillic, Azerbaijan)", "az-Cyrl-AZ" },
				{ "Azeri (Latin, Azerbaijan)", "az-Latn-AZ" },
				{ "Bashkir (Russia)", "ba-RU" },
				{ "Basque", "eu" },
				{ "Basque (Basque)", "eu-ES" },
				{ "Belarusian", "be" },
				{ "Belarusian (Belarus)", "be-BY" },
				{ "Bengali (Bangladesh)", "bn-BD" },
				{ "Bengali (India)", "bn-IN" },
				{ "Bosnian (Cyrillic, Bosnia and Herzegovina)", "bs-Cyrl-BA" },
				{ "Bosnian (Latin, Bosnia and Herzegovina)", "bs-Latn-BA" },
				{ "Breton (France)", "br-FR" },
				{ "Bulgarian", "bg" },
				{ "Bulgarian (Bulgaria)", "bg-BG" },
				{ "Catalan", "ca" },
				{ "Catalan (Catalan)", "ca-ES" },
				{ "Chinese (Hong Kong S.A.R.)", "zh-HK" },
				{ "Chinese (Macao S.A.R.)", "zh-MO" },
				{ "Chinese (People's Republic of China)", "zh-CN" },
				{ "Chinese (Simplified)", "zh-CHS" },
				{ "Chinese (Singapore)", "zh-SG" },
				{ "Chinese (Taiwan)", "zh-TW" },
				{ "Chinese (Traditional)", "zh-CHT" },
				{ "Corsican (France)", "co-FR" },
				{ "Croatian", "hr" },
				{ "Croatian (Croatia)", "hr-HR" },
				{ "Croatian (Latin, Bosnia and Herzegovina)", "hr-BA" },
				{ "Czech", "cs" },
				{ "Czech (Czech Republic)", "cs-CZ" },
				{ "Danish", "da" },
				{ "Danish (Denmark)", "da-DK" },
				{ "Dari (Afghanistan)", "prs-AF" },
				{ "Divehi", "dv" },
				{ "Divehi (Maldives)", "dv-MV" },
				{ "Dutch", "nl" },
				{ "Dutch (Belgium)", "nl-BE" },
				{ "Dutch (Netherlands)", "nl-NL" },
				{ "English", "en" },
				{ "English (Australia)", "en-AU" },
				{ "English (Belize)", "en-BZ" },
				{ "English (Canada)", "en-CA" },
				{ "English (Caribbean)", "en-029" },
				{ "English (India)", "en-IN" },
				{ "English (Ireland)", "en-IE" },
				{ "English (Jamaica)", "en-JM" },
				{ "English (Malaysia)", "en-MY" },
				{ "English (New Zealand)", "en-NZ" },
				{ "English (Republic of the Philippines)", "en-PH" },
				{ "English (Singapore)", "en-SG" },
				{ "English (South Africa)", "en-ZA" },
				{ "English (Trinidad and Tobago)", "en-TT" },
				{ "English (United Kingdom)", "en-GB" },
				{ "English (United States)", "en-US" },
				{ "English (Zimbabwe)", "en-ZW" },
				{ "Estonian", "et" },
				{ "Estonian (Estonia)", "et-EE" },
				{ "Faroese", "fo" },
				{ "Faroese (Faroe Islands)", "fo-FO" },
				{ "Filipino (Philippines)", "fil-PH" },
				{ "Finnish", "fi" },
				{ "Finnish (Finland)", "fi-FI" },
				{ "French", "fr" },
				{ "French (Belgium)", "fr-BE" },
				{ "French (Canada)", "fr-CA" },
				{ "French (France)", "fr-FR" },
				{ "French (Luxembourg)", "fr-LU" },
				{ "French (Principality of Monaco)", "fr-MC" },
				{ "French (Switzerland)", "fr-CH" },
				{ "Frisian (Netherlands)", "fy-NL" },
				{ "Galician", "gl" },
				{ "Galician (Galician)", "gl-ES" },
				{ "Georgian", "ka" },
				{ "Georgian (Georgia)", "ka-GE" },
				{ "German", "de" },
				{ "German (Austria)", "de-AT" },
				{ "German (Germany)", "de-DE" },
				{ "German (Liechtenstein)", "de-LI" },
				{ "German (Luxembourg)", "de-LU" },
				{ "German (Switzerland)", "de-CH" },
				{ "Greek", "el" },
				{ "Greek (Greece)", "el-GR" },
				{ "Greenlandic (Greenland)", "kl-GL" },
				{ "Gujarati", "gu" },
				{ "Gujarati (India)", "gu-IN" },
				{ "Hausa (Latin, Nigeria)", "ha-Latn-NG" },
				{ "Hebrew", "he" },
				{ "Hebrew (Israel)", "he-IL" },
				{ "Hindi", "hi" },
				{ "Hindi (India)", "hi-IN" },
				{ "Hungarian", "hu" },
				{ "Hungarian (Hungary)", "hu-HU" },
				{ "Icelandic", "is" },
				{ "Icelandic (Iceland)", "is-IS" },
				{ "Igbo (Nigeria)", "ig-NG" },
				{ "Indonesian", "id" },
				{ "Indonesian (Indonesia)", "id-ID" },
				{ "Inuktitut (Latin, Canada)", "iu-Latn-CA" },
				{ "Inuktitut (Syllabics, Canada)", "iu-Cans-CA" },
				{ "Invariant Language (Invariant Country)", "" },
				{ "Irish (Ireland)", "ga-IE" },
				{ "isiXhosa (South Africa)", "xh-ZA" },
				{ "isiZulu (South Africa)", "zu-ZA" },
				{ "Italian", "it" },
				{ "Italian (Italy)", "it-IT" },
				{ "Italian (Switzerland)", "it-CH" },
				{ "Japanese", "ja" },
				{ "Japanese (Japan)", "ja-JP" },
				{ "K'iche (Guatemala)", "qut-GT" },
				{ "Kannada", "kn" },
				{ "Kannada (India)", "kn-IN" },
				{ "Kazakh", "kk" },
				{ "Kazakh (Kazakhstan)", "kk-KZ" },
				{ "Khmer (Cambodia)", "km-KH" },
				{ "Kinyarwanda (Rwanda)", "rw-RW" },
				{ "Kiswahili", "sw" },
				{ "Kiswahili (Kenya)", "sw-KE" },
				{ "Konkani", "kok" },
				{ "Konkani (India)", "kok-IN" },
				{ "Korean", "ko" },
				{ "Korean (Korea)", "ko-KR" },
				{ "Kyrgyz", "ky" },
				{ "Kyrgyz (Kyrgyzstan)", "ky-KG" },
				{ "Lao (Lao P.D.R.)", "lo-LA" },
				{ "Latvian", "lv" },
				{ "Latvian (Latvia)", "lv-LV" },
				{ "Lithuanian", "lt" },
				{ "Lithuanian (Lithuania)", "lt-LT" },
				{ "Lower Sorbian (Germany)", "dsb-DE" },
				{ "Luxembourgish (Luxembourg)", "lb-LU" },
				{ "Macedonian", "mk" },
				{ "Macedonian (North Macedonia)", "mk-MK" },
				{ "Malay", "ms" },
				{ "Malay (Brunei Darussalam)", "ms-BN" },
				{ "Malay (Malaysia)", "ms-MY" },
				{ "Malayalam (India)", "ml-IN" },
				{ "Maltese (Malta)", "mt-MT" },
				{ "Maori (New Zealand)", "mi-NZ" },
				{ "Mapudungun (Chile)", "arn-CL" },
				{ "Marathi", "mr" },
				{ "Marathi (India)", "mr-IN" },
				{ "Mohawk (Mohawk)", "moh-CA" },
				{ "Mongolian", "mn" },
				{ "Mongolian (Cyrillic, Mongolia)", "mn-MN" },
				{ "Mongolian (Traditional Mongolian, PRC)", "mn-Mong-CN" },
				{ "Nepali (Nepal)", "ne-NP" },
				{ "Norwegian", "no" },
				{ "Norwegian, Bokmål (Norway)", "nb-NO" },
				{ "Norwegian, Nynorsk (Norway)", "nn-NO" },
				{ "Occitan (France)", "oc-FR" },
				{ "Oriya (India)", "or-IN" },
				{ "Pashto (Afghanistan)", "ps-AF" },
				{ "Persian", "fa" },
				{ "Persian (Iran)", "fa-IR" },
				{ "Polish", "pl" },
				{ "Polish (Poland)", "pl-PL" },
				{ "Portuguese", "pt" },
				{ "Portuguese (Brazil)", "pt-BR" },
				{ "Portuguese (Portugal)", "pt-PT" },
				{ "Punjabi", "pa" },
				{ "Punjabi (India)", "pa-IN" },
				{ "Quechua (Bolivia)", "quz-BO" },
				{ "Quechua (Ecuador)", "quz-EC" },
				{ "Quechua (Peru)", "quz-PE" },
				{ "Romanian", "ro" },
				{ "Romanian (Romania)", "ro-RO" },
				{ "Romansh (Switzerland)", "rm-CH" },
				{ "Russian", "ru" },
				{ "Russian (Russia)", "ru-RU" },
				{ "Sami, Inari (Finland)", "smn-FI" },
				{ "Sami, Lule (Norway)", "smj-NO" },
				{ "Sami, Lule (Sweden)", "smj-SE" },
				{ "Sami, Northern (Finland)", "se-FI" },
				{ "Sami, Northern (Norway)", "se-NO" },
				{ "Sami, Northern (Sweden)", "se-SE" },
				{ "Sami, Skolt (Finland)", "sms-FI" },
				{ "Sami, Southern (Norway)", "sma-NO" },
				{ "Sami, Southern (Sweden)", "sma-SE" },
				{ "Sanskrit", "sa" },
				{ "Sanskrit (India)", "sa-IN" },
				{ "Serbian", "sr" },
				{ "Serbian (Cyrillic, Bosnia and Herzegovina)", "sr-Cyrl-BA" },
				{ "Serbian (Cyrillic, Serbia)", "sr-Cyrl-CS" },
				{ "Serbian (Latin, Bosnia and Herzegovina)", "sr-Latn-BA" },
				{ "Serbian (Latin, Serbia)", "sr-Latn-CS" },
				{ "Sesotho sa Leboa (South Africa)", "nso-ZA" },
				{ "Setswana (South Africa)", "tn-ZA" },
				{ "Sinhala (Sri Lanka)", "si-LK" },
				{ "Slovak", "sk" },
				{ "Slovak (Slovakia)", "sk-SK" },
				{ "Slovenian", "sl" },
				{ "Slovenian (Slovenia)", "sl-SI" },
				{ "Spanish", "es" },
				{ "Spanish (Argentina)", "es-AR" },
				{ "Spanish (Bolivia)", "es-BO" },
				{ "Spanish (Chile)", "es-CL" },
				{ "Spanish (Colombia)", "es-CO" },
				{ "Spanish (Costa Rica)", "es-CR" },
				{ "Spanish (Dominican Republic)", "es-DO" },
				{ "Spanish (Ecuador)", "es-EC" },
				{ "Spanish (El Salvador)", "es-SV" },
				{ "Spanish (Guatemala)", "es-GT" },
				{ "Spanish (Honduras)", "es-HN" },
				{ "Spanish (Mexico)", "es-MX" },
				{ "Spanish (Nicaragua)", "es-NI" },
				{ "Spanish (Panama)", "es-PA" },
				{ "Spanish (Paraguay)", "es-PY" },
				{ "Spanish (Peru)", "es-PE" },
				{ "Spanish (Puerto Rico)", "es-PR" },
				{ "Spanish (Spain)", "es-ES" },
				{ "Spanish (United States)", "es-US" },
				{ "Spanish (Uruguay)", "es-UY" },
				{ "Spanish (Venezuela)", "es-VE" },
				{ "Swedish", "sv" },
				{ "Swedish (Finland)", "sv-FI" },
				{ "Swedish (Sweden)", "sv-SE" },
				{ "Syriac", "syr" },
				{ "Syriac (Syria)", "syr-SY" },
				{ "Tajik (Cyrillic, Tajikistan)", "tg-Cyrl-TJ" },
				{ "Tamazight (Latin, Algeria)", "tzm-Latn-DZ" },
				{ "Tamil", "ta" },
				{ "Tamil (India)", "ta-IN" },
				{ "Tatar", "tt" },
				{ "Tatar (Russia)", "tt-RU" },
				{ "Telugu", "te" },
				{ "Telugu (India)", "te-IN" },
				{ "Thai", "th" },
				{ "Thai (Thailand)", "th-TH" },
				{ "Tibetan (PRC)", "bo-CN" },
				{ "Turkish", "tr" },
				{ "Turkish (Turkey)", "tr-TR" },
				{ "Turkmen (Turkmenistan)", "tk-TM" },
				{ "Uighur (PRC)", "ug-CN" },
				{ "Ukrainian", "uk" },
				{ "Ukrainian (Ukraine)", "uk-UA" },
				{ "Upper Sorbian (Germany)", "hsb-DE" },
				{ "Urdu", "ur" },
				{ "Urdu (Islamic Republic of Pakistan)", "ur-PK" },
				{ "Uzbek", "uz" },
				{ "Uzbek (Cyrillic, Uzbekistan)", "uz-Cyrl-UZ" },
				{ "Uzbek (Latin, Uzbekistan)", "uz-Latn-UZ" },
				{ "Vietnamese", "vi" },
				{ "Vietnamese (Vietnam)", "vi-VN" },
				{ "Welsh (United Kingdom)", "cy-GB" },
				{ "Wolof (Senegal)", "wo-SN" },
				{ "Yakut (Russia)", "sah-RU" },
				{ "Yi (PRC)", "ii-CN" },
				{ "Yoruba (Nigeria)", "yo-NG" }
			};
		}

		public static string GetCultureInfoName(string cultureInfoDisplayName)
		{
			if (!s_cultureInfoNameMap.TryGetValue(cultureInfoDisplayName, out var value))
			{
				return cultureInfoDisplayName;
			}
			return value;
		}
	}

	private StandardValuesCollection _values;

	private string DefaultCultureString => System.SR.CultureInfoConverterDefaultCultureString;

	protected virtual string GetCultureName(CultureInfo culture)
	{
		if (culture == null)
		{
			throw new ArgumentNullException("culture");
		}
		return culture.Name;
	}

	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (!(sourceType == typeof(string)))
		{
			return base.CanConvertFrom(context, sourceType);
		}
		return true;
	}

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		if (!(destinationType == typeof(InstanceDescriptor)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		string text = value as string;
		if (text != null)
		{
			if (GetCultureName(CultureInfo.InvariantCulture).Equals(string.Empty))
			{
				text = CultureInfoMapper.GetCultureInfoName((string)value);
			}
			string b = DefaultCultureString;
			if (culture != null && culture.Equals(CultureInfo.InvariantCulture))
			{
				b = "(Default)";
			}
			CultureInfo cultureInfo = null;
			if (string.IsNullOrEmpty(text) || string.Equals(text, b, StringComparison.Ordinal))
			{
				cultureInfo = CultureInfo.InvariantCulture;
			}
			if (cultureInfo == null)
			{
				foreach (CultureInfo standardValue in GetStandardValues(context))
				{
					if (standardValue != null && string.Equals(GetCultureName(standardValue), text, StringComparison.Ordinal))
					{
						cultureInfo = standardValue;
						break;
					}
				}
			}
			if (cultureInfo == null)
			{
				try
				{
					cultureInfo = new CultureInfo(text);
				}
				catch
				{
				}
			}
			if (cultureInfo == null)
			{
				foreach (CultureInfo value2 in _values)
				{
					if (value2 != null && GetCultureName(value2).StartsWith(text, StringComparison.CurrentCulture))
					{
						cultureInfo = value2;
						break;
					}
				}
			}
			if (cultureInfo == null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.CultureInfoConverterInvalidCulture, (string)value), "value");
			}
			return cultureInfo;
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(string))
		{
			string result = DefaultCultureString;
			if (culture != null && culture.Equals(CultureInfo.InvariantCulture))
			{
				result = "(Default)";
			}
			if (value == null || value == CultureInfo.InvariantCulture)
			{
				return result;
			}
			if (value is CultureInfo culture2)
			{
				return GetCultureName(culture2);
			}
		}
		if (destinationType == typeof(InstanceDescriptor) && value is CultureInfo cultureInfo)
		{
			return new InstanceDescriptor(typeof(CultureInfo).GetConstructor(new Type[1] { typeof(string) }), new object[1] { cultureInfo.Name });
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	[MemberNotNull("_values")]
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
	{
		if (_values == null)
		{
			CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures);
			int num = Array.IndexOf(cultures, CultureInfo.InvariantCulture);
			CultureInfo[] array;
			if (num != -1)
			{
				cultures[num] = null;
				array = new CultureInfo[cultures.Length];
			}
			else
			{
				array = new CultureInfo[cultures.Length + 1];
			}
			Array.Copy(cultures, array, cultures.Length);
			Array.Sort(array, new CultureComparer(this));
			if (array[0] == null)
			{
				array[0] = CultureInfo.InvariantCulture;
			}
			_values = new StandardValuesCollection(array);
		}
		return _values;
	}

	public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)
	{
		return false;
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
	{
		return true;
	}
}
