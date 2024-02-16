namespace System.Net;

internal readonly struct HeaderVariantInfo
{
	private readonly string _name;

	private readonly CookieVariant _variant;

	internal string Name => _name;

	internal CookieVariant Variant => _variant;

	internal HeaderVariantInfo(string name, CookieVariant variant)
	{
		_name = name;
		_variant = variant;
	}
}
