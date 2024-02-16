using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct EncryptedPrivateKeyInfoAsn
{
	internal AlgorithmIdentifierAsn EncryptionAlgorithm;

	internal ReadOnlyMemory<byte> EncryptedData;

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out EncryptedPrivateKeyInfoAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedPrivateKeyInfoAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedPrivateKeyInfoAsn decoded)
	{
		decoded = default(EncryptedPrivateKeyInfoAsn);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.EncryptionAlgorithm);
		if (reader2.TryReadPrimitiveOctetString(out var value))
		{
			decoded.EncryptedData = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.EncryptedData = reader2.ReadOctetString();
		}
		reader2.ThrowIfNotEmpty();
	}
}
