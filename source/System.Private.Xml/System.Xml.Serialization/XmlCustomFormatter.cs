using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Xml.Serialization.Configuration;

namespace System.Xml.Serialization;

internal static class XmlCustomFormatter
{
	private static DateTimeSerializationSection.DateTimeSerializationMode s_mode;

	private static readonly string[] s_allDateTimeFormats = new string[65]
	{
		"yyyy-MM-ddTHH:mm:ss.fffffffzzzzzz", "yyyy", "---dd", "---ddZ", "---ddzzzzzz", "--MM-dd", "--MM-ddZ", "--MM-ddzzzzzz", "--MM--", "--MM--Z",
		"--MM--zzzzzz", "yyyy-MM", "yyyy-MMZ", "yyyy-MMzzzzzz", "yyyyzzzzzz", "yyyy-MM-dd", "yyyy-MM-ddZ", "yyyy-MM-ddzzzzzz", "HH:mm:ss", "HH:mm:ss.f",
		"HH:mm:ss.ff", "HH:mm:ss.fff", "HH:mm:ss.ffff", "HH:mm:ss.fffff", "HH:mm:ss.ffffff", "HH:mm:ss.fffffff", "HH:mm:ssZ", "HH:mm:ss.fZ", "HH:mm:ss.ffZ", "HH:mm:ss.fffZ",
		"HH:mm:ss.ffffZ", "HH:mm:ss.fffffZ", "HH:mm:ss.ffffffZ", "HH:mm:ss.fffffffZ", "HH:mm:sszzzzzz", "HH:mm:ss.fzzzzzz", "HH:mm:ss.ffzzzzzz", "HH:mm:ss.fffzzzzzz", "HH:mm:ss.ffffzzzzzz", "HH:mm:ss.fffffzzzzzz",
		"HH:mm:ss.ffffffzzzzzz", "HH:mm:ss.fffffffzzzzzz", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.f", "yyyy-MM-ddTHH:mm:ss.ff", "yyyy-MM-ddTHH:mm:ss.fff", "yyyy-MM-ddTHH:mm:ss.ffff", "yyyy-MM-ddTHH:mm:ss.fffff", "yyyy-MM-ddTHH:mm:ss.ffffff", "yyyy-MM-ddTHH:mm:ss.fffffff",
		"yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.fZ", "yyyy-MM-ddTHH:mm:ss.ffZ", "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyy-MM-ddTHH:mm:ss.ffffZ", "yyyy-MM-ddTHH:mm:ss.fffffZ", "yyyy-MM-ddTHH:mm:ss.ffffffZ", "yyyy-MM-ddTHH:mm:ss.fffffffZ", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:ss.fzzzzzz",
		"yyyy-MM-ddTHH:mm:ss.ffzzzzzz", "yyyy-MM-ddTHH:mm:ss.fffzzzzzz", "yyyy-MM-ddTHH:mm:ss.ffffzzzzzz", "yyyy-MM-ddTHH:mm:ss.fffffzzzzzz", "yyyy-MM-ddTHH:mm:ss.ffffffzzzzzz"
	};

	private static readonly string[] s_allDateFormats = new string[17]
	{
		"yyyy-MM-ddzzzzzz", "yyyy-MM-dd", "yyyy-MM-ddZ", "yyyy", "---dd", "---ddZ", "---ddzzzzzz", "--MM-dd", "--MM-ddZ", "--MM-ddzzzzzz",
		"--MM--", "--MM--Z", "--MM--zzzzzz", "yyyy-MM", "yyyy-MMZ", "yyyy-MMzzzzzz", "yyyyzzzzzz"
	};

	private static readonly string[] s_allTimeFormats = new string[24]
	{
		"HH:mm:ss.fffffffzzzzzz", "HH:mm:ss", "HH:mm:ss.f", "HH:mm:ss.ff", "HH:mm:ss.fff", "HH:mm:ss.ffff", "HH:mm:ss.fffff", "HH:mm:ss.ffffff", "HH:mm:ss.fffffff", "HH:mm:ssZ",
		"HH:mm:ss.fZ", "HH:mm:ss.ffZ", "HH:mm:ss.fffZ", "HH:mm:ss.ffffZ", "HH:mm:ss.fffffZ", "HH:mm:ss.ffffffZ", "HH:mm:ss.fffffffZ", "HH:mm:sszzzzzz", "HH:mm:ss.fzzzzzz", "HH:mm:ss.ffzzzzzz",
		"HH:mm:ss.fffzzzzzz", "HH:mm:ss.ffffzzzzzz", "HH:mm:ss.fffffzzzzzz", "HH:mm:ss.ffffffzzzzzz"
	};

