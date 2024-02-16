using System.Formats.Asn1;
using System.Numerics;

namespace System.Security.Cryptography.Asn1;

internal struct DssParms
{
	internal BigInteger P;

	internal BigInteger Q;

	internal BigInteger G;

	internal static DssParms Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static DssParms Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		try
		{
			AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);
			DecodeCore(ref reader, expectedTag, encoded, out var decoded);
			reader.ThrowIfNotEmpty();
			return decoded;
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out DssParms decoded)
	{
		decoded = default(DssParms);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		decoded.P = asnValueReader.ReadInteger();
		decoded.Q = asnValueReader.ReadInteger();
		decoded.G = asnValueReader.ReadInteger();
		asnValueReader.ThrowIfNotEmpty();
	}
}
