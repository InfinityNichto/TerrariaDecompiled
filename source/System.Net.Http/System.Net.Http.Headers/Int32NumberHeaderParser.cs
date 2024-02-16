using System.Globalization;

namespace System.Net.Http.Headers;

internal sealed class Int32NumberHeaderParser : BaseHeaderParser
{
	internal static readonly Int32NumberHeaderParser Parser = new Int32NumberHeaderParser();

	private Int32NumberHeaderParser()
		: base(supportsMultipleValues: false)
	{
	}

	public override string ToString(object value)
	{
		return ((int)value).ToString(NumberFormatInfo.InvariantInfo);
	}

	protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
	{
		parsedValue = null;
		int numberLength = HttpRuleParser.GetNumberLength(value, startIndex, allowDecimal: false);
		if (numberLength == 0 || numberLength > 10)
		{
			return 0;
		}
		int result = 0;
		if (!HeaderUtilities.TryParseInt32(value, startIndex, numberLength, out result))
		{
			return 0;
		}
		parsedValue = result;
		return numberLength;
	}
}
