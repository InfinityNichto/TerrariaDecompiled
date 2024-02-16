using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct PBEParameter
{
	internal ReadOnlyMemory<byte> Salt;

	internal int IterationCount;

	internal static System.Security.Cryptography.Asn1.PBEParameter Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static System.Security.Cryptography.Asn1.PBEParameter Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.PBEParameter decoded)
	{
		decoded = default(System.Security.Cryptography.Asn1.PBEParameter);
		System.Formats.Asn1.AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		if (asnValueReader.TryReadPrimitiveOctetString(out var value))
		{
			decoded.Salt = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.Salt = asnValueReader.ReadOctetString();
		}
		if (!asnValueReader.TryReadInt32(out decoded.IterationCount))
		{
			asnValueReader.ThrowIfNotEmpty();
		}
		asnValueReader.ThrowIfNotEmpty();
	}
}
