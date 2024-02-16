using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct OtherNameAsn
{
	internal string TypeId;

	internal ReadOnlyMemory<byte> Value;

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		try
		{
			writer.WriteObjectIdentifier(TypeId);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		try
		{
			writer.WriteEncodedValue(Value.Span);
		}
		catch (ArgumentException inner2)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner2);
		}
		writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		writer.PopSequence(tag);
	}
}
