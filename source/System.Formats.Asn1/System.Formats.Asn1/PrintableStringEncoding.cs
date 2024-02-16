namespace System.Formats.Asn1;

internal sealed class PrintableStringEncoding : RestrictedAsciiStringEncoding
{
	internal PrintableStringEncoding()
		: base("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 '()+,-./:=?")
	{
	}
}
