using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct AttributeAsn
{
	internal string AttrType;

	internal ReadOnlyMemory<byte>[] AttrValues;

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		try
		{
			writer.WriteObjectIdentifier(AttrType);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		writer.PushSetOf();
		for (int i = 0; i < AttrValues.Length; i++)
		{
			try
			{
				writer.WriteEncodedValue(AttrValues[i].Span);
			}
			catch (ArgumentException inner2)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner2);
			}
		}
		writer.PopSetOf();
		writer.PopSequence(tag);
	}

	public AttributeAsn(AsnEncodedData attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		AttrType = attribute.Oid.Value;
		AttrValues = new ReadOnlyMemory<byte>[1]
		{
			new ReadOnlyMemory<byte>(attribute.RawData)
		};
	}
}
