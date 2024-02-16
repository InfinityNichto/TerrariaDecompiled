using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1;

internal struct CertificationRequestInfoAsn
{
	internal BigInteger Version;

	internal ReadOnlyMemory<byte> Subject;

	internal System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn SubjectPublicKeyInfo;

	internal System.Security.Cryptography.Asn1.AttributeAsn[] Attributes;

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		writer.WriteInteger(Version);
		if (!Asn1Tag.TryDecode(Subject.Span, out var tag2, out var _) || !tag2.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.Sequence)))
		{
			throw new CryptographicException();
		}
		try
		{
			writer.WriteEncodedValue(Subject.Span);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		SubjectPublicKeyInfo.Encode(writer);
		writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
		for (int i = 0; i < Attributes.Length; i++)
		{
			Attributes[i].Encode(writer);
		}
		writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
		writer.PopSequence(tag);
	}
}
