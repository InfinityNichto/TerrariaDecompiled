using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers;

public class WarningHeaderValue : ICloneable
{
	private readonly int _code;

	private readonly string _agent;

	private readonly string _text;

	private readonly DateTimeOffset? _date;

	public int Code => _code;

	public string Agent => _agent;

	public string Text => _text;

	public DateTimeOffset? Date => _date;

	public WarningHeaderValue(int code, string agent, string text)
	{
		CheckCode(code);
		CheckAgent(agent);
		HeaderUtilities.CheckValidQuotedString(text, "text");
		_code = code;
		_agent = agent;
		_text = text;
	}

	public WarningHeaderValue(int code, string agent, string text, DateTimeOffset date)
	{
		CheckCode(code);
		CheckAgent(agent);
		HeaderUtilities.CheckValidQuotedString(text, "text");
		_code = code;
		_agent = agent;
		_text = text;
		_date = date;
	}

	private WarningHeaderValue(WarningHeaderValue source)
	{
		_code = source._code;
		_agent = source._agent;
		_text = source._text;
		_date = source._date;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2, invariantInfo);
		handler.AppendFormatted(_code, "000");
		stringBuilder2.Append(invariantInfo, ref handler);
		stringBuilder.Append(' ');
		stringBuilder.Append(_agent);
		stringBuilder.Append(' ');
		stringBuilder.Append(_text);
		if (_date.HasValue)
		{
			stringBuilder.Append(" \"");
			stringBuilder.Append(HttpDateParser.DateToString(_date.Value));
			stringBuilder.Append('"');
		}
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is WarningHeaderValue warningHeaderValue))
		{
			return false;
		}
		if (_code != warningHeaderValue._code || !string.Equals(_agent, warningHeaderValue._agent, StringComparison.OrdinalIgnoreCase) || !string.Equals(_text, warningHeaderValue._text, StringComparison.Ordinal))
		{
			return false;
		}
		if (_date.HasValue)
		{
			if (warningHeaderValue._date.HasValue)
			{
				return _date.Value == warningHeaderValue._date.Value;
			}
			return false;
		}
		return !warningHeaderValue._date.HasValue;
	}

	public override int GetHashCode()
	{
		int num = _code.GetHashCode() ^ StringComparer.OrdinalIgnoreCase.GetHashCode(_agent) ^ _text.GetHashCode();
		if (_date.HasValue)
		{
			num ^= _date.Value.GetHashCode();
		}
		return num;
	}

	public static WarningHeaderValue Parse(string? input)
	{
		int index = 0;
		return (WarningHeaderValue)GenericHeaderParser.SingleValueWarningParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out WarningHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.SingleValueWarningParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (WarningHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetWarningLength(string input, int startIndex, out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int current = startIndex;
		if (!TryReadCode(input, ref current, out var code))
		{
			return 0;
		}
		if (!TryReadAgent(input, current, ref current, out var agent))
		{
			return 0;
		}
		int length = 0;
		int startIndex2 = current;
		if (HttpRuleParser.GetQuotedStringLength(input, current, out length) != 0)
		{
			return 0;
		}
		string text = input.Substring(startIndex2, length);
		current += length;
		DateTimeOffset? date = null;
		if (!TryReadDate(input, ref current, out date))
		{
			return 0;
		}
		parsedValue = ((!date.HasValue) ? new WarningHeaderValue(code, agent, text) : new WarningHeaderValue(code, agent, text, date.Value));
		return current - startIndex;
	}

	private static bool TryReadAgent(string input, int startIndex, ref int current, [NotNullWhen(true)] out string agent)
	{
		int hostLength = HttpRuleParser.GetHostLength(input, startIndex, allowToken: true, out agent);
		if (hostLength == 0)
		{
			return false;
		}
		current += hostLength;
		int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
		current += whitespaceLength;
		if (whitespaceLength == 0 || current == input.Length)
		{
			return false;
		}
		return true;
	}

	private static bool TryReadCode(string input, ref int current, out int code)
	{
		code = 0;
		int numberLength = HttpRuleParser.GetNumberLength(input, current, allowDecimal: false);
		if (numberLength == 0 || numberLength > 3)
		{
			return false;
		}
		if (!HeaderUtilities.TryParseInt32(input, current, numberLength, out code))
		{
			return false;
		}
		current += numberLength;
		int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
		current += whitespaceLength;
		if (whitespaceLength == 0 || current == input.Length)
		{
			return false;
		}
		return true;
	}

	private static bool TryReadDate(string input, ref int current, out DateTimeOffset? date)
	{
		date = null;
		int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
		current += whitespaceLength;
		if (current < input.Length && input[current] == '"')
		{
			if (whitespaceLength == 0)
			{
				return false;
			}
			current++;
			int num = current;
			while (current < input.Length && input[current] != '"')
			{
				current++;
			}
			if (current == input.Length || current == num)
			{
				return false;
			}
			if (!HttpDateParser.TryParse(input.AsSpan(num, current - num), out var result))
			{
				return false;
			}
			date = result;
			current++;
			current += HttpRuleParser.GetWhitespaceLength(input, current);
		}
		return true;
	}

	object ICloneable.Clone()
	{
		return new WarningHeaderValue(this);
	}

	private static void CheckCode(int code)
	{
		if (code < 0 || code > 999)
		{
			throw new ArgumentOutOfRangeException("code");
		}
	}

	private static void CheckAgent(string agent)
	{
		if (string.IsNullOrEmpty(agent))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, "agent");
		}
		if (HttpRuleParser.GetHostLength(agent, 0, allowToken: true, out var _) != agent.Length)
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, agent));
		}
	}
}
