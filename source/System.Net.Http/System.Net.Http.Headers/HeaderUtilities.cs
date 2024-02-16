using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers;

internal static class HeaderUtilities
{
	internal static readonly TransferCodingHeaderValue TransferEncodingChunked = new TransferCodingHeaderValue("chunked");

	internal static readonly NameValueWithParametersHeaderValue ExpectContinue = new NameValueWithParametersHeaderValue("100-continue");

	internal static readonly Action<HttpHeaderValueCollection<string>, string> TokenValidator = ValidateToken;

	internal static void SetQuality(ObjectCollection<NameValueHeaderValue> parameters, double? value)
	{
		NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(parameters, "q");
		if (value.HasValue)
		{
			if (value < 0.0 || value > 1.0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			string value2 = value.Value.ToString("0.0##", NumberFormatInfo.InvariantInfo);
			if (nameValueHeaderValue != null)
			{
				nameValueHeaderValue.Value = value2;
			}
			else
			{
				parameters.Add(new NameValueHeaderValue("q", value2));
			}
		}
		else if (nameValueHeaderValue != null)
		{
			parameters.Remove(nameValueHeaderValue);
		}
	}

	internal static bool ContainsNonAscii(string input)
	{
		foreach (char c in input)
		{
			if (c > '\u007f')
			{
				return true;
			}
		}
		return false;
	}

	internal static string Encode5987(string input)
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		byte[] array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(input.Length));
		int bytes = Encoding.UTF8.GetBytes(input, 0, input.Length, array, 0);
		stringBuilder.Append("utf-8''");
		for (int i = 0; i < bytes; i++)
		{
			byte b = array[i];
			if (b > 127)
			{
				AddHexEscaped(b, stringBuilder);
			}
			else if (!HttpRuleParser.IsTokenChar((char)b) || b == 42 || b == 39 || b == 37)
			{
				AddHexEscaped(b, stringBuilder);
			}
			else
			{
				stringBuilder.Append((char)b);
			}
		}
		Array.Clear(array, 0, bytes);
		ArrayPool<byte>.Shared.Return(array);
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	private static void AddHexEscaped(byte c, StringBuilder destination)
	{
		destination.Append('%');
		destination.Append(System.HexConverter.ToCharUpper(c >> 4));
		destination.Append(System.HexConverter.ToCharUpper(c));
	}

