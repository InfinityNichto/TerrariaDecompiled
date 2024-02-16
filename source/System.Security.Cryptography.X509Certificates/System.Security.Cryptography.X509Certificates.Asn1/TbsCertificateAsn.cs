using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1;

internal struct TbsCertificateAsn
{
	internal int Version;

	internal ReadOnlyMemory<byte> SerialNumber;

	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn SignatureAlgorithm;

	internal ReadOnlyMemory<byte> Issuer;

	internal ValidityAsn Validity;

	internal ReadOnlyMemory<byte> Subject;

	internal System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn SubjectPublicKeyInfo;

	internal ReadOnlyMemory<byte>? IssuerUniqueId;

	internal ReadOnlyMemory<byte>? SubjectUniqueId;

	internal X509ExtensionAsn[] Extensions;

	private static ReadOnlySpan<byte> DefaultVersion => new byte[3] { 2, 1, 0 };

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.WriteInteger(Version);
		if (!asnWriter.EncodedValueEquals(DefaultVersion))
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
			asnWriter.CopyTo(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		}
		writer.WriteInteger(SerialNumber.Span);
		SignatureAlgorithm.Encode(writer);
		if (!Asn1Tag.TryDecode(Issuer.Span, out var tag2, out var bytesConsumed) || !tag2.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.Sequence)))
		{
			throw new CryptographicException();
		}
		try
		{
			writer.WriteEncodedValue(Issuer.Span);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		Validity.Encode(writer);
		if (!Asn1Tag.TryDecode(Subject.Span, out var tag3, out bytesConsumed) || !tag3.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.Sequence)))
		{
			throw new CryptographicException();
		}
		try
		{
			writer.WriteEncodedValue(Subject.Span);
		}
		catch (ArgumentException inner2)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner2);
		}
		SubjectPublicKeyInfo.Encode(writer);
		if (IssuerUniqueId.HasValue)
		{
			writer.WriteBitString(IssuerUniqueId.Value.Span, 0, new Asn1Tag(TagClass.ContextSpecific, 1));
		}
		if (SubjectUniqueId.HasValue)
		{
			writer.WriteBitString(SubjectUniqueId.Value.Span, 0, new Asn1Tag(TagClass.ContextSpecific, 2));
		}
		if (Extensions != null)
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
			writer.PushSequence();
			for (int i = 0; i < Extensions.Length; i++)
			{
				Extensions[i].Encode(writer);
			}
			writer.PopSequence();
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
		}
		writer.PopSequence(tag);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out TbsCertificateAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out TbsCertificateAsn decoded)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out TbsCertificateAsn decoded)
	{
		decoded = default(TbsCertificateAsn);
		System.Formats.Asn1.AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		System.Formats.Asn1.AsnValueReader asnValueReader;
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
		{
			asnValueReader = reader2.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
			if (!asnValueReader.TryReadInt32(out decoded.Version))
			{
				asnValueReader.ThrowIfNotEmpty();
			}
			asnValueReader.ThrowIfNotEmpty();
		}
		else
		{
			System.Formats.Asn1.AsnValueReader asnValueReader2 = new System.Formats.Asn1.AsnValueReader(DefaultVersion, AsnEncodingRules.DER);
			if (!asnValueReader2.TryReadInt32(out decoded.Version))
			{
				asnValueReader2.ThrowIfNotEmpty();
			}
		}
		ReadOnlySpan<byte> value = reader2.ReadIntegerBytes();
		decoded.SerialNumber = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.SignatureAlgorithm);
		if (!reader2.PeekTag().HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.Sequence)))
		{
			throw new CryptographicException();
		}
		value = reader2.ReadEncodedValue();
		decoded.Issuer = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		ValidityAsn.Decode(ref reader2, rebind, out decoded.Validity);
		if (!reader2.PeekTag().HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.Sequence)))
		{
			throw new CryptographicException();
		}
		value = reader2.ReadEncodedValue();
		decoded.Subject = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn.Decode(ref reader2, rebind, out decoded.SubjectPublicKeyInfo);
		int unusedBitCount;
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
		{
			if (reader2.TryReadPrimitiveBitString(out unusedBitCount, out value, new Asn1Tag(TagClass.ContextSpecific, 1)))
			{
				decoded.IssuerUniqueId = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
			}
			else
			{
				decoded.IssuerUniqueId = reader2.ReadBitString(out unusedBitCount, new Asn1Tag(TagClass.ContextSpecific, 1));
			}
		}
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
		{
			if (reader2.TryReadPrimitiveBitString(out unusedBitCount, out value, new Asn1Tag(TagClass.ContextSpecific, 2)))
			{
				decoded.SubjectUniqueId = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
			}
			else
			{
				decoded.SubjectUniqueId = reader2.ReadBitString(out unusedBitCount, new Asn1Tag(TagClass.ContextSpecific, 2));
			}
		}
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
		{
			asnValueReader = reader2.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
			System.Formats.Asn1.AsnValueReader reader3 = asnValueReader.ReadSequence();
			List<X509ExtensionAsn> list = new List<X509ExtensionAsn>();
			while (reader3.HasData)
			{
				X509ExtensionAsn.Decode(ref reader3, rebind, out var decoded2);
				list.Add(decoded2);
			}
			decoded.Extensions = list.ToArray();
			asnValueReader.ThrowIfNotEmpty();
		}
		reader2.ThrowIfNotEmpty();
	}
}
