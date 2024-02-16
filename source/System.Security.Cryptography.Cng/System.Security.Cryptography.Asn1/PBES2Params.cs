using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct PBES2Params
{
	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn KeyDerivationFunc;

	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn EncryptionScheme;

	internal static System.Security.Cryptography.Asn1.PBES2Params Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static System.Security.Cryptography.Asn1.PBES2Params Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		try
		{
			System.Formats.Asn1.AsnValueReader reader = new System.Formats.Asn1.AsnValueReader(encoded.Span, ruleSet);
			DecodeCore(ref reader, expectedTag, encoded, out var decoded);
			reader.ThrowIfNotEmpty();
			return decoded;
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.PBES2Params decoded)
	{
		decoded = default(System.Security.Cryptography.Asn1.PBES2Params);
		System.Formats.Asn1.AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.KeyDerivationFunc);
		System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.EncryptionScheme);
		reader2.ThrowIfNotEmpty();
	}
}
