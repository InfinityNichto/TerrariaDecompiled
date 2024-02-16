using System.Collections.Generic;
using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct PrivateKeyInfoAsn
{
	internal int Version;

	internal AlgorithmIdentifierAsn PrivateKeyAlgorithm;

	internal ReadOnlyMemory<byte> PrivateKey;

	internal AttributeAsn[] Attributes;

	internal static PrivateKeyInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static PrivateKeyInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
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

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out PrivateKeyInfoAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PrivateKeyInfoAsn decoded)
	{
		try
		{
			DecodeCore(ref reader, expectedTag, rebind, out decoded);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PrivateKeyInfoAsn decoded)
	{
		decoded = default(PrivateKeyInfoAsn);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		if (!reader2.TryReadInt32(out decoded.Version))
		{
			reader2.ThrowIfNotEmpty();
		}
		AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.PrivateKeyAlgorithm);
		if (reader2.TryReadPrimitiveOctetString(out var value))
		{
			decoded.PrivateKey = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.PrivateKey = reader2.ReadOctetString();
		}
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
		{
			AsnValueReader reader3 = reader2.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
			List<AttributeAsn> list = new List<AttributeAsn>();
			while (reader3.HasData)
			{
				AttributeAsn.Decode(ref reader3, rebind, out var decoded2);
				list.Add(decoded2);
			}
			decoded.Attributes = list.ToArray();
		}
		reader2.ThrowIfNotEmpty();
	}
}
