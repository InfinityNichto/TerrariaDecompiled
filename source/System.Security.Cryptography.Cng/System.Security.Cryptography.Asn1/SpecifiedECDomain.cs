using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct SpecifiedECDomain
{
	internal int Version;

	internal System.Security.Cryptography.Asn1.FieldID FieldID;

	internal System.Security.Cryptography.Asn1.CurveAsn Curve;

	internal ReadOnlyMemory<byte> Base;

	internal ReadOnlyMemory<byte> Order;

	internal ReadOnlyMemory<byte>? Cofactor;

	internal string Hash;

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		writer.WriteInteger((long)Version, (Asn1Tag?)null);
		FieldID.Encode(writer);
		Curve.Encode(writer);
		writer.WriteOctetString(Base.Span);
		writer.WriteInteger(Order.Span);
		if (Cofactor.HasValue)
		{
			writer.WriteInteger(Cofactor.Value.Span);
		}
		if (Hash != null)
		{
			try
			{
				writer.WriteObjectIdentifier(Hash);
			}
			catch (ArgumentException inner)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
			}
		}
		writer.PopSequence(tag);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.SpecifiedECDomain decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.SpecifiedECDomain decoded)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.SpecifiedECDomain decoded)
	{
		decoded = default(System.Security.Cryptography.Asn1.SpecifiedECDomain);
		System.Formats.Asn1.AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		if (!reader2.TryReadInt32(out decoded.Version))
		{
			reader2.ThrowIfNotEmpty();
		}
		System.Security.Cryptography.Asn1.FieldID.Decode(ref reader2, rebind, out decoded.FieldID);
		System.Security.Cryptography.Asn1.CurveAsn.Decode(ref reader2, rebind, out decoded.Curve);
		int elementOffset;
		if (reader2.TryReadPrimitiveOctetString(out var value))
		{
			decoded.Base = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.Base = reader2.ReadOctetString();
		}
		value = reader2.ReadIntegerBytes();
		decoded.Order = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
		{
			value = reader2.ReadIntegerBytes();
			decoded.Cofactor = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(Asn1Tag.ObjectIdentifier))
		{
			decoded.Hash = reader2.ReadObjectIdentifier();
		}
		reader2.ThrowIfNotEmpty();
	}
}
