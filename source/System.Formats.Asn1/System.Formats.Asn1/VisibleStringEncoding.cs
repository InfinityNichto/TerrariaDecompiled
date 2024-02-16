namespace System.Formats.Asn1;

internal sealed class VisibleStringEncoding : RestrictedAsciiStringEncoding
{
	internal VisibleStringEncoding()
		: base(32, 126)
	{
	}
}
