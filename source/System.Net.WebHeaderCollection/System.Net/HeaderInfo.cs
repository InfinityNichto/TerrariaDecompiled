namespace System.Net;

internal sealed class HeaderInfo
{
	internal readonly bool IsRequestRestricted;

	internal readonly bool IsResponseRestricted;

	internal readonly Func<string, string[]> Parser;

	internal readonly string HeaderName;

	internal readonly bool AllowMultiValues;

	internal HeaderInfo(string name, bool requestRestricted, bool responseRestricted, bool multi, Func<string, string[]> parser)
	{
		HeaderName = name;
		IsRequestRestricted = requestRestricted;
		IsResponseRestricted = responseRestricted;
		Parser = parser;
		AllowMultiValues = multi;
	}
}
