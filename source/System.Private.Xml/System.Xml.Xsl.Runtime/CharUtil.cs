using System.Globalization;

namespace System.Xml.Xsl.Runtime;

internal static class CharUtil
{
	public static bool IsAlphaNumeric(char ch)
	{
		int unicodeCategory = (int)CharUnicodeInfo.GetUnicodeCategory(ch);
		if (unicodeCategory > 4)
		{
			if (unicodeCategory <= 10)
			{
				return unicodeCategory >= 8;
			}
			return false;
		}
		return true;
	}

	public static bool IsDecimalDigitOne(char ch)
	{
		int unicodeCategory = (int)CharUnicodeInfo.GetUnicodeCategory(ch = (char)(ch - 1));
		if (unicodeCategory == 8)
		{
			return char.GetNumericValue(ch) == 0.0;
		}
		return false;
	}
}
