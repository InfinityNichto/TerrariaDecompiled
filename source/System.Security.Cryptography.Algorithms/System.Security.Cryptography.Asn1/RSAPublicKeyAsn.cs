using System.Formats.Asn1;
using System.Numerics;

namespace System.Security.Cryptography.Asn1;

internal struct RSAPublicKeyAsn
{
	internal BigInteger Modulus;

	internal BigInteger PublicExponent;

	internal static RSAPublicKeyAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static RSAPublicKeyAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		try
		{
			AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);
			DecodeCore(ref reader, expectedTag, encoded, out var decoded);
			reader.ThrowIfNotEmpty();
			return decoded;
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out RSAPublicKeyAsn decoded)
	{
		decoded = default(RSAPublicKeyAsn);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		decoded.Modulus = asnValueReader.ReadInteger();
		decoded.PublicExponent = asnValueReader.ReadInteger();
		asnValueReader.ThrowIfNotEmpty();
	}
}
