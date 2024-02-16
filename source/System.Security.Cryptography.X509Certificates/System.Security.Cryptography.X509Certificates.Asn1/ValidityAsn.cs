using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1;

internal struct ValidityAsn
{
	internal TimeAsn NotBefore;

	internal TimeAsn NotAfter;

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		NotBefore.Encode(writer);
		NotAfter.Encode(writer);
		writer.PopSequence(tag);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out ValidityAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out ValidityAsn decoded)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out ValidityAsn decoded)
	{
		decoded = default(ValidityAsn);
		System.Formats.Asn1.AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		TimeAsn.Decode(ref reader2, rebind, out decoded.NotBefore);
		TimeAsn.Decode(ref reader2, rebind, out decoded.NotAfter);
		reader2.ThrowIfNotEmpty();
	}

	public ValidityAsn(DateTimeOffset notBefore, DateTimeOffset notAfter)
	{
		NotBefore = new TimeAsn(notBefore);
		NotAfter = new TimeAsn(notAfter);
	}
}
