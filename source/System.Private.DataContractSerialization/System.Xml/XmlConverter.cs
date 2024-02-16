using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace System.Xml;

internal static class XmlConverter
{
	public const int MaxDateTimeChars = 64;

	public const int MaxInt32Chars = 16;

	public const int MaxInt64Chars = 32;

	public const int MaxBoolChars = 5;

	public const int MaxFloatChars = 16;

	public const int MaxDoubleChars = 32;

	public const int MaxDecimalChars = 40;

	public const int MaxUInt64Chars = 32;

	public const int MaxPrimitiveChars = 64;

	private static UTF8Encoding s_utf8Encoding;

	private static UnicodeEncoding s_unicodeEncoding;

	private static Base64Encoding s_base64Encoding;

	public static Base64Encoding Base64Encoding
	{
		get
		{
			if (s_base64Encoding == null)
			{
				s_base64Encoding = new Base64Encoding();
			}
			return s_base64Encoding;
		}
	}

	private static UTF8Encoding UTF8Encoding
	{
		get
		{
			if (s_utf8Encoding == null)
			{
				s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
			}
			return s_utf8Encoding;
		}
	}

	private static UnicodeEncoding UnicodeEncoding
	{
		get
		{
			if (s_unicodeEncoding == null)
			{
				s_unicodeEncoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
			}
			return s_unicodeEncoding;
		}
	}

