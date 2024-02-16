namespace System.Net.Http.Headers;

internal sealed class CacheControlHeaderParser : BaseHeaderParser
{
	internal static readonly CacheControlHeaderParser Parser = new CacheControlHeaderParser();

	private CacheControlHeaderParser()
		: base(supportsMultipleValues: true)
	{
	}

	protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
	{
		CacheControlHeaderValue parsedValue2 = storeValue as CacheControlHeaderValue;
		int cacheControlLength = CacheControlHeaderValue.GetCacheControlLength(value, startIndex, parsedValue2, out parsedValue2);
		parsedValue = parsedValue2;
		return cacheControlLength;
	}
}
