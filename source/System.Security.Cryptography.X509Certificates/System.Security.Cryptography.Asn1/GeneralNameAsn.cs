using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct GeneralNameAsn
{
	internal OtherNameAsn? OtherName;

	internal string Rfc822Name;

	internal string DnsName;

	internal ReadOnlyMemory<byte>? X400Address;

	internal ReadOnlyMemory<byte>? DirectoryName;

	internal EdiPartyNameAsn? EdiPartyName;

	internal string Uri;

	internal ReadOnlyMemory<byte>? IPAddress;

	internal string RegisteredId;

	internal void Encode(AsnWriter writer)
	{
		bool flag = false;
		if (OtherName.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			OtherName.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 0));
			flag = true;
		}
		if (Rfc822Name != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.IA5String, Rfc822Name, new Asn1Tag(TagClass.ContextSpecific, 1));
			flag = true;
		}
		if (DnsName != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.IA5String, DnsName, new Asn1Tag(TagClass.ContextSpecific, 2));
			flag = true;
		}
		if (X400Address.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			if (!Asn1Tag.TryDecode(X400Address.Value.Span, out var tag, out var _) || !tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
			{
				throw new CryptographicException();
			}
			try
			{
				writer.WriteEncodedValue(X400Address.Value.Span);
			}
			catch (ArgumentException inner)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
			}
			flag = true;
		}
		if (DirectoryName.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
			try
			{
				writer.WriteEncodedValue(DirectoryName.Value.Span);
			}
			catch (ArgumentException inner2)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner2);
			}
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
			flag = true;
		}
		if (EdiPartyName.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			EdiPartyName.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 5));
			flag = true;
		}
		if (Uri != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.IA5String, Uri, new Asn1Tag(TagClass.ContextSpecific, 6));
			flag = true;
		}
		if (IPAddress.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteOctetString(IPAddress.Value.Span, new Asn1Tag(TagClass.ContextSpecific, 7));
			flag = true;
		}
		if (RegisteredId != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			try
			{
				writer.WriteObjectIdentifier(RegisteredId, new Asn1Tag(TagClass.ContextSpecific, 8));
			}
			catch (ArgumentException inner3)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner3);
			}
			flag = true;
		}
		if (!flag)
		{
			throw new CryptographicException();
		}
	}
}
