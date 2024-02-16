using System.Net.Mime;

namespace System.Net.Mail;

internal static class WhitespaceReader
{
	internal static bool TryReadFwsReverse(string data, int index, out int outIndex, bool throwExceptionIfFail)
	{
		bool flag = false;
		while (index >= 0)
		{
			if (data[index] == '\r' && flag)
			{
				flag = false;
			}
			else
			{
				if (data[index] == '\r' || flag)
				{
					if (throwExceptionIfFail)
					{
						throw new FormatException(System.SR.MailAddressInvalidFormat);
					}
					outIndex = 0;
					return false;
				}
				if (data[index] == '\n')
				{
					flag = true;
				}
				else if (data[index] != ' ' && data[index] != '\t')
				{
					break;
				}
			}
			index--;
		}
		if (flag)
		{
			if (throwExceptionIfFail)
			{
				throw new FormatException(System.SR.MailAddressInvalidFormat);
			}
			outIndex = 0;
			return false;
		}
		outIndex = index;
		return true;
	}

	internal static bool TryReadCfwsReverse(string data, int index, out int outIndex, bool throwExceptionIfFail)
	{
		int num = 0;
		if (!TryReadFwsReverse(data, index, out index, throwExceptionIfFail))
		{
			outIndex = 0;
			return false;
		}
		while (index >= 0)
		{
			if (!QuotedPairReader.TryCountQuotedChars(data, index, permitUnicodeEscaping: true, out var outIndex2, throwExceptionIfFail))
			{
				outIndex = 0;
				return false;
			}
			if (num > 0 && outIndex2 > 0)
			{
				index -= outIndex2;
			}
			else if (data[index] == ')')
			{
				num++;
				index--;
			}
			else if (data[index] == '(')
			{
				num--;
				if (num < 0)
				{
					if (throwExceptionIfFail)
					{
						throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, '('));
					}
					outIndex = 0;
					return false;
				}
				index--;
			}
			else
			{
				if (num <= 0 || (data[index] <= '\u007f' && !MailBnfHelper.Ctext[(uint)data[index]]))
				{
					if (num <= 0)
					{
						break;
					}
					if (throwExceptionIfFail)
					{
						throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data[index]));
					}
					outIndex = 0;
					return false;
				}
				index--;
			}
			if (!TryReadFwsReverse(data, index, out index, throwExceptionIfFail))
			{
				outIndex = 0;
				return false;
			}
		}
		if (num > 0)
		{
			if (throwExceptionIfFail)
			{
				throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, ')'));
			}
			outIndex = 0;
			return false;
		}
		outIndex = index;
		return true;
	}
}
