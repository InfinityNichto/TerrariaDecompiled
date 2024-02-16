using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct CurveAsn
{
	internal ReadOnlyMemory<byte> A;

	internal ReadOnlyMemory<byte> B;

	internal ReadOnlyMemory<byte>? Seed;

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		writer.WriteOctetString(A.Span);
		writer.WriteOctetString(B.Span);
		if (Seed.HasValue)
		{
			writer.WriteBitString(Seed.Value.Span);
		}
		writer.PopSequence(tag);
	}

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out CurveAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out CurveAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out CurveAsn decoded)
	{
		decoded = default(CurveAsn);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		int elementOffset;
		if (asnValueReader.TryReadPrimitiveOctetString(out var value))
		{
			decoded.A = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.A = asnValueReader.ReadOctetString();
		}
		if (asnValueReader.TryReadPrimitiveOctetString(out value))
		{
			decoded.B = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.B = asnValueReader.ReadOctetString();
		}
		if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(Asn1Tag.PrimitiveBitString))
		{
			if (asnValueReader.TryReadPrimitiveBitString(out var unusedBitCount, out value))
			{
				decoded.Seed = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
			}
			else
			{
				decoded.Seed = asnValueReader.ReadBitString(out unusedBitCount);
			}
		}
		asnValueReader.ThrowIfNotEmpty();
	}
}
