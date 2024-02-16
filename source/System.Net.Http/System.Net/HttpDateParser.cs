using System.Globalization;

namespace System.Net;

internal static class HttpDateParser
{
	private static readonly string[] s_dateFormats = new string[21]
	{
		"ddd, d MMM yyyy H:m:s 'GMT'", "ddd, d MMM yyyy H:m:s 'UTC'", "ddd, d MMM yyyy H:m:s", "d MMM yyyy H:m:s 'GMT'", "d MMM yyyy H:m:s 'UTC'", "d MMM yyyy H:m:s", "ddd, d MMM yy H:m:s 'GMT'", "ddd, d MMM yy H:m:s 'UTC'", "ddd, d MMM yy H:m:s", "d MMM yy H:m:s 'GMT'",
		"d MMM yy H:m:s 'UTC'", "d MMM yy H:m:s", "dddd, d'-'MMM'-'yy H:m:s 'GMT'", "dddd, d'-'MMM'-'yy H:m:s 'UTC'", "dddd, d'-'MMM'-'yy H:m:s zzz", "dddd, d'-'MMM'-'yy H:m:s", "ddd MMM d H:m:s yyyy", "ddd, d MMM yyyy H:m:s zzz", "ddd, d MMM yyyy H:m:s", "d MMM yyyy H:m:s zzz",
		"d MMM yyyy H:m:s"
	};

	internal static bool TryParse(ReadOnlySpan<char> input, out DateTimeOffset result)
	{
		input = input.Trim();
		if (!DateTimeOffset.TryParseExact(input, "r", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out result))
		{
			return DateTimeOffset.TryParseExact(input, s_dateFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowInnerWhite | DateTimeStyles.AssumeUniversal, out result);
		}
		return true;
	}

	internal static string DateToString(DateTimeOffset dateTime)
	{
		return dateTime.ToUniversalTime().ToString("r");
	}
}
