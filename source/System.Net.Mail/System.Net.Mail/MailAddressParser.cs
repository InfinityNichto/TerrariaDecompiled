using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail;

internal static class MailAddressParser
{
	internal static bool TryParseAddress(string data, out ParseAddressInfo parsedAddress, bool throwExceptionIfFail)
	{
		int index = data.Length - 1;
		return TryParseAddress(data, expectMultipleAddresses: false, ref index, out parsedAddress, throwExceptionIfFail);
	}

	internal static List<MailAddress> ParseMultipleAddresses(string data)
	{
		List<MailAddress> list = new List<MailAddress>();
		for (int index = data.Length - 1; index >= 0; index--)
		{
			TryParseAddress(data, expectMultipleAddresses: true, ref index, out var parseAddressInfo, throwExceptionIfFail: true);
			list.Insert(0, new MailAddress(parseAddressInfo.DisplayName, parseAddressInfo.User, parseAddressInfo.Host, null));
		}
		return list;
	}

	private static bool TryParseAddress(string data, bool expectMultipleAddresses, ref int index, out ParseAddressInfo parseAddressInfo, bool throwExceptionIfFail)
	{
		if (!TryReadCfwsAndThrowIfIncomplete(data, index, out index, throwExceptionIfFail))
		{
			parseAddressInfo = default(ParseAddressInfo);
			return false;
		}
		bool flag = false;
		if (data[index] == '>')
		{
			flag = true;
			index--;
		}
		if (!TryParseDomain(data, ref index, out var domain, throwExceptionIfFail))
		{
			parseAddressInfo = default(ParseAddressInfo);
			return false;
		}
		if (data[index] != '@')
		{
			if (throwExceptionIfFail)
			{
				throw new FormatException(System.SR.MailAddressInvalidFormat);
			}
			parseAddressInfo = default(ParseAddressInfo);
			return false;
		}
		index--;
		if (!TryParseLocalPart(data, ref index, flag, expectMultipleAddresses, out var localPart, throwExceptionIfFail))
		{
			parseAddressInfo = default(ParseAddressInfo);
			return false;
		}
		if (flag)
		{
			if (index < 0 || data[index] != '<')
			{
				if (throwExceptionIfFail)
				{
					throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, (index >= 0) ? data[index] : '>'));
				}
				parseAddressInfo = default(ParseAddressInfo);
				return false;
			}
			index--;
			if (!WhitespaceReader.TryReadFwsReverse(data, index, out index, throwExceptionIfFail))
			{
				parseAddressInfo = default(ParseAddressInfo);
				return false;
			}
		}
		string displayName;
		if (index >= 0 && (!expectMultipleAddresses || data[index] != ','))
		{
			if (!TryParseDisplayName(data, ref index, expectMultipleAddresses, out displayName, throwExceptionIfFail))
			{
				parseAddressInfo = default(ParseAddressInfo);
				return false;
			}
		}
		else
		{
			displayName = string.Empty;
		}
		parseAddressInfo = new ParseAddressInfo(displayName, localPart, domain);
		return true;
	}

	private static bool TryReadCfwsAndThrowIfIncomplete(string data, int index, out int outIndex, bool throwExceptionIfFail)
	{
		if (!WhitespaceReader.TryReadCfwsReverse(data, index, out index, throwExceptionIfFail))
		{
			outIndex = 0;
			return false;
		}
		if (index < 0)
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

	private static bool TryParseDomain(string data, ref int index, [NotNullWhen(true)] out string domain, bool throwExceptionIfFail)
	{
		if (!TryReadCfwsAndThrowIfIncomplete(data, index, out index, throwExceptionIfFail))
		{
			domain = null;
			return false;
		}
		int num = index;
		if (data[index] == ']')
		{
			if (!DomainLiteralReader.TryReadReverse(data, index, out index, throwExceptionIfFail))
			{
				domain = null;
				return false;
			}
		}
		else if (!DotAtomReader.TryReadReverse(data, index, out index, throwExceptionIfFail))
		{
			domain = null;
			return false;
		}
		domain = data.Substring(index + 1, num - index);
		if (!TryReadCfwsAndThrowIfIncomplete(data, index, out index, throwExceptionIfFail))
		{
			return false;
		}
		if (!TryNormalizeOrThrow(domain, out domain, throwExceptionIfFail))
		{
			return false;
		}
		return true;
	}

	private static bool TryParseLocalPart(string data, ref int index, bool expectAngleBracket, bool expectMultipleAddresses, [NotNullWhen(true)] out string localPart, bool throwExceptionIfFail)
	{
		if (!TryReadCfwsAndThrowIfIncomplete(data, index, out index, throwExceptionIfFail))
		{
			localPart = null;
			return false;
		}
		int num = index;
		if (data[index] == '"')
		{
			if (!QuotedStringFormatReader.TryReadReverseQuoted(data, index, permitUnicode: true, out index, throwExceptionIfFail))
			{
				localPart = null;
				return false;
			}
		}
		else
		{
			if (!DotAtomReader.TryReadReverse(data, index, out index, throwExceptionIfFail))
			{
				localPart = null;
				return false;
			}
			if (index >= 0 && !MailBnfHelper.IsAllowedWhiteSpace(data[index]) && data[index] != ')' && (!expectAngleBracket || data[index] != '<') && (!expectMultipleAddresses || data[index] != ',') && data[index] != '"')
			{
				if (throwExceptionIfFail)
				{
					throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data[index]));
				}
				localPart = null;
				return false;
			}
		}
		localPart = data.Substring(index + 1, num - index);
		if (!WhitespaceReader.TryReadCfwsReverse(data, index, out index, throwExceptionIfFail))
		{
			return false;
		}
		if (!TryNormalizeOrThrow(localPart, out localPart, throwExceptionIfFail))
		{
			return false;
		}
		return true;
	}

	private static bool TryParseDisplayName(string data, ref int index, bool expectMultipleAddresses, [NotNullWhen(true)] out string displayName, bool throwExceptionIfFail)
	{
		if (!WhitespaceReader.TryReadCfwsReverse(data, index, out var outIndex, throwExceptionIfFail))
		{
			displayName = null;
			return false;
		}
		if (outIndex >= 0 && data[outIndex] == '"')
		{
			if (!QuotedStringFormatReader.TryReadReverseQuoted(data, outIndex, permitUnicode: true, out index, throwExceptionIfFail))
			{
				displayName = null;
				return false;
			}
			int num = index + 2;
			displayName = data.Substring(num, outIndex - num);
			if (!WhitespaceReader.TryReadCfwsReverse(data, index, out index, throwExceptionIfFail))
			{
				return false;
			}
			if (index >= 0 && (!expectMultipleAddresses || data[index] != ','))
			{
				if (throwExceptionIfFail)
				{
					throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data[index]));
				}
				return false;
			}
		}
		else
		{
			int num2 = index;
			if (!QuotedStringFormatReader.TryReadReverseUnQuoted(data, index, permitUnicode: true, expectMultipleAddresses, out index, throwExceptionIfFail))
			{
				displayName = null;
				return false;
			}
			displayName = data.SubstringTrim(index + 1, num2 - index);
		}
		if (!TryNormalizeOrThrow(displayName, out displayName, throwExceptionIfFail))
		{
			return false;
		}
		return true;
	}

	internal static bool TryNormalizeOrThrow(string input, [NotNullWhen(true)] out string normalizedString, bool throwExceptionIfFail)
	{
		try
		{
			normalizedString = input.Normalize(NormalizationForm.FormC);
			return true;
		}
		catch (ArgumentException innerException)
		{
			if (throwExceptionIfFail)
			{
				throw new FormatException(System.SR.MailAddressInvalidFormat, innerException);
			}
			normalizedString = null;
			return false;
		}
	}
}