	public static bool ToBoolean(string value)
	{
		try
		{
			return XmlConvert.ToBoolean(value);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Boolean", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Boolean", exception2));
		}
	}

	public static bool ToBoolean(byte[] buffer, int offset, int count)
	{
		if (count == 1)
		{
			switch (buffer[offset])
			{
			case 49:
				return true;
			case 48:
				return false;
			}
		}
		return ToBoolean(ToString(buffer, offset, count));
	}

	public static int ToInt32(string value)
	{
		try
		{
			return XmlConvert.ToInt32(value);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Int32", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Int32", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Int32", exception3));
		}
	}

	public static int ToInt32(byte[] buffer, int offset, int count)
	{
		if (TryParseInt32(buffer, offset, count, out var result))
		{
			return result;
		}
		return ToInt32(ToString(buffer, offset, count));
	}

	public static long ToInt64(string value)
	{
		try
		{
			return XmlConvert.ToInt64(value);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Int64", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Int64", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Int64", exception3));
		}
	}

	public static long ToInt64(byte[] buffer, int offset, int count)
	{
		if (TryParseInt64(buffer, offset, count, out var result))
		{
			return result;
		}
		return ToInt64(ToString(buffer, offset, count));
	}

	public static float ToSingle(string value)
	{
		try
		{
			return XmlConvert.ToSingle(value);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "float", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "float", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "float", exception3));
		}
	}

	public static float ToSingle(byte[] buffer, int offset, int count)
	{
		if (TryParseSingle(buffer, offset, count, out var result))
		{
			return result;
		}
		return ToSingle(ToString(buffer, offset, count));
	}

	public static double ToDouble(string value)
	{
		try
		{
			return XmlConvert.ToDouble(value);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "double", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "double", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "double", exception3));
		}
	}

	public static double ToDouble(byte[] buffer, int offset, int count)
	{
		if (TryParseDouble(buffer, offset, count, out var result))
		{
			return result;
		}
		return ToDouble(ToString(buffer, offset, count));
	}

	public static decimal ToDecimal(string value)
	{
		try
		{
			return XmlConvert.ToDecimal(value);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "decimal", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "decimal", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "decimal", exception3));
		}
	}

	public static decimal ToDecimal(byte[] buffer, int offset, int count)
	{
		return ToDecimal(ToString(buffer, offset, count));
	}

	public static DateTime ToDateTime(long value)
	{
		try
		{
			return DateTime.FromBinary(value);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(ToString(value), "DateTime", exception));
		}
	}

	public static DateTime ToDateTime(string value)
	{
		try
		{
			return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "DateTime", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "DateTime", exception2));
		}
	}

	public static DateTime ToDateTime(byte[] buffer, int offset, int count)
	{
		if (TryParseDateTime(buffer, offset, count, out var result))
		{
			return result;
		}
		return ToDateTime(ToString(buffer, offset, count));
	}

	public static UniqueId ToUniqueId(string value)
	{
		try
		{
			return new UniqueId(Trim(value));
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "UniqueId", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "UniqueId", exception2));
		}
	}

	public static UniqueId ToUniqueId(byte[] buffer, int offset, int count)
	{
		return ToUniqueId(ToString(buffer, offset, count));
	}

	public static TimeSpan ToTimeSpan(string value)
	{
		try
		{
			return XmlConvert.ToTimeSpan(value);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "TimeSpan", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "TimeSpan", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "TimeSpan", exception3));
		}
	}

	public static TimeSpan ToTimeSpan(byte[] buffer, int offset, int count)
	{
		return ToTimeSpan(ToString(buffer, offset, count));
	}

	public static Guid ToGuid(string value)
	{
		try
		{
			return new Guid(Trim(value));
		}
		catch (FormatException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Guid", exception));
		}
		catch (ArgumentException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Guid", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "Guid", exception3));
		}
	}

	public static Guid ToGuid(byte[] buffer, int offset, int count)
	{
		return ToGuid(ToString(buffer, offset, count));
	}

	public static ulong ToUInt64(string value)
	{
		try
		{
			return ulong.Parse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "UInt64", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "UInt64", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value, "UInt64", exception3));
		}
	}

	public static ulong ToUInt64(byte[] buffer, int offset, int count)
	{
		return ToUInt64(ToString(buffer, offset, count));
	}

	public static string ToString(byte[] buffer, int offset, int count)
	{
		try
		{
			return UTF8Encoding.GetString(buffer, offset, count);
		}
		catch (DecoderFallbackException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateEncodingException(buffer, offset, count, exception));
		}
	}

	public static string ToStringUnicode(byte[] buffer, int offset, int count)
	{
		try
		{
			return UnicodeEncoding.GetString(buffer, offset, count);
		}
		catch (DecoderFallbackException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateEncodingException(buffer, offset, count, exception));
		}
	}

	public static byte[] ToBytes(string value)
	{
		try
		{
			return UTF8Encoding.GetBytes(value);
		}
		catch (DecoderFallbackException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateEncodingException(value, exception));
		}
	}

	public static int ToChars(byte[] buffer, int offset, int count, char[] chars, int charOffset)
	{
		try
		{
			return UTF8Encoding.GetChars(buffer, offset, count, chars, charOffset);
		}
		catch (DecoderFallbackException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateEncodingException(buffer, offset, count, exception));
		}
	}

	public static string ToString(bool value)
	{
		if (!value)
		{
			return "false";
		}
		return "true";
	}

	public static string ToString(int value)
	{
		return XmlConvert.ToString(value);
	}

	public static string ToString(long value)
	{
		return XmlConvert.ToString(value);
	}

	public static string ToString(float value)
	{
		return XmlConvert.ToString(value);
	}

	public static string ToString(double value)
	{
		return XmlConvert.ToString(value);
	}

	public static string ToString(decimal value)
	{
		return XmlConvert.ToString(value);
	}

	public static string ToString(TimeSpan value)
	{
		return XmlConvert.ToString(value);
	}

	public static string ToString(UniqueId value)
	{
		return value.ToString();
	}

	public static string ToString(Guid value)
	{
		return value.ToString();
	}

	public static string ToString(ulong value)
	{
		return value.ToString(NumberFormatInfo.InvariantInfo);
	}

	public static string ToString(DateTime value)
	{
		byte[] array = new byte[64];
		int num = ToChars(value, array, 0);
		return ToString(array, 0, num);
	}

	private static string ToString(object value)
	{
		if (value is int)
		{
			return ToString((int)value);
		}
		if (value is long)
		{
			return ToString((long)value);
		}
		if (value is float)
		{
			return ToString((float)value);
		}
		if (value is double)
		{
			return ToString((double)value);
		}
		if (value is decimal)
		{
			return ToString((decimal)value);
		}
		if (value is TimeSpan)
		{
			return ToString((TimeSpan)value);
		}
		if (value is UniqueId)
		{
			return ToString((UniqueId)value);
		}
		if (value is Guid)
		{
			return ToString((Guid)value);
		}
		if (value is ulong)
		{
			return ToString((ulong)value);
		}
		if (value is DateTime)
		{
			return ToString((DateTime)value);
		}
		if (value is bool)
		{
			return ToString((bool)value);
		}
		return value.ToString();
	}

	public static string ToString(object[] objects)
	{
		if (objects.Length == 0)
		{
			return string.Empty;
		}
		string text = ToString(objects[0]);
		if (objects.Length > 1)
		{
			StringBuilder stringBuilder = new StringBuilder(text);
			for (int i = 1; i < objects.Length; i++)
			{
				stringBuilder.Append(' ');
				stringBuilder.Append(ToString(objects[i]));
			}
			text = stringBuilder.ToString();
		}
		return text;
	}

	public static void ToQualifiedName(string qname, out string prefix, out string localName)
	{
		int num = qname.IndexOf(':');
		if (num < 0)
		{
			prefix = string.Empty;
			localName = Trim(qname);
			return;
		}
		if (num == qname.Length - 1)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(System.SR.Format(System.SR.XmlInvalidQualifiedName, qname)));
		}
		prefix = Trim(qname.Substring(0, num));
		localName = Trim(qname.Substring(num + 1));
	}

	private static bool TryParseInt32(byte[] chars, int offset, int count, out int result)
	{
		result = 0;
		if (count == 0)
		{
			return false;
		}
		int num = 0;
		int num2 = offset + count;
		if (chars[offset] == 45)
		{
			if (count == 1)
			{
				return false;
			}
			for (int i = offset + 1; i < num2; i++)
			{
				int num3 = chars[i] - 48;
				if ((uint)num3 > 9u)
				{
					return false;
				}
				if (num < -214748364)
				{
					return false;
				}
				num *= 10;
				if (num < int.MinValue + num3)
				{
					return false;
				}
				num -= num3;
			}
		}
		else
		{
			for (int j = offset; j < num2; j++)
			{
				int num4 = chars[j] - 48;
				if ((uint)num4 > 9u)
				{
					return false;
				}
				if (num > 214748364)
				{
					return false;
				}
				num *= 10;
				if (num > int.MaxValue - num4)
				{
					return false;
				}
				num += num4;
			}
		}
		result = num;
		return true;
	}

	private static bool TryParseInt64(byte[] chars, int offset, int count, out long result)
	{
		result = 0L;
		if (count < 11)
		{
			if (!TryParseInt32(chars, offset, count, out var result2))
			{
				return false;
			}
			result = result2;
			return true;
		}
		long num = 0L;
		int num2 = offset + count;
		if (chars[offset] == 45)
		{
			for (int i = offset + 1; i < num2; i++)
			{
				int num3 = chars[i] - 48;
				if ((uint)num3 > 9u)
				{
					return false;
				}
				if (num < -922337203685477580L)
				{
					return false;
				}
				num *= 10;
				if (num < long.MinValue + num3)
				{
					return false;
				}
				num -= num3;
			}
		}
		else
		{
			for (int j = offset; j < num2; j++)
			{
				int num4 = chars[j] - 48;
				if ((uint)num4 > 9u)
				{
					return false;
				}
				if (num > 922337203685477580L)
				{
					return false;
				}
				num *= 10;
				if (num > long.MaxValue - num4)
				{
					return false;
				}
				num += num4;
			}
		}
		result = num;
		return true;
	}

	private static bool TryParseSingle(byte[] chars, int offset, int count, out float result)
	{
		result = 0f;
		int num = offset + count;
		bool flag = false;
		if (offset < num && chars[offset] == 45)
		{
			flag = true;
			offset++;
			count--;
		}
		if (count < 1 || count > 10)
		{
			return false;
		}
		int num2 = 0;
		while (offset < num)
		{
			int num3 = chars[offset] - 48;
			switch (num3)
			{
			case -2:
			{
				offset++;
				int num4 = 1;
				while (offset < num)
				{
					num3 = chars[offset] - 48;
					if ((uint)num3 >= 10u)
					{
						return false;
					}
					num4 *= 10;
					num2 = num2 * 10 + num3;
					offset++;
				}
				if (count > 8)
				{
					result = (float)((double)num2 / (double)num4);
				}
				else
				{
					result = (float)num2 / (float)num4;
				}
				if (flag)
				{
					result = 0f - result;
				}
				return true;
			}
			default:
				return false;
			case 0:
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
				break;
			}
			num2 = num2 * 10 + num3;
			offset++;
		}
		if (count == 10)
		{
			return false;
		}
		if (flag)
		{
			result = -num2;
		}
		else
		{
			result = num2;
		}
		return true;
	}

	private static bool TryParseDouble(byte[] chars, int offset, int count, out double result)
	{
		result = 0.0;
		int num = offset + count;
		bool flag = false;
		if (offset < num && chars[offset] == 45)
		{
			flag = true;
			offset++;
			count--;
		}
		if (count < 1 || count > 10)
		{
			return false;
		}
		int num2 = 0;
		while (offset < num)
		{
			int num3 = chars[offset] - 48;
			switch (num3)
			{
			case -2:
			{
				offset++;
				int num4 = 1;
				while (offset < num)
				{
					num3 = chars[offset] - 48;
					if ((uint)num3 >= 10u)
					{
						return false;
					}
					num4 *= 10;
					num2 = num2 * 10 + num3;
					offset++;
				}
				if (flag)
				{
					result = (0.0 - (double)num2) / (double)num4;
				}
				else
				{
					result = (double)num2 / (double)num4;
				}
				return true;
			}
			default:
				return false;
			case 0:
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
				break;
			}
			num2 = num2 * 10 + num3;
			offset++;
		}
		if (count == 10)
		{
			return false;
		}
		if (flag)
		{
			result = -num2;
		}
		else
		{
			result = num2;
		}
		return true;
	}

	public static int ToChars(int value, byte[] chars, int offset)
	{
		int num = ToCharsR(value, chars, offset + 16);
		Buffer.BlockCopy(chars, offset + 16 - num, chars, offset, num);
		return num;
	}

	public static int ToChars(long value, byte[] chars, int offset)
	{
		int num = ToCharsR(value, chars, offset + 32);
		Buffer.BlockCopy(chars, offset + 32 - num, chars, offset, num);
		return num;
	}

	public static int ToCharsR(long value, byte[] chars, int offset)
	{
		int num = 0;
		if (value >= 0)
		{
			while (value > int.MaxValue)
			{
				long num2 = value / 10;
				num++;
				chars[--offset] = (byte)(48 + (int)(value - num2 * 10));
				value = num2;
			}
		}
		else
		{
			while (value < int.MinValue)
			{
				long num3 = value / 10;
				num++;
				chars[--offset] = (byte)(48 - (int)(value - num3 * 10));
				value = num3;
			}
		}
		return num + ToCharsR((int)value, chars, offset);
	}

	private unsafe static bool IsNegativeZero(float value)
	{
		float num = -0f;
		return *(int*)(&value) == *(int*)(&num);
	}

	private unsafe static bool IsNegativeZero(double value)
	{
		double num = -0.0;
		return *(long*)(&value) == *(long*)(&num);
	}

	private static int ToInfinity(bool isNegative, byte[] buffer, int offset)
	{
		if (isNegative)
		{
			buffer[offset] = 45;
			buffer[offset + 1] = 73;
			buffer[offset + 2] = 78;
			buffer[offset + 3] = 70;
			return 4;
		}
		buffer[offset] = 73;
		buffer[offset + 1] = 78;
		buffer[offset + 2] = 70;
		return 3;
	}

	private static int ToZero(bool isNegative, byte[] buffer, int offset)
	{
		if (isNegative)
		{
			buffer[offset] = 45;
			buffer[offset + 1] = 48;
			return 2;
		}
		buffer[offset] = 48;
		return 1;
	}

	public static int ToChars(double value, byte[] buffer, int offset)
	{
		if (double.IsInfinity(value))
		{
			return ToInfinity(double.IsNegativeInfinity(value), buffer, offset);
		}
		if (value == 0.0)
		{
			return ToZero(IsNegativeZero(value), buffer, offset);
		}
		return ToAsciiChars(value.ToString("R", NumberFormatInfo.InvariantInfo), buffer, offset);
	}

	public static int ToChars(float value, byte[] buffer, int offset)
	{
		if (float.IsInfinity(value))
		{
			return ToInfinity(float.IsNegativeInfinity(value), buffer, offset);
		}
		if ((double)value == 0.0)
		{
			return ToZero(IsNegativeZero(value), buffer, offset);
		}
		return ToAsciiChars(value.ToString("R", NumberFormatInfo.InvariantInfo), buffer, offset);
	}

	public static int ToChars(decimal value, byte[] buffer, int offset)
	{
		return ToAsciiChars(value.ToString(null, NumberFormatInfo.InvariantInfo), buffer, offset);
	}

	public static int ToChars(ulong value, byte[] buffer, int offset)
	{
		return ToAsciiChars(value.ToString(null, NumberFormatInfo.InvariantInfo), buffer, offset);
	}

	private static int ToAsciiChars(string s, byte[] buffer, int offset)
	{
		for (int i = 0; i < s.Length; i++)
		{
			buffer[offset++] = (byte)s[i];
		}
		return s.Length;
	}

	public static int ToChars(bool value, byte[] buffer, int offset)
	{
		if (value)
		{
			buffer[offset] = 116;
			buffer[offset + 1] = 114;
			buffer[offset + 2] = 117;
			buffer[offset + 3] = 101;
			return 4;
		}
		buffer[offset] = 102;
		buffer[offset + 1] = 97;
		buffer[offset + 2] = 108;
		buffer[offset + 3] = 115;
		buffer[offset + 4] = 101;
		return 5;
	}

	private static int ToInt32D2(byte[] chars, int offset)
	{
		byte b = (byte)(chars[offset] - 48);
		byte b2 = (byte)(chars[offset + 1] - 48);
		if (b > 9 || b2 > 9)
		{
			return -1;
		}
		return 10 * b + b2;
	}

	private static int ToInt32D4(byte[] chars, int offset, int count)
	{
		return ToInt32D7(chars, offset, count);
	}

	private static int ToInt32D7(byte[] chars, int offset, int count)
	{
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			byte b = (byte)(chars[offset + i] - 48);
			if (b > 9)
			{
				return -1;
			}
			num = num * 10 + b;
		}
		return num;
	}

	private static bool TryParseDateTime(byte[] chars, int offset, int count, out DateTime result)
	{
		int num = offset + count;
		result = DateTime.MaxValue;
		if (count < 19)
		{
			return false;
		}
		if (chars[offset + 4] != 45 || chars[offset + 7] != 45 || chars[offset + 10] != 84 || chars[offset + 13] != 58 || chars[offset + 16] != 58)
		{
			return false;
		}
		int num2 = ToInt32D4(chars, offset, 4);
		int num3 = ToInt32D2(chars, offset + 5);
		int num4 = ToInt32D2(chars, offset + 8);
		int num5 = ToInt32D2(chars, offset + 11);
		int num6 = ToInt32D2(chars, offset + 14);
		int num7 = ToInt32D2(chars, offset + 17);
		if ((num2 | num3 | num4 | num5 | num6 | num7) < 0)
		{
			return false;
		}
		DateTimeKind kind = DateTimeKind.Unspecified;
		offset += 19;
		int num8 = 0;
		if (offset < num && chars[offset] == 46)
		{
			offset++;
			int num9 = offset;
			while (offset < num)
			{
				byte b = chars[offset];
				if (b < 48 || b > 57)
				{
					break;
				}
				offset++;
			}
			int num10 = offset - num9;
			if (num10 < 1 || num10 > 7)
			{
				return false;
			}
			num8 = ToInt32D7(chars, num9, num10);
			if (num8 < 0)
			{
				return false;
			}
			for (int i = num10; i < 7; i++)
			{
				num8 *= 10;
			}
		}
		bool flag = false;
		int num11 = 0;
		int num12 = 0;
		if (offset < num)
		{
			byte b2 = chars[offset];
			switch (b2)
			{
			case 90:
				offset++;
				kind = DateTimeKind.Utc;
				break;
			case 43:
			case 45:
				offset++;
				if (offset + 5 > num || chars[offset + 2] != 58)
				{
					return false;
				}
				kind = DateTimeKind.Utc;
				flag = true;
				num11 = ToInt32D2(chars, offset);
				num12 = ToInt32D2(chars, offset + 3);
				if ((num11 | num12) < 0)
				{
					return false;
				}
				if (b2 == 43)
				{
					num11 = -num11;
					num12 = -num12;
				}
				offset += 5;
				break;
			}
		}
		if (offset < num)
		{
			return false;
		}
		DateTime dateTime;
		try
		{
			dateTime = new DateTime(num2, num3, num4, num5, num6, num7, kind);
		}
		catch (ArgumentException)
		{
			return false;
		}
		if (num8 > 0)
		{
			dateTime = dateTime.AddTicks(num8);
		}
		if (flag)
		{
			try
			{
				TimeSpan timeSpan = new TimeSpan(num11, num12, 0);
				dateTime = (((num11 < 0 || !(dateTime < DateTime.MaxValue - timeSpan)) && (num11 >= 0 || !(dateTime > DateTime.MinValue - timeSpan))) ? dateTime.ToLocalTime().Add(timeSpan) : dateTime.Add(timeSpan).ToLocalTime());
			}
			catch (ArgumentOutOfRangeException)
			{
				return false;
			}
		}
		result = dateTime;
		return true;
	}

	public static int ToCharsR(int value, byte[] chars, int offset)
	{
		int num = 0;
		if (value >= 0)
		{
			while (value >= 10)
			{
				int num2 = value / 10;
				num++;
				chars[--offset] = (byte)(48 + (value - num2 * 10));
				value = num2;
			}
			chars[--offset] = (byte)(48 + value);
			return num + 1;
		}
		while (value <= -10)
		{
			int num3 = value / 10;
			num++;
			chars[--offset] = (byte)(48 - (value - num3 * 10));
			value = num3;
		}
		chars[--offset] = (byte)(48 - value);
		chars[--offset] = 45;
		return num + 2;
	}

	private static int ToCharsD2(int value, byte[] chars, int offset)
	{
		if (value < 10)
		{
			chars[offset] = 48;
			chars[offset + 1] = (byte)(48 + value);
		}
		else
		{
			int num = value / 10;
			chars[offset] = (byte)(48 + num);
			chars[offset + 1] = (byte)(48 + value - num * 10);
		}
		return 2;
	}

	private static int ToCharsD4(int value, byte[] chars, int offset)
	{
		ToCharsD2(value / 100, chars, offset);
		ToCharsD2(value % 100, chars, offset + 2);
		return 4;
	}

	private static int ToCharsD7(int value, byte[] chars, int offset)
	{
		int num = 7 - ToCharsR(value, chars, offset + 7);
		for (int i = 0; i < num; i++)
		{
			chars[offset + i] = 48;
		}
		int num2 = 7;
		while (num2 > 0 && chars[offset + num2 - 1] == 48)
		{
			num2--;
		}
		return num2;
	}

	public static int ToChars(DateTime value, byte[] chars, int offset)
	{
		int num = offset;
		offset += ToCharsD4(value.Year, chars, offset);
		chars[offset++] = 45;
		offset += ToCharsD2(value.Month, chars, offset);
		chars[offset++] = 45;
		offset += ToCharsD2(value.Day, chars, offset);
		chars[offset++] = 84;
		offset += ToCharsD2(value.Hour, chars, offset);
		chars[offset++] = 58;
		offset += ToCharsD2(value.Minute, chars, offset);
		chars[offset++] = 58;
		offset += ToCharsD2(value.Second, chars, offset);
		int num2 = (int)(value.Ticks % 10000000);
		if (num2 != 0)
		{
			chars[offset++] = 46;
			offset += ToCharsD7(num2, chars, offset);
		}
		switch (value.Kind)
		{
		case DateTimeKind.Local:
		{
			TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(value);
			if (utcOffset.Ticks < 0)
			{
				chars[offset++] = 45;
			}
			else
			{
				chars[offset++] = 43;
			}
			offset += ToCharsD2(Math.Abs(utcOffset.Hours), chars, offset);
			chars[offset++] = 58;
			offset += ToCharsD2(Math.Abs(utcOffset.Minutes), chars, offset);
			break;
		}
		case DateTimeKind.Utc:
			chars[offset++] = 90;
			break;
		default:
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException());
		case DateTimeKind.Unspecified:
			break;
		}
		return offset - num;
	}

	public static bool IsWhitespace(string s)
	{
		for (int i = 0; i < s.Length; i++)
		{
			if (!IsWhitespace(s[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsWhitespace(char ch)
	{
		if (ch <= ' ')
		{
			if (ch != ' ' && ch != '\t' && ch != '\r')
			{
				return ch == '\n';
			}
			return true;
		}
		return false;
	}

	public static string StripWhitespace(string s)
	{
		int num = s.Length;
		for (int i = 0; i < s.Length; i++)
		{
			if (IsWhitespace(s[i]))
			{
				num--;
			}
		}
		if (num == s.Length)
		{
			return s;
		}
		return string.Create(num, s, delegate(Span<char> chars, string s)
		{
			int num2 = 0;
			foreach (char c in s)
			{
				if (!IsWhitespace(c))
				{
					chars[num2++] = c;
				}
			}
		});
	}

	private static string Trim(string s)
	{
		int i;
		for (i = 0; i < s.Length && IsWhitespace(s[i]); i++)
		{
		}
		int num = s.Length;
		while (num > 0 && IsWhitespace(s[num - 1]))
		{
			num--;
		}
		if (i == 0 && num == s.Length)
		{
			return s;
		}
		if (num == 0)
		{
			return string.Empty;
		}
		return s.Substring(i, num - i);
	}
}