	private static DateTimeSerializationSection.DateTimeSerializationMode Mode
	{
		get
		{
			if (s_mode == DateTimeSerializationSection.DateTimeSerializationMode.Default)
			{
				s_mode = DateTimeSerializationSection.DateTimeSerializationMode.Roundtrip;
			}
			return s_mode;
		}
	}

	[return: NotNullIfNotNull("value")]
	internal static string FromDefaultValue(object value, string formatter)
	{
		if (value == null)
		{
			return null;
		}
		Type type = value.GetType();
		if (type == typeof(DateTime))
		{
			switch (formatter)
			{
			case "DateTime":
				return FromDateTime((DateTime)value);
			case "Date":
				return FromDate((DateTime)value);
			case "Time":
				return FromTime((DateTime)value);
			}
		}
		else if (type == typeof(string))
		{
			switch (formatter)
			{
			case "XmlName":
				return FromXmlName((string)value);
			case "XmlNCName":
				return FromXmlNCName((string)value);
			case "XmlNmToken":
				return FromXmlNmToken((string)value);
			case "XmlNmTokens":
				return FromXmlNmTokens((string)value);
			}
		}
		throw new XmlException(System.SR.Format(System.SR.XmlUnsupportedDefaultType, type.FullName));
	}

	internal static string FromDate(DateTime value)
	{
		return XmlConvert.ToString(value, "yyyy-MM-dd");
	}

	internal static string FromTime(DateTime value)
	{
		if (!System.LocalAppContextSwitches.IgnoreKindInUtcTimeSerialization && value.Kind == DateTimeKind.Utc)
		{
			return XmlConvert.ToString(DateTime.MinValue + value.TimeOfDay, "HH:mm:ss.fffffffZ");
		}
		return XmlConvert.ToString(DateTime.MinValue + value.TimeOfDay, "HH:mm:ss.fffffffzzzzzz");
	}

