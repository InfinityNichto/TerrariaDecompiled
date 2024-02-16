using System.Globalization;

namespace System.Net.Http.Headers;

internal sealed class Int64NumberHeaderParser : BaseHeaderParser
{
	internal static readonly Int64NumberHeaderParser Parser = new Int64NumberHeaderParser();

	private Int64NumberHeaderParser()
		: base(supportsMultipleValues: false)
	{
	}

	public override string ToString(object value)
	{
		return ((long)value).ToString(NumberFormatInfo.InvariantInfo);
	}

	protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
	{
		parsedValue = null;
		int numberLength = HttpRuleParser.GetNumberLength(value, startIndex, allowDecimal: false);
		if (numberLength == 0 || numberLength > 19)
		{
			return 0;
		}
		long result = 0L;
		if (!HeaderUtilities.TryParseInt64(value, startIndex, numberLength, out result))
		{
			return 0;
		}
		parsedValue = result;
		return numberLength;
	}
}
