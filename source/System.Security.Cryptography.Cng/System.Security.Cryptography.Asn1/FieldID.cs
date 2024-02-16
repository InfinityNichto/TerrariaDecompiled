using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct FieldID
{
	internal string FieldType;

	internal ReadOnlyMemory<byte> Parameters;

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		try
		{
			writer.WriteObjectIdentifier(FieldType);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		try
		{
			writer.WriteEncodedValue(Parameters.Span);
		}
		catch (ArgumentException inner2)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner2);
		}
		writer.PopSequence(tag);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.FieldID decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.FieldID decoded)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.FieldID decoded)
	{
		decoded = default(System.Security.Cryptography.Asn1.FieldID);
		System.Formats.Asn1.AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		decoded.FieldType = asnValueReader.ReadObjectIdentifier();
		ReadOnlySpan<byte> other = asnValueReader.ReadEncodedValue();
		decoded.Parameters = (span.Overlaps(other, out var elementOffset) ? rebind.Slice(elementOffset, other.Length) : ((ReadOnlyMemory<byte>)other.ToArray()));
		asnValueReader.ThrowIfNotEmpty();
	}
}
