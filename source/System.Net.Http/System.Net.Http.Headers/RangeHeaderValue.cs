using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Net.Http.Headers;

public class RangeHeaderValue : ICloneable
{
	private string _unit;

	private ObjectCollection<RangeItemHeaderValue> _ranges;

	public string Unit
	{
		get
		{
			return _unit;
		}
		set
		{
			HeaderUtilities.CheckValidToken(value, "value");
			_unit = value;
		}
	}

	public ICollection<RangeItemHeaderValue> Ranges => _ranges ?? (_ranges = new ObjectCollection<RangeItemHeaderValue>());

	public RangeHeaderValue()
	{
		_unit = "bytes";
	}

	public RangeHeaderValue(long? from, long? to)
	{
		_unit = "bytes";
		Ranges.Add(new RangeItemHeaderValue(from, to));
	}

	private RangeHeaderValue(RangeHeaderValue source)
	{
		_unit = source._unit;
		if (source._ranges == null)
		{
			return;
		}
		foreach (RangeItemHeaderValue range in source._ranges)
		{
			Ranges.Add(new RangeItemHeaderValue(range));
		}
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		stringBuilder.Append(_unit);
		stringBuilder.Append('=');
		if (_ranges != null)
		{
			bool flag = true;
			foreach (RangeItemHeaderValue range in _ranges)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					stringBuilder.Append(", ");
				}
				if (range.From.HasValue)
				{
					stringBuilder.Append(range.From.GetValueOrDefault());
				}
				stringBuilder.Append('-');
				if (range.To.HasValue)
				{
					stringBuilder.Append(range.To.GetValueOrDefault());
				}
			}
		}
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is RangeHeaderValue rangeHeaderValue))
		{
			return false;
		}
		if (string.Equals(_unit, rangeHeaderValue._unit, StringComparison.OrdinalIgnoreCase))
		{
			return HeaderUtilities.AreEqualCollections(_ranges, rangeHeaderValue._ranges);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = StringComparer.OrdinalIgnoreCase.GetHashCode(_unit);
		if (_ranges != null)
		{
			foreach (RangeItemHeaderValue range in _ranges)
			{
				num ^= range.GetHashCode();
			}
		}
		return num;
	}

	public static RangeHeaderValue Parse(string? input)
	{
		int index = 0;
		return (RangeHeaderValue)GenericHeaderParser.RangeParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out RangeHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.RangeParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (RangeHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetRangeLength(string input, int startIndex, out object parsedValue)
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
		RangeHeaderValue rangeHeaderValue = new RangeHeaderValue();
		rangeHeaderValue._unit = input.Substring(startIndex, tokenLength);
		int num = startIndex + tokenLength;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		if (num == input.Length || input[num] != '=')
		{
			return 0;
		}
		num++;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		int rangeItemListLength = RangeItemHeaderValue.GetRangeItemListLength(input, num, rangeHeaderValue.Ranges);
		if (rangeItemListLength == 0)
		{
			return 0;
		}
		num += rangeItemListLength;
		parsedValue = rangeHeaderValue;
		return num - startIndex;
	}

	object ICloneable.Clone()
	{
		return new RangeHeaderValue(this);
	}
}
