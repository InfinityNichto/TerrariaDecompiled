using System.Text;

namespace System.Net.Http;

internal static class StringBuilderExtensions
{
	private static readonly char[] SpecialCharacters = new char[2] { '"', '\\' };

	public static void AppendKeyValue(this StringBuilder sb, string key, string value, bool includeQuotes = true, bool includeComma = true)
	{
		sb.Append(key);
		sb.Append('=');
		if (includeQuotes)
		{
			sb.Append('"');
			int num = 0;
			while (true)
			{
				int num2 = value.IndexOfAny(SpecialCharacters, num);
				if (num2 < 0)
				{
					break;
				}
				sb.Append(value, num, num2 - num);
				sb.Append('\\');
				sb.Append(value[num2]);
				num = num2 + 1;
			}
			sb.Append(value, num, value.Length - num);
			sb.Append('"');
		}
		else
		{
			sb.Append(value);
		}
		if (includeComma)
		{
			sb.Append(',');
			sb.Append(' ');
		}
	}
}
