using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Net.Http.Headers;

internal sealed class AltSvcHeaderParser : BaseHeaderParser
{
	public static AltSvcHeaderParser Parser { get; } = new AltSvcHeaderParser();


	private AltSvcHeaderParser()
		: base(supportsMultipleValues: true)
	{
	}

	protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
	{
		if (string.IsNullOrEmpty(value))
		{
			parsedValue = null;
			return 0;
		}
		int num = startIndex;
		if (!TryReadPercentEncodedAlpnProtocolName(value, num, out var result, out var readLength))
		{
			parsedValue = null;
			return 0;
		}
		num += readLength;
		if (result == "clear")
		{
			if (num != value.Length)
			{
				parsedValue = null;
				return 0;
			}
			parsedValue = AltSvcHeaderValue.Clear;
			return num - startIndex;
		}
		if (num == value.Length || value[num++] != '=')
		{
			parsedValue = null;
			return 0;
		}
		if (!TryReadQuotedAltAuthority(value, num, out var host, out var port, out var readLength2))
		{
			parsedValue = null;
			return 0;
		}
		num += readLength2;
		int? num2 = null;
		bool persist = false;
		while (num != value.Length)
		{
			for (; num != value.Length && IsOptionalWhiteSpace(value[num]); num++)
			{
			}
			if (num == value.Length)
			{
				parsedValue = null;
				return 0;
			}
			switch (value[num])
			{
			default:
				parsedValue = null;
				return 0;
			case ';':
			{
				for (num++; num != value.Length && IsOptionalWhiteSpace(value[num]); num++)
				{
				}
				int tokenLength = HttpRuleParser.GetTokenLength(value, num);
				if (tokenLength == 0)
				{
					parsedValue = null;
					return 0;
				}
				if (num + tokenLength >= value.Length || value[num + tokenLength] != '=')
				{
					parsedValue = null;
					return 0;
				}
				if (tokenLength == 2 && value[num] == 'm' && value[num + 1] == 'a')
				{
					num += 3;
					if (!TryReadTokenOrQuotedInt32(value, num, out var result2, out var readLength3))
					{
						parsedValue = null;
						return 0;
					}
					num2 = (num2.HasValue ? new int?(Math.Min(num2.GetValueOrDefault(), result2)) : new int?(result2));
					num += readLength3;
				}
				else if (value.AsSpan(num).StartsWith("persist="))
				{
					num += 8;
					if (TryReadTokenOrQuotedInt32(value, num, out var result3, out var readLength4))
					{
						persist = result3 == 1;
					}
					else if (!TrySkipTokenOrQuoted(value, num, out readLength4))
					{
						parsedValue = null;
						return 0;
					}
					num += readLength4;
				}
				else
				{
					num += tokenLength + 1;
					if (!TrySkipTokenOrQuoted(value, num, out var readLength5))
					{
						parsedValue = null;
						return 0;
					}
					num += readLength5;
				}
				continue;
			}
			case ',':
				break;
			}
			break;
		}
		TimeSpan maxAge = TimeSpan.FromTicks(((long?)num2 * 10000000L) ?? 864000000000L);
		parsedValue = new AltSvcHeaderValue(result, host, port, maxAge, persist);
		return num - startIndex;
	}

	private static bool IsOptionalWhiteSpace(char ch)
	{
		if (ch != ' ')
		{
			return ch == '\t';
		}
		return true;
	}

	private static bool TryReadPercentEncodedAlpnProtocolName(string value, int startIndex, [NotNullWhen(true)] out string result, out int readLength)
	{
		int tokenLength = HttpRuleParser.GetTokenLength(value, startIndex);
		if (tokenLength == 0)
		{
			result = null;
			readLength = 0;
			return false;
		}
		ReadOnlySpan<char> readOnlySpan = value.AsSpan(startIndex, tokenLength);
		readLength = tokenLength;
		switch (readOnlySpan.Length)
		{
		case 2:
			if (readOnlySpan[0] == 'h')
			{
				switch (readOnlySpan[1])
				{
				case '3':
					result = "h3";
					return true;
				case '2':
					result = "h2";
					return true;
				}
			}
			break;
		case 3:
			if (readOnlySpan[0] == 'h' && readOnlySpan[1] == '2' && readOnlySpan[2] == 'c')
			{
				result = "h2c";
				readLength = 3;
				return true;
			}
			break;
		case 5:
			if (readOnlySpan.SequenceEqual("clear"))
			{
				result = "clear";
				return true;
			}
			break;
		case 10:
			if (readOnlySpan.StartsWith("http%2F1."))
			{
				switch (readOnlySpan[9])
				{
				case '1':
					result = "http/1.1";
					return true;
				case '0':
					result = "http/1.0";
					return true;
				}
			}
			break;
		}
		return TryReadUnknownPercentEncodedAlpnProtocolName(readOnlySpan, out result);
	}

