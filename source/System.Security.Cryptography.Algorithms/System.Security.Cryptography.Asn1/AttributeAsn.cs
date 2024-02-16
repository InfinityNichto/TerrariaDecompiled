using System.Collections.Generic;
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

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out AttributeAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out AttributeAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out AttributeAsn decoded)
	{
		decoded = default(AttributeAsn);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		decoded.AttrType = asnValueReader.ReadObjectIdentifier();
		AsnValueReader asnValueReader2 = asnValueReader.ReadSetOf();
		List<ReadOnlyMemory<byte>> list = new List<ReadOnlyMemory<byte>>();
		while (asnValueReader2.HasData)
		{
			ReadOnlySpan<byte> other = asnValueReader2.ReadEncodedValue();
			int elementOffset;
			ReadOnlyMemory<byte> item = (span.Overlaps(other, out elementOffset) ? rebind.Slice(elementOffset, other.Length) : ((ReadOnlyMemory<byte>)other.ToArray()));
			list.Add(item);
		}
		decoded.AttrValues = list.ToArray();
		asnValueReader.ThrowIfNotEmpty();
	}
}
