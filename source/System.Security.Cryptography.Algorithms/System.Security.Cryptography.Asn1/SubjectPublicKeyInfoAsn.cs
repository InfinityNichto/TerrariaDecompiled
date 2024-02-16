using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct SubjectPublicKeyInfoAsn
{
	internal AlgorithmIdentifierAsn Algorithm;

	internal ReadOnlyMemory<byte> SubjectPublicKey;

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out SubjectPublicKeyInfoAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SubjectPublicKeyInfoAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SubjectPublicKeyInfoAsn decoded)
	{
		decoded = default(SubjectPublicKeyInfoAsn);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.Algorithm);
		if (reader2.TryReadPrimitiveBitString(out var unusedBitCount, out var value))
		{
			decoded.SubjectPublicKey = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.SubjectPublicKey = reader2.ReadBitString(out unusedBitCount);
		}
		reader2.ThrowIfNotEmpty();
	}
}
