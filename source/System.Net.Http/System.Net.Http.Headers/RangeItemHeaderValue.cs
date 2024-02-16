using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Net.Http.Headers;

public class RangeItemHeaderValue : ICloneable
{
	private readonly long? _from;

	private readonly long? _to;

	public long? From => _from;

	public long? To => _to;

	public RangeItemHeaderValue(long? from, long? to)
	{
		if (!from.HasValue && !to.HasValue)
		{
			throw new ArgumentException(System.SR.net_http_headers_invalid_range);
		}
		if (from.HasValue && from.Value < 0)
		{
			throw new ArgumentOutOfRangeException("from");
		}
		if (to.HasValue && to.Value < 0)
		{
			throw new ArgumentOutOfRangeException("to");
		}
		if (from.HasValue && to.HasValue && from.Value > to.Value)
		{
			throw new ArgumentOutOfRangeException("from");
		}
		_from = from;
		_to = to;
	}

	internal RangeItemHeaderValue(RangeItemHeaderValue source)
	{
		_from = source._from;
		_to = source._to;
	}

	public override string ToString()
	{
		Span<char> span = stackalloc char[128];
		IFormatProvider invariantCulture;
		Span<char> span2;
		if (!_from.HasValue)
		{
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			span2 = span;
			Span<char> initialBuffer = span2;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 1, invariantCulture, span2);
			handler.AppendLiteral("-");
			handler.AppendFormatted(_to.Value);
			return string.Create(provider, initialBuffer, ref handler);
		}
		if (!_to.HasValue)
		{
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			span2 = span;
			Span<char> initialBuffer2 = span2;
			DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(1, 1, invariantCulture, span2);
			handler2.AppendFormatted(_from.Value);
			handler2.AppendLiteral("-");
			return string.Create(provider2, initialBuffer2, ref handler2);
		}
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		span2 = span;
		Span<char> initialBuffer3 = span2;
		DefaultInterpolatedStringHandler handler3 = new DefaultInterpolatedStringHandler(1, 2, invariantCulture, span2);
		handler3.AppendFormatted(_from.Value);
		handler3.AppendLiteral("-");
		handler3.AppendFormatted(_to.Value);
		return string.Create(provider3, initialBuffer3, ref handler3);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is RangeItemHeaderValue rangeItemHeaderValue))
		{
			return false;
		}
		if (_from == rangeItemHeaderValue._from)
		{
			return _to == rangeItemHeaderValue._to;
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (!_from.HasValue)
		{
			return _to.GetHashCode();
		}
		if (!_to.HasValue)
		{
			return _from.GetHashCode();
		}
		return _from.GetHashCode() ^ _to.GetHashCode();
	}

	internal static int GetRangeItemListLength(string input, int startIndex, ICollection<RangeItemHeaderValue> rangeCollection)
	{
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		bool separatorFound = false;
		int nextNonEmptyOrWhitespaceIndex = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, startIndex, skipEmptyValues: true, out separatorFound);
		if (nextNonEmptyOrWhitespaceIndex == input.Length)
		{
			return 0;
		}
		do
		{
			RangeItemHeaderValue parsedValue;
			int rangeItemLength = GetRangeItemLength(input, nextNonEmptyOrWhitespaceIndex, out parsedValue);
			if (rangeItemLength == 0)
			{
				return 0;
			}
			rangeCollection.Add(parsedValue);
			nextNonEmptyOrWhitespaceIndex += rangeItemLength;
			nextNonEmptyOrWhitespaceIndex = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, nextNonEmptyOrWhitespaceIndex, skipEmptyValues: true, out separatorFound);
			if (nextNonEmptyOrWhitespaceIndex < input.Length && !separatorFound)
			{
				return 0;
			}
		}
		while (nextNonEmptyOrWhitespaceIndex != input.Length);
		return nextNonEmptyOrWhitespaceIndex - startIndex;
	}

	internal static int GetRangeItemLength(string input, int startIndex, out RangeItemHeaderValue parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int num = startIndex;
		int offset = num;
		int numberLength = HttpRuleParser.GetNumberLength(input, num, allowDecimal: false);
		if (numberLength > 19)
		{
			return 0;
		}
		num += numberLength;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		if (num == input.Length || input[num] != '-')
		{
			return 0;
		}
		num++;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		int offset2 = num;
		int num2 = 0;
		if (num < input.Length)
		{
			num2 = HttpRuleParser.GetNumberLength(input, num, allowDecimal: false);
			if (num2 > 19)
			{
				return 0;
			}
			num += num2;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
		}
		if (numberLength == 0 && num2 == 0)
		{
			return 0;
		}
		long result = 0L;
		if (numberLength > 0 && !HeaderUtilities.TryParseInt64(input, offset, numberLength, out result))
		{
			return 0;
		}
		long result2 = 0L;
		if (num2 > 0 && !HeaderUtilities.TryParseInt64(input, offset2, num2, out result2))
		{
			return 0;
		}
		if (numberLength > 0 && num2 > 0 && result > result2)
		{
			return 0;
		}
		parsedValue = new RangeItemHeaderValue((numberLength == 0) ? null : new long?(result), (num2 == 0) ? null : new long?(result2));
		return num - startIndex;
	}

	object ICloneable.Clone()
	{
		return new RangeItemHeaderValue(this);
	}
}
