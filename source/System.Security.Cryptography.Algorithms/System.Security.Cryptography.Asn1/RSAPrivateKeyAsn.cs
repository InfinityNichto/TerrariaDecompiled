using System.Formats.Asn1;
using System.Numerics;

namespace System.Security.Cryptography.Asn1;

internal struct RSAPrivateKeyAsn
{
	internal int Version;

	internal BigInteger Modulus;

	internal BigInteger PublicExponent;

	internal BigInteger PrivateExponent;

	internal BigInteger Prime1;

	internal BigInteger Prime2;

	internal BigInteger Exponent1;

	internal BigInteger Exponent2;

	internal BigInteger Coefficient;

	internal static RSAPrivateKeyAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static RSAPrivateKeyAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out RSAPrivateKeyAsn decoded)
	{
		decoded = default(RSAPrivateKeyAsn);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		if (!asnValueReader.TryReadInt32(out decoded.Version))
		{
			asnValueReader.ThrowIfNotEmpty();
		}
		decoded.Modulus = asnValueReader.ReadInteger();
		decoded.PublicExponent = asnValueReader.ReadInteger();
		decoded.PrivateExponent = asnValueReader.ReadInteger();
		decoded.Prime1 = asnValueReader.ReadInteger();
		decoded.Prime2 = asnValueReader.ReadInteger();
		decoded.Exponent1 = asnValueReader.ReadInteger();
		decoded.Exponent2 = asnValueReader.ReadInteger();
		decoded.Coefficient = asnValueReader.ReadInteger();
		asnValueReader.ThrowIfNotEmpty();
	}
}
