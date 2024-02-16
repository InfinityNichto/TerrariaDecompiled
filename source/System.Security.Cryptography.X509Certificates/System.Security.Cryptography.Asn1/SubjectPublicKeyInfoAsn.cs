using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct SubjectPublicKeyInfoAsn
{
	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn Algorithm;

	internal ReadOnlyMemory<byte> SubjectPublicKey;

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		Algorithm.Encode(writer);
		writer.WriteBitString(SubjectPublicKey.Span);
		writer.PopSequence(tag);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn decoded)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn decoded)
	{
		decoded = default(System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn);
		System.Formats.Asn1.AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.Algorithm);
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
