namespace System.Web.Util;

internal static class Utf16StringValidator
{
	internal static string ValidateString(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		int num = -1;
		for (int i = 0; i < input.Length; i++)
		{
			if (char.IsSurrogate(input[i]))
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			return input;
		}
		return string.Create(input.Length, (input, num), delegate(Span<char> chars, (string input, int idxOfFirstSurrogate) state)
		{
			state.input.CopyTo(chars);
			for (int j = state.idxOfFirstSurrogate; j < chars.Length; j++)
			{
				char c = chars[j];
				if (char.IsLowSurrogate(c))
				{
					chars[j] = '\ufffd';
				}
				else if (char.IsHighSurrogate(c))
				{
					if (j + 1 < chars.Length && char.IsLowSurrogate(chars[j + 1]))
					{
						j++;
					}
					else
					{
						chars[j] = '\ufffd';
					}
				}
			}
		});
	}
}
