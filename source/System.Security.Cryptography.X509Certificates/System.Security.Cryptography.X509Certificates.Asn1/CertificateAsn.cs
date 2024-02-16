using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1;

internal struct CertificateAsn
{
	internal TbsCertificateAsn TbsCertificate;

	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn SignatureAlgorithm;

	internal ReadOnlyMemory<byte> SignatureValue;

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		TbsCertificate.Encode(writer);
		SignatureAlgorithm.Encode(writer);
		writer.WriteBitString(SignatureValue.Span);
		writer.PopSequence(tag);
	}

	internal static CertificateAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static CertificateAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out CertificateAsn decoded)
	{
		decoded = default(CertificateAsn);
		System.Formats.Asn1.AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		TbsCertificateAsn.Decode(ref reader2, rebind, out decoded.TbsCertificate);
		System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.SignatureAlgorithm);
		if (reader2.TryReadPrimitiveBitString(out var unusedBitCount, out var value))
		{
			decoded.SignatureValue = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.SignatureValue = reader2.ReadBitString(out unusedBitCount);
		}
		reader2.ThrowIfNotEmpty();
	}
}
