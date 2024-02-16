using System.Collections;

namespace System.Net.Http.Headers;

internal sealed class GenericHeaderParser : BaseHeaderParser
{
	private delegate int GetParsedValueLengthDelegate(string value, int startIndex, out object parsedValue);

	internal static readonly GenericHeaderParser HostParser = new GenericHeaderParser(supportsMultipleValues: false, ParseHost, StringComparer.OrdinalIgnoreCase);

	internal static readonly GenericHeaderParser TokenListParser = new GenericHeaderParser(supportsMultipleValues: true, ParseTokenList, StringComparer.OrdinalIgnoreCase);

	internal static readonly GenericHeaderParser SingleValueNameValueWithParametersParser = new GenericHeaderParser(supportsMultipleValues: false, NameValueWithParametersHeaderValue.GetNameValueWithParametersLength);

	internal static readonly GenericHeaderParser MultipleValueNameValueWithParametersParser = new GenericHeaderParser(supportsMultipleValues: true, NameValueWithParametersHeaderValue.GetNameValueWithParametersLength);

	internal static readonly GenericHeaderParser SingleValueNameValueParser = new GenericHeaderParser(supportsMultipleValues: false, ParseNameValue);

	internal static readonly GenericHeaderParser MultipleValueNameValueParser = new GenericHeaderParser(supportsMultipleValues: true, ParseNameValue);

	internal static readonly GenericHeaderParser SingleValueParserWithoutValidation = new GenericHeaderParser(supportsMultipleValues: false, ParseWithoutValidation);

	internal static readonly GenericHeaderParser SingleValueProductParser = new GenericHeaderParser(supportsMultipleValues: false, ParseProduct);

	internal static readonly GenericHeaderParser MultipleValueProductParser = new GenericHeaderParser(supportsMultipleValues: true, ParseProduct);

	internal static readonly GenericHeaderParser RangeConditionParser = new GenericHeaderParser(supportsMultipleValues: false, RangeConditionHeaderValue.GetRangeConditionLength);

	internal static readonly GenericHeaderParser SingleValueAuthenticationParser = new GenericHeaderParser(supportsMultipleValues: false, AuthenticationHeaderValue.GetAuthenticationLength);

	internal static readonly GenericHeaderParser MultipleValueAuthenticationParser = new GenericHeaderParser(supportsMultipleValues: true, AuthenticationHeaderValue.GetAuthenticationLength);

	internal static readonly GenericHeaderParser RangeParser = new GenericHeaderParser(supportsMultipleValues: false, RangeHeaderValue.GetRangeLength);

	internal static readonly GenericHeaderParser RetryConditionParser = new GenericHeaderParser(supportsMultipleValues: false, RetryConditionHeaderValue.GetRetryConditionLength);

	internal static readonly GenericHeaderParser ContentRangeParser = new GenericHeaderParser(supportsMultipleValues: false, ContentRangeHeaderValue.GetContentRangeLength);

	internal static readonly GenericHeaderParser ContentDispositionParser = new GenericHeaderParser(supportsMultipleValues: false, ContentDispositionHeaderValue.GetDispositionTypeLength);

	internal static readonly GenericHeaderParser SingleValueStringWithQualityParser = new GenericHeaderParser(supportsMultipleValues: false, StringWithQualityHeaderValue.GetStringWithQualityLength);

	internal static readonly GenericHeaderParser MultipleValueStringWithQualityParser = new GenericHeaderParser(supportsMultipleValues: true, StringWithQualityHeaderValue.GetStringWithQualityLength);

	internal static readonly GenericHeaderParser SingleValueEntityTagParser = new GenericHeaderParser(supportsMultipleValues: false, ParseSingleEntityTag);

	internal static readonly GenericHeaderParser MultipleValueEntityTagParser = new GenericHeaderParser(supportsMultipleValues: true, ParseMultipleEntityTags);

	internal static readonly GenericHeaderParser SingleValueViaParser = new GenericHeaderParser(supportsMultipleValues: false, ViaHeaderValue.GetViaLength);

	internal static readonly GenericHeaderParser MultipleValueViaParser = new GenericHeaderParser(supportsMultipleValues: true, ViaHeaderValue.GetViaLength);

	internal static readonly GenericHeaderParser SingleValueWarningParser = new GenericHeaderParser(supportsMultipleValues: false, WarningHeaderValue.GetWarningLength);

	internal static readonly GenericHeaderParser MultipleValueWarningParser = new GenericHeaderParser(supportsMultipleValues: true, WarningHeaderValue.GetWarningLength);

	private readonly GetParsedValueLengthDelegate _getParsedValueLength;

	private readonly IEqualityComparer _comparer;

	public override IEqualityComparer Comparer => _comparer;

	private GenericHeaderParser(bool supportsMultipleValues, GetParsedValueLengthDelegate getParsedValueLength)
		: this(supportsMultipleValues, getParsedValueLength, null)
	{
	}

	private GenericHeaderParser(bool supportsMultipleValues, GetParsedValueLengthDelegate getParsedValueLength, IEqualityComparer comparer)
		: base(supportsMultipleValues)
	{
		_getParsedValueLength = getParsedValueLength;
		_comparer = comparer;
	}

	protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
	{
		return _getParsedValueLength(value, startIndex, out parsedValue);
	}

	private static int ParseNameValue(string value, int startIndex, out object parsedValue)
	{
		NameValueHeaderValue parsedValue2;
		int nameValueLength = NameValueHeaderValue.GetNameValueLength(value, startIndex, out parsedValue2);
		parsedValue = parsedValue2;
		return nameValueLength;
	}

	private static int ParseProduct(string value, int startIndex, out object parsedValue)
	{
		ProductHeaderValue parsedValue2;
		int productLength = ProductHeaderValue.GetProductLength(value, startIndex, out parsedValue2);
		parsedValue = parsedValue2;
		return productLength;
	}

	private static int ParseSingleEntityTag(string value, int startIndex, out object parsedValue)
	{
		parsedValue = null;
		EntityTagHeaderValue parsedValue2;
		int entityTagLength = EntityTagHeaderValue.GetEntityTagLength(value, startIndex, out parsedValue2);
		if (parsedValue2 == EntityTagHeaderValue.Any)
		{
			return 0;
		}
		parsedValue = parsedValue2;
		return entityTagLength;
	}

	private static int ParseMultipleEntityTags(string value, int startIndex, out object parsedValue)
	{
		EntityTagHeaderValue parsedValue2;
		int entityTagLength = EntityTagHeaderValue.GetEntityTagLength(value, startIndex, out parsedValue2);
		parsedValue = parsedValue2;
		return entityTagLength;
	}

	private static int ParseWithoutValidation(string value, int startIndex, out object parsedValue)
	{
		if (HttpRuleParser.ContainsNewLine(value, startIndex))
		{
			parsedValue = null;
			return 0;
		}
		return ((string)(parsedValue = value.Substring(startIndex))).Length;
	}

	private static int ParseHost(string value, int startIndex, out object parsedValue)
	{
		string host;
		int hostLength = HttpRuleParser.GetHostLength(value, startIndex, allowToken: false, out host);
		parsedValue = host;
		return hostLength;
	}

	private static int ParseTokenList(string value, int startIndex, out object parsedValue)
	{
		int tokenLength = HttpRuleParser.GetTokenLength(value, startIndex);
		parsedValue = value.Substring(startIndex, tokenLength);
		return tokenLength;
	}
}
