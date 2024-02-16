namespace System.Net;

internal static class HttpValidationHelpers
{
	private static readonly char[] s_httpTrimCharacters = new char[6] { '\t', '\n', '\v', '\f', '\r', ' ' };

	internal static bool ContainsNonAsciiChars(string token)
	{
		for (int i = 0; i < token.Length; i++)
		{
			if (token[i] < ' ' || token[i] > '~')
			{
				return true;
			}
		}
		return false;
	}

	internal static bool IsValidToken(string token)
	{
		if (token.Length > 0 && !IsInvalidMethodOrHeaderString(token))
		{
			return !ContainsNonAsciiChars(token);
		}
		return false;
	}

	public static string CheckBadHeaderValueChars(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		value = value.Trim(s_httpTrimCharacters);
		int num = 0;
		for (int i = 0; i < value.Length; i++)
		{
			char c = (char)(0xFFu & value[i]);
			switch (num)
			{
			case 0:
				if (c == '\r')
				{
					num = 1;
				}
				else if (c == '\n')
				{
					num = 2;
				}
				else if (c == '\u007f' || (c < ' ' && c != '\t'))
				{
					throw new ArgumentException(System.SR.net_WebHeaderInvalidControlChars, "value");
				}
				break;
			case 1:
				if (c == '\n')
				{
					num = 2;
					break;
				}
				throw new ArgumentException(System.SR.net_WebHeaderInvalidCRLFChars, "value");
			case 2:
				if (c == ' ' || c == '\t')
				{
					num = 0;
					break;
				}
				throw new ArgumentException(System.SR.net_WebHeaderInvalidControlChars, "value");
			}
		}
		if (num != 0)
		{
			throw new ArgumentException(System.SR.net_WebHeaderInvalidCRLFChars, "value");
		}
		return value;
	}

	public static bool IsInvalidMethodOrHeaderString(string stringValue)
	{
		for (int i = 0; i < stringValue.Length; i++)
		{
			switch (stringValue[i])
			{
			case '\t':
			case '\n':
			case '\r':
			case ' ':
			case '"':
			case '\'':
			case '(':
			case ')':
			case ',':
			case '/':
			case ':':
			case ';':
			case '<':
			case '=':
			case '>':
			case '?':
			case '@':
			case '[':
			case '\\':
			case ']':
			case '{':
			case '}':
				return true;
			}
		}
		return false;
	}
}
