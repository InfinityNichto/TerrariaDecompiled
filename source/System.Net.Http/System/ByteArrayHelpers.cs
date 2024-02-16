namespace System;

internal static class ByteArrayHelpers
{
	internal static bool EqualsOrdinalAsciiIgnoreCase(string left, ReadOnlySpan<byte> right)
	{
		if (left.Length != right.Length)
		{
			return false;
		}
		for (int i = 0; i < left.Length; i++)
		{
			uint num = left[i];
			uint num2 = right[i];
			if (num - 97 <= 25)
			{
				num -= 32;
			}
			if (num2 - 97 <= 25)
			{
				num2 -= 32;
			}
			if (num != num2)
			{
				return false;
			}
		}
		return true;
	}

	internal static bool EqualsOrdinalAscii(string left, ReadOnlySpan<byte> right)
	{
		if (left.Length != right.Length)
		{
			return false;
		}
		for (int i = 0; i < left.Length; i++)
		{
			if (left[i] != right[i])
			{
				return false;
			}
		}
		return true;
	}
}
