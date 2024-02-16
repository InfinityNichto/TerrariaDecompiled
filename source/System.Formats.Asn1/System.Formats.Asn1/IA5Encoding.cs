namespace System.Formats.Asn1;

internal sealed class IA5Encoding : RestrictedAsciiStringEncoding
{
	internal IA5Encoding()
		: base(0, 127)
	{
	}
}