	internal static double? GetQuality(ObjectCollection<NameValueHeaderValue> parameters)
	{
		NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(parameters, "q");
		if (nameValueHeaderValue != null)
		{
			double result = 0.0;
			if (double.TryParse(nameValueHeaderValue.Value, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out result))
			{
				return result;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, System.SR.Format(System.SR.net_http_log_headers_invalid_quality, nameValueHeaderValue.Value), "GetQuality");
			}
		}
		return null;
	}

	internal static void CheckValidToken(string value, string parameterName)
	{
		if (string.IsNullOrEmpty(value))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, parameterName);
		}
		if (HttpRuleParser.GetTokenLength(value, 0) != value.Length)
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, value));
		}
	}

	internal static void CheckValidComment(string value, string parameterName)
	{
		if (string.IsNullOrEmpty(value))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, parameterName);
		}
		int length = 0;
		if (HttpRuleParser.GetCommentLength(value, 0, out length) != 0 || length != value.Length)
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, value));
		}
	}

	internal static void CheckValidQuotedString(string value, string parameterName)
	{
		if (string.IsNullOrEmpty(value))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, parameterName);
		}
		int length = 0;
		if (HttpRuleParser.GetQuotedStringLength(value, 0, out length) != 0 || length != value.Length)
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, value));
		}
	}

	internal static bool AreEqualCollections<T>(ObjectCollection<T> x, ObjectCollection<T> y) where T : class
	{
		return AreEqualCollections(x, y, null);
	}

	internal static bool AreEqualCollections<T>(ObjectCollection<T> x, ObjectCollection<T> y, IEqualityComparer<T> comparer) where T : class
	{
		if (x == null)
		{
			if (y != null)
			{
				return y.Count == 0;
			}
			return true;
		}
		if (y == null)
		{
			return x.Count == 0;
		}
		if (x.Count != y.Count)
		{
			return false;
		}
		if (x.Count == 0)
		{
			return true;
		}
		bool[] array = new bool[x.Count];
		int num = 0;
		foreach (T item in x)
		{
			num = 0;
			bool flag = false;
			foreach (T item2 in y)
			{
				if (!array[num] && ((comparer == null && item.Equals(item2)) || (comparer != null && comparer.Equals(item, item2))))
				{
					array[num] = true;
					flag = true;
					break;
				}
				num++;
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	internal static int GetNextNonEmptyOrWhitespaceIndex(string input, int startIndex, bool skipEmptyValues, out bool separatorFound)
	{
		separatorFound = false;
		int num = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);
		if (num == input.Length || input[num] != ',')
		{
			return num;
		}
		separatorFound = true;
		num++;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		if (skipEmptyValues)
		{
			while (num < input.Length && input[num] == ',')
			{
				num++;
				num += HttpRuleParser.GetWhitespaceLength(input, num);
			}
		}
		return num;
	}

	internal static DateTimeOffset? GetDateTimeOffsetValue(HeaderDescriptor descriptor, HttpHeaders store, DateTimeOffset? defaultValue = null)
	{
		object parsedValues = store.GetParsedValues(descriptor);
		if (parsedValues != null)
		{
			return (DateTimeOffset)parsedValues;
		}
		if (defaultValue.HasValue && store.Contains(descriptor))
		{
			return defaultValue;
		}
		return null;
	}

	internal static TimeSpan? GetTimeSpanValue(HeaderDescriptor descriptor, HttpHeaders store)
	{
		object parsedValues = store.GetParsedValues(descriptor);
		if (parsedValues != null)
		{
			return (TimeSpan)parsedValues;
		}
		return null;
	}

	internal static bool TryParseInt32(string value, out int result)
	{
		return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);
	}

	internal static bool TryParseInt32(string value, int offset, int length, out int result)
	{
		if (offset < 0 || length < 0 || offset > value.Length - length)
		{
			result = 0;
			return false;
		}
		return int.TryParse(value.AsSpan(offset, length), NumberStyles.None, CultureInfo.InvariantCulture, out result);
	}

	internal static bool TryParseInt64(string value, int offset, int length, out long result)
	{
		if (offset < 0 || length < 0 || offset > value.Length - length)
		{
			result = 0L;
			return false;
		}
		return long.TryParse(value.AsSpan(offset, length), NumberStyles.None, CultureInfo.InvariantCulture, out result);
	}

	internal static void DumpHeaders(StringBuilder sb, params HttpHeaders[] headers)
	{
		sb.AppendLine("{");
		foreach (HttpHeaders httpHeaders in headers)
		{
			if (httpHeaders == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, HeaderStringValues> item in httpHeaders.NonValidated)
			{
				foreach (string item2 in item.Value)
				{
					sb.Append("  ");
					sb.Append(item.Key);
					sb.Append(": ");
					sb.AppendLine(item2);
				}
			}
		}
		sb.Append('}');
	}

	private static void ValidateToken(HttpHeaderValueCollection<string> collection, string value)
	{
		CheckValidToken(value, "item");
	}

	internal static ObjectCollection<NameValueHeaderValue> Clone(this ObjectCollection<NameValueHeaderValue> source)
	{
		if (source == null)
		{
			return null;
		}
		ObjectCollection<NameValueHeaderValue> objectCollection = new ObjectCollection<NameValueHeaderValue>();
		foreach (NameValueHeaderValue item in source)
		{
			objectCollection.Add(new NameValueHeaderValue(item));
		}
		return objectCollection;
	}
}
