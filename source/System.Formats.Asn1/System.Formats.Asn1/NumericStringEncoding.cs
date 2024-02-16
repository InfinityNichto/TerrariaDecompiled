namespace System.Formats.Asn1;

internal sealed class NumericStringEncoding : RestrictedAsciiStringEncoding
{
	internal NumericStringEncoding()
		: base("0123456789 ")
	{
	}
}
