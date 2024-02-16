using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography.Asn1;

internal struct X509ExtensionAsn
{
	internal string ExtnId;

	internal bool Critical;

	internal ReadOnlyMemory<byte> ExtnValue;

	private static ReadOnlySpan<byte> DefaultCritical => new byte[3] { 1, 1, 0 };

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		try
		{
			writer.WriteObjectIdentifier(ExtnId);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.WriteBoolean(Critical);
		if (!asnWriter.EncodedValueEquals(DefaultCritical))
		{
			asnWriter.CopyTo(writer);
		}
		writer.WriteOctetString(ExtnValue.Span);
		writer.PopSequence(tag);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out X509ExtensionAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out X509ExtensionAsn decoded)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out X509ExtensionAsn decoded)
	{
		decoded = default(X509ExtensionAsn);
		System.Formats.Asn1.AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		decoded.ExtnId = asnValueReader.ReadObjectIdentifier();
		if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
		{
			decoded.Critical = asnValueReader.ReadBoolean();
		}
		else
		{
			decoded.Critical = new System.Formats.Asn1.AsnValueReader(DefaultCritical, AsnEncodingRules.DER).ReadBoolean();
		}
		if (asnValueReader.TryReadPrimitiveOctetString(out var value))
		{
			decoded.ExtnValue = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.ExtnValue = asnValueReader.ReadOctetString();
		}
		asnValueReader.ThrowIfNotEmpty();
	}

	public X509ExtensionAsn(X509Extension extension)
	{
		if (extension == null)
		{
			throw new ArgumentNullException("extension");
		}
		ExtnId = extension.Oid.Value;
		Critical = extension.Critical;
		ExtnValue = extension.RawData;
	}
}
