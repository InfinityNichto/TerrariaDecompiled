using System.Text;

namespace System.Net.Http;

internal static class HttpRuleParser
{
	private static readonly bool[] s_tokenChars = CreateTokenChars();

	internal static Encoding DefaultHttpEncoding => Encoding.Latin1;

	private static bool[] CreateTokenChars()
	{
		bool[] array = new bool[128];
		for (int i = 33; i < 127; i++)
		{
			array[i] = true;
		}
		array[40] = false;
		array[41] = false;
		array[60] = false;
		array[62] = false;
		array[64] = false;
		array[44] = false;
		array[59] = false;
		array[58] = false;
		array[92] = false;
		array[34] = false;
		array[47] = false;
		array[91] = false;
		array[93] = false;
		array[63] = false;
		array[61] = false;
		array[123] = false;
		array[125] = false;
		return array;
	}

	internal static bool IsTokenChar(char character)
	{
		if (character > '\u007f')
		{
			return false;
		}
		return s_tokenChars[(uint)character];
	}

	internal static int GetTokenLength(string input, int startIndex)
	{
		if (startIndex >= input.Length)
		{
			return 0;
		}
		for (int i = startIndex; i < input.Length; i++)
		{
			if (!IsTokenChar(input[i]))
			{
				return i - startIndex;
			}
		}
		return input.Length - startIndex;
	}

	internal static bool IsToken(string input)
	{
		for (int i = 0; i < input.Length; i++)
		{
			if (!IsTokenChar(input[i]))
			{
				return false;
			}
		}
		return true;
	}

	internal static bool IsToken(ReadOnlySpan<byte> input)
	{
		for (int i = 0; i < input.Length; i++)
		{
			if (!IsTokenChar((char)input[i]))
			{
				return false;
			}
		}
		return true;
	}

	internal static string GetTokenString(ReadOnlySpan<byte> input)
	{
		return Encoding.ASCII.GetString(input);
	}

	internal static int GetWhitespaceLength(string input, int startIndex)
	{
		if (startIndex >= input.Length)
		{
			return 0;
		}
		for (int i = startIndex; i < input.Length; i++)
		{
			char c = input[i];
			if (c != ' ' && c != '\t')
			{
				return i - startIndex;
			}
		}
		return input.Length - startIndex;
	}

	internal static bool ContainsNewLine(string value, int startIndex = 0)
	{
		return value.AsSpan(startIndex).IndexOfAny('\r', '\n') != -1;
	}

	internal static int GetNumberLength(string input, int startIndex, bool allowDecimal)
	{
		int num = startIndex;
		bool flag = !allowDecimal;
		if (input[num] == '.')
		{
			return 0;
		}
		while (num < input.Length)
		{
			char c = input[num];
			if (c >= '0' && c <= '9')
			{
				num++;
				continue;
			}
			if (flag || c != '.')
			{
				break;
			}
			flag = true;
			num++;
		}
		return num - startIndex;
	}

	internal static int GetHostLength(string input, int startIndex, bool allowToken, out string host)
	{
		host = null;
		if (startIndex >= input.Length)
		{
			return 0;
		}
		int i = startIndex;
		bool flag;
		bool num;
		for (flag = true; i < input.Length; flag = num, i++)
		{
			char c = input[i];
			switch (c)
			{
			case '/':
				return 0;
			default:
				num = flag && IsTokenChar(c);
				continue;
			case '\t':
			case '\r':
			case ' ':
			case ',':
				break;
			}
			break;
		}
		int num2 = i - startIndex;
		if (num2 == 0)
		{
			return 0;
		}
		string text = input.Substring(startIndex, num2);
		if ((!allowToken || !flag) && !IsValidHostName(text))
		{
			return 0;
		}
		host = text;
		return num2;
	}

	internal static HttpParseResult GetCommentLength(string input, int startIndex, out int length)
	{
		return GetExpressionLength(input, startIndex, '(', ')', supportsNesting: true, 1, out length);
	}

	internal static HttpParseResult GetQuotedStringLength(string input, int startIndex, out int length)
	{
		return GetExpressionLength(input, startIndex, '"', '"', supportsNesting: false, 1, out length);
	}

	internal static HttpParseResult GetQuotedPairLength(string input, int startIndex, out int length)
	{
		length = 0;
		if (input[startIndex] != '\\')
		{
			return HttpParseResult.NotParsed;
		}
		if (startIndex + 2 > input.Length || input[startIndex + 1] > '\u007f')
		{
			return HttpParseResult.InvalidFormat;
		}
		length = 2;
		return HttpParseResult.Parsed;
	}

	private static HttpParseResult GetExpressionLength(string input, int startIndex, char openChar, char closeChar, bool supportsNesting, int nestedCount, out int length)
	{
		length = 0;
		if (input[startIndex] != openChar)
		{
			return HttpParseResult.NotParsed;
		}
		int num = startIndex + 1;
		while (num < input.Length)
		{
			int length2 = 0;
			if (num + 2 < input.Length && GetQuotedPairLength(input, num, out length2) == HttpParseResult.Parsed)
			{
				num += length2;
				continue;
			}
			char c = input[num];
			if (c == '\r' || c == '\n')
			{
				return HttpParseResult.InvalidFormat;
			}
			if (supportsNesting && c == openChar)
			{
				if (nestedCount > 5)
				{
					return HttpParseResult.InvalidFormat;
				}
				int length3 = 0;
				switch (GetExpressionLength(input, num, openChar, closeChar, supportsNesting, nestedCount + 1, out length3))
				{
				case HttpParseResult.Parsed:
					num += length3;
					break;
				case HttpParseResult.InvalidFormat:
					return HttpParseResult.InvalidFormat;
				}
			}
			else
			{
				if (input[num] == closeChar)
				{
					length = num - startIndex + 1;
					return HttpParseResult.Parsed;
				}
				num++;
			}
		}
		return HttpParseResult.InvalidFormat;
	}

	private static bool IsValidHostName(string host)
	{
		Uri result;
		return Uri.TryCreate("http://u@" + host + "/", UriKind.Absolute, out result);
	}
}
