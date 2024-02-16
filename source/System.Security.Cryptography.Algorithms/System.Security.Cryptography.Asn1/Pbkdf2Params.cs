using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct Pbkdf2Params
{
	internal Pbkdf2SaltChoice Salt;

	internal int IterationCount;

	internal int? KeyLength;

	internal AlgorithmIdentifierAsn Prf;

	private static ReadOnlySpan<byte> DefaultPrf => new byte[14]
	{
		48, 12, 6, 8, 42, 134, 72, 134, 247, 13,
		2, 7, 5, 0
	};

	internal static Pbkdf2Params Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static Pbkdf2Params Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Pbkdf2Params decoded)
	{
		decoded = default(Pbkdf2Params);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		Pbkdf2SaltChoice.Decode(ref reader2, rebind, out decoded.Salt);
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
			AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.Prf);
		}
		else
		{
			AsnValueReader reader3 = new AsnValueReader(DefaultPrf, AsnEncodingRules.DER);
			AlgorithmIdentifierAsn.Decode(ref reader3, rebind, out decoded.Prf);
		}
		reader2.ThrowIfNotEmpty();
	}
}
