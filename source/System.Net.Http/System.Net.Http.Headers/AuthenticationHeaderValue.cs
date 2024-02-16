using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers;

public class AuthenticationHeaderValue : ICloneable
{
	private readonly string _scheme;

	private readonly string _parameter;

	public string Scheme => _scheme;

	public string? Parameter => _parameter;

	public AuthenticationHeaderValue(string scheme)
		: this(scheme, null)
	{
	}

	public AuthenticationHeaderValue(string scheme, string? parameter)
	{
		HeaderUtilities.CheckValidToken(scheme, "scheme");
		HttpHeaders.CheckContainsNewLine(parameter);
		_scheme = scheme;
		_parameter = parameter;
	}

	private AuthenticationHeaderValue(AuthenticationHeaderValue source)
	{
		_scheme = source._scheme;
		_parameter = source._parameter;
	}

	public override string ToString()
	{
		if (string.IsNullOrEmpty(_parameter))
		{
			return _scheme;
		}
		return _scheme + " " + _parameter;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is AuthenticationHeaderValue authenticationHeaderValue))
		{
			return false;
		}
		if (string.IsNullOrEmpty(_parameter) && string.IsNullOrEmpty(authenticationHeaderValue._parameter))
		{
			return string.Equals(_scheme, authenticationHeaderValue._scheme, StringComparison.OrdinalIgnoreCase);
		}
		if (string.Equals(_scheme, authenticationHeaderValue._scheme, StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(_parameter, authenticationHeaderValue._parameter, StringComparison.Ordinal);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = StringComparer.OrdinalIgnoreCase.GetHashCode(_scheme);
		if (!string.IsNullOrEmpty(_parameter))
		{
			num ^= _parameter.GetHashCode();
		}
		return num;
	}

	public static AuthenticationHeaderValue Parse(string? input)
	{
		int index = 0;
		return (AuthenticationHeaderValue)GenericHeaderParser.SingleValueAuthenticationParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out AuthenticationHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.SingleValueAuthenticationParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (AuthenticationHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetAuthenticationLength(string input, int startIndex, out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
		if (tokenLength == 0)
		{
			return 0;
		}
		string text = null;
		switch (tokenLength)
		{
		case 5:
			text = "Basic";
			break;
		case 6:
			text = "Digest";
			break;
		case 4:
			text = "NTLM";
			break;
		case 9:
			text = "Negotiate";
			break;
		}
		string scheme = ((text != null && string.CompareOrdinal(input, startIndex, text, 0, tokenLength) == 0) ? text : input.Substring(startIndex, tokenLength));
		int num = startIndex + tokenLength;
		int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, num);
		num += whitespaceLength;
		if (num == input.Length || input[num] == ',')
		{
			parsedValue = new AuthenticationHeaderValue(scheme);
			return num - startIndex;
		}
		if (whitespaceLength == 0)
		{
			return 0;
		}
		int num2 = num;
		int parameterEndIndex = num;
		if (!TrySkipFirstBlob(input, ref num, ref parameterEndIndex))
		{
			return 0;
		}
		if (num < input.Length && !TryGetParametersEndIndex(input, ref num, ref parameterEndIndex))
		{
			return 0;
		}
		string parameter = input.Substring(num2, parameterEndIndex - num2 + 1);
		parsedValue = new AuthenticationHeaderValue(scheme, parameter);
		return num - startIndex;
	}

	private static bool TrySkipFirstBlob(string input, ref int current, ref int parameterEndIndex)
	{
		while (current < input.Length && input[current] != ',')
		{
			if (input[current] == '"')
			{
				int length = 0;
				if (HttpRuleParser.GetQuotedStringLength(input, current, out length) != 0)
				{
					return false;
				}
				current += length;
				parameterEndIndex = current - 1;
			}
			else
			{
				int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
				if (whitespaceLength == 0)
				{
					parameterEndIndex = current;
					current++;
				}
				else
				{
					current += whitespaceLength;
				}
			}
		}
		return true;
	}

	private static bool TryGetParametersEndIndex(string input, ref int parseEndIndex, ref int parameterEndIndex)
	{
		int num = parseEndIndex;
		do
		{
			num++;
			bool separatorFound = false;
			num = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, num, skipEmptyValues: true, out separatorFound);
			if (num == input.Length)
			{
				return true;
			}
			int tokenLength = HttpRuleParser.GetTokenLength(input, num);
			if (tokenLength == 0)
			{
				return false;
			}
			num += tokenLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num == input.Length || input[num] != '=')
			{
				return true;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			int valueLength = NameValueHeaderValue.GetValueLength(input, num);
			if (valueLength == 0)
			{
				return false;
			}
			num += valueLength;
			parameterEndIndex = num - 1;
			num = (parseEndIndex = num + HttpRuleParser.GetWhitespaceLength(input, num));
		}
		while (num < input.Length && input[num] == ',');
		return true;
	}

	object ICloneable.Clone()
	{
		return new AuthenticationHeaderValue(this);
	}
}
