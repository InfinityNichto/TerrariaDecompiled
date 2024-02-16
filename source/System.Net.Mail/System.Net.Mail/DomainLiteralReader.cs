using System.Net.Mime;

namespace System.Net.Mail;

internal static class DomainLiteralReader
{
	internal static bool TryReadReverse(string data, int index, out int outIndex, bool throwExceptionIfFail)
	{
		index--;
		do
		{
			if (!WhitespaceReader.TryReadFwsReverse(data, index, out index, throwExceptionIfFail))
			{
				outIndex = 0;
				return false;
			}
			if (index < 0)
			{
				break;
			}
			if (!QuotedPairReader.TryCountQuotedChars(data, index, permitUnicodeEscaping: false, out var outIndex2, throwExceptionIfFail))
			{
				outIndex = 0;
				return false;
			}
			if (outIndex2 > 0)
			{
				index -= outIndex2;
				continue;
			}
			if (data[index] == '[')
			{
				outIndex = index - 1;
				return true;
			}
			if (data[index] > '\u007f' || !MailBnfHelper.Dtext[(uint)data[index]])
			{
				if (throwExceptionIfFail)
				{
					throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data[index]));
				}
				outIndex = 0;
				return false;
			}
			index--;
		}
		while (index >= 0);
		if (throwExceptionIfFail)
		{
			throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, ']'));
		}
		outIndex = 0;
		return false;
	}
}
