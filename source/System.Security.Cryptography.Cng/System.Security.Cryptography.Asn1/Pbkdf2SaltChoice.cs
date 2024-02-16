using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct Pbkdf2SaltChoice
{
	internal ReadOnlyMemory<byte>? Specified;

	internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn? OtherSource;

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.Pbkdf2SaltChoice decoded)
	{
		try
		{
			DecodeCore(ref reader, rebind, out decoded);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out System.Security.Cryptography.Asn1.Pbkdf2SaltChoice decoded)
	{
		decoded = default(System.Security.Cryptography.Asn1.Pbkdf2SaltChoice);
		Asn1Tag asn1Tag = reader.PeekTag();
		ReadOnlySpan<byte> span = rebind.Span;
		if (asn1Tag.HasSameClassAndValue(Asn1Tag.PrimitiveOctetString))
		{
			if (reader.TryReadPrimitiveOctetString(out var value))
			{
				decoded.Specified = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
			}
			else
			{
				decoded.Specified = reader.ReadOctetString();
			}
			return;
		}
		if (asn1Tag.HasSameClassAndValue(Asn1Tag.Sequence))
		{
			System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader, rebind, out var decoded2);
			decoded.OtherSource = decoded2;
			return;
		}
		throw new CryptographicException();
	}
}
