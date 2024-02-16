using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct EncryptedPrivateKeyInfoAsn
{
	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn EncryptionAlgorithm;

	internal ReadOnlyMemory<byte> EncryptedData;

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.EncryptedPrivateKeyInfoAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.EncryptedPrivateKeyInfoAsn decoded)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.EncryptedPrivateKeyInfoAsn decoded)
	{
		decoded = default(System.Security.Cryptography.Asn1.EncryptedPrivateKeyInfoAsn);
		System.Formats.Asn1.AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.EncryptionAlgorithm);
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
