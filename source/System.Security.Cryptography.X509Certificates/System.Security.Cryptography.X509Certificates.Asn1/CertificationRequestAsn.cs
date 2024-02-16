using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1;

internal struct CertificationRequestAsn
{
	internal CertificationRequestInfoAsn CertificationRequestInfo;

	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn SignatureAlgorithm;

	internal ReadOnlyMemory<byte> SignatureValue;

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		CertificationRequestInfo.Encode(writer);
		SignatureAlgorithm.Encode(writer);
		writer.WriteBitString(SignatureValue.Span);
		writer.PopSequence(tag);
	}
}
