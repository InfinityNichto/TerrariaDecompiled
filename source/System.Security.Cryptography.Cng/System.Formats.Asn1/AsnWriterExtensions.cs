using System.Security.Cryptography;

namespace System.Formats.Asn1;

internal static class AsnWriterExtensions
{
	internal static void WriteEncodedValueForCrypto(this AsnWriter writer, ReadOnlySpan<byte> value)
	{
		try
		{
			writer.WriteEncodedValue(value);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	internal static void WriteObjectIdentifierForCrypto(this AsnWriter writer, string value)
	{
		try
		{
			writer.WriteObjectIdentifier(value);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}
}
