using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct EdiPartyNameAsn
{
	internal DirectoryStringAsn? NameAssigner;

	internal DirectoryStringAsn PartyName;

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		if (NameAssigner.HasValue)
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
			NameAssigner.Value.Encode(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		}
		writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
		PartyName.Encode(writer);
		writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
		writer.PopSequence(tag);
	}
}
