using System.Diagnostics.CodeAnalysis;

namespace System.Web.Util;

internal static class HttpEncoderUtility
{
	public static bool IsUrlSafeChar(char ch)
	{
		if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
		{
			return true;
		}
		switch (ch)
		{
		case '!':
		case '(':
		case ')':
		case '*':
		case '-':
		case '.':
		case '_':
			return true;
		default:
			return false;
		}
	}

	[return: NotNullIfNotNull("str")]
	internal static string UrlEncodeSpaces(string str)
	{
		if (str == null || !str.Contains(' '))
		{
			return str;
		}
		return str.Replace(" ", "%20");
	}
}
