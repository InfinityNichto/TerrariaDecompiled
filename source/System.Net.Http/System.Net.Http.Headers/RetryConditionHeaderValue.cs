using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Net.Http.Headers;

public class RetryConditionHeaderValue : ICloneable
{
	private readonly DateTimeOffset? _date;

	private readonly TimeSpan? _delta;

	public DateTimeOffset? Date => _date;

	public TimeSpan? Delta => _delta;

	public RetryConditionHeaderValue(DateTimeOffset date)
	{
		_date = date;
	}

	public RetryConditionHeaderValue(TimeSpan delta)
	{
		if (delta.TotalSeconds > 2147483647.0)
		{
			throw new ArgumentOutOfRangeException("delta");
		}
		_delta = delta;
	}

	private RetryConditionHeaderValue(RetryConditionHeaderValue source)
	{
		_delta = source._delta;
		_date = source._date;
	}

	public override string ToString()
	{
		if (_delta.HasValue)
		{
			return ((int)_delta.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
		}
		return HttpDateParser.DateToString(_date.Value);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is RetryConditionHeaderValue retryConditionHeaderValue))
		{
			return false;
		}
		if (_delta.HasValue)
		{
			if (retryConditionHeaderValue._delta.HasValue)
			{
				return _delta.Value == retryConditionHeaderValue._delta.Value;
			}
			return false;
		}
		if (retryConditionHeaderValue._date.HasValue)
		{
			return _date.Value == retryConditionHeaderValue._date.Value;
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (!_delta.HasValue)
		{
			return _date.Value.GetHashCode();
		}
		return _delta.Value.GetHashCode();
	}

	public static RetryConditionHeaderValue Parse(string? input)
	{
		int index = 0;
		return (RetryConditionHeaderValue)GenericHeaderParser.RetryConditionParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out RetryConditionHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.RetryConditionParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (RetryConditionHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetRetryConditionLength(string input, int startIndex, out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int num = startIndex;
		DateTimeOffset result = DateTimeOffset.MinValue;
		int result2 = -1;
		char c = input[num];
		if (c >= '0' && c <= '9')
		{
			int offset = num;
			int numberLength = HttpRuleParser.GetNumberLength(input, num, allowDecimal: false);
			if (numberLength == 0 || numberLength > 10)
			{
				return 0;
			}
			num += numberLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num != input.Length)
			{
				return 0;
			}
			if (!HeaderUtilities.TryParseInt32(input, offset, numberLength, out result2))
			{
				return 0;
			}
		}
		else
		{
			if (!HttpDateParser.TryParse(input.AsSpan(num), out result))
			{
				return 0;
			}
			num = input.Length;
		}
		if (result2 == -1)
		{
			parsedValue = new RetryConditionHeaderValue(result);
		}
		else
		{
			parsedValue = new RetryConditionHeaderValue(new TimeSpan(0, 0, result2));
		}
		return num - startIndex;
	}

	object ICloneable.Clone()
	{
		return new RetryConditionHeaderValue(this);
	}
}
