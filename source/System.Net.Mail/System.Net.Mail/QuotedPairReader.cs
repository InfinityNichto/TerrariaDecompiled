namespace System.Net.Mail;

internal static class QuotedPairReader
{
	internal static bool TryCountQuotedChars(string data, int index, bool permitUnicodeEscaping, out int outIndex, bool throwExceptionIfFail)
	{
		if (index <= 0 || data[index - 1] != '\\')
		{
			outIndex = 0;
			return true;
		}
		int num = CountBackslashes(data, index - 1);
		if (num % 2 == 0)
		{
			outIndex = 0;
			return true;
		}
		if (!permitUnicodeEscaping && data[index] > '\u007f')
		{
			if (throwExceptionIfFail)
			{
				throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data[index]));
			}
			outIndex = 0;
			return false;
		}
		outIndex = num + 1;
		return true;
	}

	private static int CountBackslashes(string data, int index)
	{
		int num = 0;
		do
		{
			num++;
			index--;
		}
		while (index >= 0 && data[index] == '\\');
		return num;
	}
}