	internal static string FromDateTime(DateTime value)
	{
		if (Mode == DateTimeSerializationSection.DateTimeSerializationMode.Local)
		{
			return XmlConvert.ToString(value, "yyyy-MM-ddTHH:mm:ss.fffffffzzzzzz");
		}
		return XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind);
	}

	internal static string FromChar(char value)
	{
		return XmlConvert.ToString((ushort)value);
	}

	[return: NotNullIfNotNull("name")]
	internal static string FromXmlName(string name)
	{
		return XmlConvert.EncodeName(name);
	}

	[return: NotNullIfNotNull("ncName")]
	internal static string FromXmlNCName(string ncName)
	{
		return XmlConvert.EncodeLocalName(ncName);
	}

	[return: NotNullIfNotNull("nmToken")]
	internal static string FromXmlNmToken(string nmToken)
	{
		return XmlConvert.EncodeNmToken(nmToken);
	}

	[return: NotNullIfNotNull("nmTokens")]
	internal static string FromXmlNmTokens(string nmTokens)
	{
		if (nmTokens == null)
		{
			return null;
		}
		if (!nmTokens.Contains(' '))
		{
			return FromXmlNmToken(nmTokens);
		}
		string[] array = nmTokens.Split(' ');
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(FromXmlNmToken(array[i]));
		}
		return stringBuilder.ToString();
	}

	internal static void WriteArrayBase64(XmlWriter writer, byte[] inData, int start, int count)
	{
		if (inData != null && count != 0)
		{
			writer.WriteBase64(inData, start, count);
		}
	}

	[return: NotNullIfNotNull("value")]
	internal static string FromByteArrayHex(byte[] value)
	{
		if (value == null)
		{
			return null;
		}
		if (value.Length == 0)
		{
			return "";
		}
		return XmlConvert.ToBinHexString(value);
	}

	internal static string FromEnum(long val, string[] vals, long[] ids, string typeName)
	{
		long num = val;
		StringBuilder stringBuilder = new StringBuilder();
		int num2 = -1;
		for (int i = 0; i < ids.Length; i++)
		{
			if (ids[i] == 0L)
			{
				num2 = i;
				continue;
			}
			if (val == 0L)
			{
				break;
			}
			if ((ids[i] & num) == ids[i])
			{
				if (stringBuilder.Length != 0)
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append(vals[i]);
				val &= ~ids[i];
			}
		}
		if (val != 0L)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlUnknownConstant, num, (typeName == null) ? "enum" : typeName));
		}
		if (stringBuilder.Length == 0 && num2 >= 0)
		{
			stringBuilder.Append(vals[num2]);
		}
		return stringBuilder.ToString();
	}

	internal static object ToDefaultValue(string value, string formatter)
	{
		return formatter switch
		{
			"DateTime" => ToDateTime(value), 
			"Date" => ToDate(value), 
			"Time" => ToTime(value), 
			"XmlName" => ToXmlName(value), 
			"XmlNCName" => ToXmlNCName(value), 
			"XmlNmToken" => ToXmlNmToken(value), 
			"XmlNmTokens" => ToXmlNmTokens(value), 
			_ => throw new XmlException(System.SR.Format(System.SR.XmlUnsupportedDefaultValue, formatter)), 
		};
	}

	internal static DateTime ToDateTime(string value)
	{
		if (Mode == DateTimeSerializationSection.DateTimeSerializationMode.Local)
		{
			return ToDateTime(value, s_allDateTimeFormats);
		}
		return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);
	}

	internal static DateTime ToDateTime(string value, string[] formats)
	{
		return XmlConvert.ToDateTime(value, formats);
	}

	internal static DateTime ToDate(string value)
	{
		return ToDateTime(value, s_allDateFormats);
	}

	internal static DateTime ToTime(string value)
	{
		if (!System.LocalAppContextSwitches.IgnoreKindInUtcTimeSerialization)
		{
			return DateTime.ParseExact(value, s_allTimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.RoundtripKind);
		}
		return DateTime.ParseExact(value, s_allTimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.NoCurrentDateDefault);
	}

	internal static char ToChar(string value)
	{
		return (char)XmlConvert.ToUInt16(value);
	}

	[return: NotNullIfNotNull("value")]
	internal static string ToXmlName(string value)
	{
		return XmlConvert.DecodeName(CollapseWhitespace(value));
	}

	[return: NotNullIfNotNull("value")]
	internal static string ToXmlNCName(string value)
	{
		return XmlConvert.DecodeName(CollapseWhitespace(value));
	}

	[return: NotNullIfNotNull("value")]
	internal static string ToXmlNmToken(string value)
	{
		return XmlConvert.DecodeName(CollapseWhitespace(value));
	}

	[return: NotNullIfNotNull("value")]
	internal static string ToXmlNmTokens(string value)
	{
		return XmlConvert.DecodeName(CollapseWhitespace(value));
	}

	[return: NotNullIfNotNull("value")]
	internal static byte[] ToByteArrayBase64(string value)
	{
		if (value == null)
		{
			return null;
		}
		value = value.Trim();
		if (value.Length == 0)
		{
			return Array.Empty<byte>();
		}
		return Convert.FromBase64String(value);
	}

	[return: NotNullIfNotNull("value")]
	internal static byte[] ToByteArrayHex(string value)
	{
		if (value == null)
		{
			return null;
		}
		value = value.Trim();
		return XmlConvert.FromBinHexString(value);
	}

	internal static long ToEnum(string val, Hashtable vals, string typeName, bool validate)
	{
		long num = 0L;
		string[] array = val.Split((char[]?)null);
		for (int i = 0; i < array.Length; i++)
		{
			object obj = vals[array[i]];
			if (obj != null)
			{
				num |= (long)obj;
			}
			else if (validate && array[i].Length > 0)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlUnknownConstant, array[i], typeName));
			}
		}
		return num;
	}

	[return: NotNullIfNotNull("value")]
	private static string CollapseWhitespace(string value)
	{
		return value?.Trim();
	}
}
