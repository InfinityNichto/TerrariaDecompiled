namespace System.Text;

internal static class SimpleRegex
{
	public static bool IsMatchWithStarWildcard(ReadOnlySpan<char> input, ReadOnlySpan<char> pattern)
	{
		int num = 0;
		int num2 = -1;
		int i = 0;
		int num3 = -1;
		while (num < input.Length)
		{
			if (i < pattern.Length && pattern[i] == '*')
			{
				num2 = num;
				num3 = ++i;
				continue;
			}
			if (i < pattern.Length && (pattern[i] == input[num] || char.ToUpperInvariant(pattern[i]) == char.ToUpperInvariant(input[num])))
			{
				num++;
				i++;
				continue;
			}
			if (num3 == -1)
			{
				return false;
			}
			num = ++num2;
			i = num3;
		}
		for (; i < pattern.Length && pattern[i] == '*'; i++)
		{
		}
		return i == pattern.Length;
	}
}
