using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct PssParamsAsn
{
	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn HashAlgorithm;

	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn MaskGenAlgorithm;

	internal int SaltLength;

	internal int TrailerField;

	private static ReadOnlySpan<byte> DefaultHashAlgorithm => new byte[11]
	{
		48, 9, 6, 5, 43, 14, 3, 2, 26, 5,
		0
	};

	private static ReadOnlySpan<byte> DefaultMaskGenAlgorithm => new byte[24]
	{
		48, 22, 6, 9, 42, 134, 72, 134, 247, 13,
		1, 1, 8, 48, 9, 6, 5, 43, 14, 3,
		2, 26, 5, 0
	};

	private static ReadOnlySpan<byte> DefaultSaltLength => new byte[3] { 2, 1, 20 };

	private static ReadOnlySpan<byte> DefaultTrailerField => new byte[3] { 2, 1, 1 };

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		HashAlgorithm.Encode(asnWriter);
		if (!asnWriter.EncodedValueEquals(DefaultHashAlgorithm))
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
			asnWriter.CopyTo(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		}
		AsnWriter asnWriter2 = new AsnWriter(AsnEncodingRules.DER);
		MaskGenAlgorithm.Encode(asnWriter2);
		if (!asnWriter2.EncodedValueEquals(DefaultMaskGenAlgorithm))
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
			asnWriter2.CopyTo(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
		}
		AsnWriter asnWriter3 = new AsnWriter(AsnEncodingRules.DER);
		asnWriter3.WriteInteger(SaltLength);
		if (!asnWriter3.EncodedValueEquals(DefaultSaltLength))
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
			asnWriter3.CopyTo(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
		}
		AsnWriter asnWriter4 = new AsnWriter(AsnEncodingRules.DER);
		asnWriter4.WriteInteger(TrailerField);
		if (!asnWriter4.EncodedValueEquals(DefaultTrailerField))
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
			asnWriter4.CopyTo(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
		}
		writer.PopSequence(tag);
	}
}