	private static bool TryReadUnknownPercentEncodedAlpnProtocolName(ReadOnlySpan<char> value, [NotNullWhen(true)] out string result)
	{
		int num = value.IndexOf('%');
		if (num == -1)
		{
			result = new string(value);
			return true;
		}
		Span<char> initialBuffer = ((value.Length > 128) ? ((Span<char>)new char[value.Length]) : stackalloc char[128]);
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		do
		{
			if (num != 0)
			{
				valueStringBuilder.Append(value.Slice(0, num));
			}
			if (value.Length - num < 3 || !TryReadAlpnHexDigit(value[1], out var nibble) || !TryReadAlpnHexDigit(value[2], out var nibble2))
			{
				result = null;
				return false;
			}
			valueStringBuilder.Append((char)((nibble << 8) | nibble2));
			value = value.Slice(num + 3);
			num = value.IndexOf('%');
		}
		while (num != -1);
		if (value.Length != 0)
		{
			valueStringBuilder.Append(value);
		}
		result = valueStringBuilder.ToString();
		return true;
	}

	private static bool TryReadAlpnHexDigit(char ch, out int nibble)
	{
		int num = System.HexConverter.FromUpperChar(ch);
		if (num == 255)
		{
			nibble = 0;
			return false;
		}
		nibble = num;
		return true;
	}

	private static bool TryReadQuotedAltAuthority(string value, int startIndex, out string host, out int port, out int readLength)
	{
		if (HttpRuleParser.GetQuotedStringLength(value, startIndex, out var length) == HttpParseResult.Parsed)
		{
			ReadOnlySpan<char> span = value.AsSpan(startIndex + 1, length - 2);
			int num = span.IndexOf(':');
			if (num != -1 && TryReadQuotedInt32Value(span.Slice(num + 1), out port))
			{
				if (num == 0)
				{
					host = null;
				}
				else if (!TryReadQuotedValue(span.Slice(0, num), out host))
				{
					goto IL_0056;
				}
				readLength = length;
				return true;
			}
		}
		goto IL_0056;
		IL_0056:
		host = null;
		port = 0;
		readLength = 0;
		return false;
	}

	private static bool TryReadQuotedValue(ReadOnlySpan<char> value, out string result)
	{
		int num = value.IndexOf('\\');
		if (num == -1)
		{
			result = ((value.Length != 0) ? new string(value) : null);
			return true;
		}
		Span<char> initialBuffer = stackalloc char[128];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		do
		{
			if (num + 1 == value.Length)
			{
				valueStringBuilder.Dispose();
				result = null;
				return false;
			}
			if (num != 0)
			{
				valueStringBuilder.Append(value.Slice(0, num));
			}
			valueStringBuilder.Append(value[num + 1]);
			value = value.Slice(num + 2);
			num = value.IndexOf('\\');
		}
		while (num != -1);
		if (value.Length != 0)
		{
			valueStringBuilder.Append(value);
		}
		result = valueStringBuilder.ToString();
		return true;
	}

	private static bool TryReadTokenOrQuotedInt32(string value, int startIndex, out int result, out int readLength)
	{
		if (startIndex >= value.Length)
		{
			result = 0;
			readLength = 0;
			return false;
		}
		if (HttpRuleParser.IsTokenChar(value[startIndex]))
		{
			return HeaderUtilities.TryParseInt32(value, startIndex, readLength = HttpRuleParser.GetTokenLength(value, startIndex), out result);
		}
		if (HttpRuleParser.GetQuotedStringLength(value, startIndex, out var length) == HttpParseResult.Parsed)
		{
			readLength = length;
			return TryReadQuotedInt32Value(value.AsSpan(1, length - 2), out result);
		}
		result = 0;
		readLength = 0;
		return false;
	}

	private static bool TryReadQuotedInt32Value(ReadOnlySpan<char> value, out int result)
	{
		if (value.Length == 0)
		{
			result = 0;
			return false;
		}
		int num = 0;
		ReadOnlySpan<char> readOnlySpan = value;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			switch (c)
			{
			default:
				result = 0;
				return false;
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
			{
				long num2 = (long)num * 10L + (c - 48);
				if (num2 > int.MaxValue)
				{
					result = 0;
					return false;
				}
				num = (int)num2;
				break;
			}
			case '\\':
				break;
			}
		}
		result = num;
		return true;
	}

	private static bool TrySkipTokenOrQuoted(string value, int startIndex, out int readLength)
	{
		if (startIndex >= value.Length)
		{
			readLength = 0;
			return false;
		}
		if (HttpRuleParser.IsTokenChar(value[startIndex]))
		{
			readLength = HttpRuleParser.GetTokenLength(value, startIndex);
			return true;
		}
		if (HttpRuleParser.GetQuotedStringLength(value, startIndex, out var length) == HttpParseResult.Parsed)
		{
			readLength = length;
			return true;
		}
		readLength = 0;
		return false;
	}
}
