using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct ECPrivateKey
{
	internal int Version;

	internal ReadOnlyMemory<byte> PrivateKey;

	internal System.Security.Cryptography.Asn1.ECDomainParameters? Parameters;

	internal ReadOnlyMemory<byte>? PublicKey;

	internal static System.Security.Cryptography.Asn1.ECPrivateKey Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static System.Security.Cryptography.Asn1.ECPrivateKey Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.ECPrivateKey decoded)
	{
		decoded = default(System.Security.Cryptography.Asn1.ECPrivateKey);
		System.Formats.Asn1.AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		if (!asnValueReader.TryReadInt32(out decoded.Version))
		{
			asnValueReader.ThrowIfNotEmpty();
		}
		int elementOffset;
		if (asnValueReader.TryReadPrimitiveOctetString(out var value))
		{
			decoded.PrivateKey = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.PrivateKey = asnValueReader.ReadOctetString();
		}
		System.Formats.Asn1.AsnValueReader reader2;
		if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
		{
			reader2 = asnValueReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
			System.Security.Cryptography.Asn1.ECDomainParameters.Decode(ref reader2, rebind, out var decoded2);
			decoded.Parameters = decoded2;
			reader2.ThrowIfNotEmpty();
		}
		if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
		{
			reader2 = asnValueReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
			if (reader2.TryReadPrimitiveBitString(out var unusedBitCount, out value))
			{
				decoded.PublicKey = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
			}
			else
			{
				decoded.PublicKey = reader2.ReadBitString(out unusedBitCount);
			}
			reader2.ThrowIfNotEmpty();
		}
		asnValueReader.ThrowIfNotEmpty();
	}
}
