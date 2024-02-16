namespace System.Net.Http.Headers;

internal abstract class BaseHeaderParser : HttpHeaderParser
{
	protected BaseHeaderParser(bool supportsMultipleValues)
		: base(supportsMultipleValues)
	{
	}

	protected abstract int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue);

	public sealed override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(value) || index == value.Length)
		{
			return base.SupportsMultipleValues;
		}
		bool separatorFound = false;
		int nextNonEmptyOrWhitespaceIndex = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, index, base.SupportsMultipleValues, out separatorFound);
		if (separatorFound && !base.SupportsMultipleValues)
		{
			return false;
		}
		if (nextNonEmptyOrWhitespaceIndex == value.Length)
		{
			if (base.SupportsMultipleValues)
			{
				index = nextNonEmptyOrWhitespaceIndex;
			}
			return base.SupportsMultipleValues;
		}
		object parsedValue2;
		int parsedValueLength = GetParsedValueLength(value, nextNonEmptyOrWhitespaceIndex, storeValue, out parsedValue2);
		if (parsedValueLength == 0)
		{
			return false;
		}
		nextNonEmptyOrWhitespaceIndex += parsedValueLength;
		nextNonEmptyOrWhitespaceIndex = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, nextNonEmptyOrWhitespaceIndex, base.SupportsMultipleValues, out separatorFound);
		if ((separatorFound && !base.SupportsMultipleValues) || (!separatorFound && nextNonEmptyOrWhitespaceIndex < value.Length))
		{
			return false;
		}
		index = nextNonEmptyOrWhitespaceIndex;
		parsedValue = parsedValue2;
		return true;
	}
}
