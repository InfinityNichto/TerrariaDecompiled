using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers;

internal sealed class DateHeaderParser : HttpHeaderParser
{
	internal static readonly DateHeaderParser Parser = new DateHeaderParser();

	private DateHeaderParser()
		: base(supportsMultipleValues: false)
	{
	}

	public override string ToString(object value)
	{
		return HttpDateParser.DateToString((DateTimeOffset)value);
	}

	public override bool TryParseValue([NotNullWhen(true)] string value, object storeValue, ref int index, [NotNullWhen(true)] out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(value) || index == value.Length)
		{
			return false;
		}
		ReadOnlySpan<char> input = value;
		if (index > 0)
		{
			input = value.AsSpan(index);
		}
		if (!HttpDateParser.TryParse(input, out var result))
		{
			return false;
		}
		index = value.Length;
		parsedValue = result;
		return true;
	}
}
