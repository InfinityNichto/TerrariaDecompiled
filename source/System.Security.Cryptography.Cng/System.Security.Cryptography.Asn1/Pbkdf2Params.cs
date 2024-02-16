using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct Pbkdf2Params
{
	internal System.Security.Cryptography.Asn1.Pbkdf2SaltChoice Salt;

	internal int IterationCount;

	internal int? KeyLength;

	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn Prf;

	private static ReadOnlySpan<byte> DefaultPrf => new byte[14]
	{
		48, 12, 6, 8, 42, 134, 72, 134, 247, 13,
		2, 7, 5, 0
	};

	internal static System.Security.Cryptography.Asn1.Pbkdf2Params Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static System.Security.Cryptography.Asn1.Pbkdf2Params Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.Pbkdf2Params decoded)
	{
		decoded = default(System.Security.Cryptography.Asn1.Pbkdf2Params);
		System.Formats.Asn1.AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		System.Security.Cryptography.Asn1.Pbkdf2SaltChoice.Decode(ref reader2, rebind, out decoded.Salt);
		if (!reader2.TryReadInt32(out decoded.IterationCount))
		{
			reader2.ThrowIfNotEmpty();
		}
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
		{
			if (reader2.TryReadInt32(out var value))
			{
				decoded.KeyLength = value;
			}
			else
			{
				reader2.ThrowIfNotEmpty();
			}
		}
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
		{
			System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.Prf);
		}
		else
		{
			System.Formats.Asn1.AsnValueReader reader3 = new System.Formats.Asn1.AsnValueReader(DefaultPrf, AsnEncodingRules.DER);
			System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader3, rebind, out decoded.Prf);
		}
		reader2.ThrowIfNotEmpty();
	}
}
