using System.Net.Mime;

namespace System.Net.Mail;

internal static class QuotedStringFormatReader
{
	internal static bool TryReadReverseQuoted(string data, int index, bool permitUnicode, out int outIndex, bool throwExceptionIfFail)
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
			if (!QuotedPairReader.TryCountQuotedChars(data, index, permitUnicode, out var outIndex2, throwExceptionIfFail))
			{
				outIndex = 0;
				return false;
			}
			if (outIndex2 > 0)
			{
				index -= outIndex2;
				continue;
			}
			if (data[index] == '"')
			{
				outIndex = index - 1;
				return true;
			}
			if (!IsValidQtext(permitUnicode, data[index]))
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
			throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, '"'));
		}
		outIndex = 0;
		return false;
	}

	internal static bool TryReadReverseUnQuoted(string data, int index, bool permitUnicode, bool expectCommaDelimiter, out int outIndex, bool throwExceptionIfFail)
	{
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
			if (!QuotedPairReader.TryCountQuotedChars(data, index, permitUnicode, out var outIndex2, throwExceptionIfFail))
			{
				outIndex = 0;
				return false;
			}
			if (outIndex2 > 0)
			{
				index -= outIndex2;
				continue;
			}
			if (expectCommaDelimiter && data[index] == ',')
			{
				break;
			}
			if (!IsValidQtext(permitUnicode, data[index]))
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
		outIndex = index;
		return true;
	}

	private static bool IsValidQtext(bool allowUnicode, char ch)
	{
		if (ch > '\u007f')
		{
			return allowUnicode;
		}
		return MailBnfHelper.Qtext[(uint)ch];
	}
}
