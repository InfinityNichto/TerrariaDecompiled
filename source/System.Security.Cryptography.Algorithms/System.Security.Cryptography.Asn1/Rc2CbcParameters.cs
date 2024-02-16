using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct Rc2CbcParameters
{
	internal int Rc2Version;

	internal ReadOnlyMemory<byte> Iv;

	private static readonly byte[] s_rc2EkbEncoding = new byte[256]
	{
		189, 86, 234, 242, 162, 241, 172, 42, 176, 147,
		209, 156, 27, 51, 253, 208, 48, 4, 182, 220,
		125, 223, 50, 75, 247, 203, 69, 155, 49, 187,
		33, 90, 65, 159, 225, 217, 74, 77, 158, 218,
		160, 104, 44, 195, 39, 95, 128, 54, 62, 238,
		251, 149, 26, 254, 206, 168, 52, 169, 19, 240,
		166, 63, 216, 12, 120, 36, 175, 35, 82, 193,
		103, 23, 245, 102, 144, 231, 232, 7, 184, 96,
		72, 230, 30, 83, 243, 146, 164, 114, 140, 8,
		21, 110, 134, 0, 132, 250, 244, 127, 138, 66,
		25, 246, 219, 205, 20, 141, 80, 18, 186, 60,
		6, 78, 236, 179, 53, 17, 161, 136, 142, 43,
		148, 153, 183, 113, 116, 211, 228, 191, 58, 222,
		150, 14, 188, 10, 237, 119, 252, 55, 107, 3,
		121, 137, 98, 198, 215, 192, 210, 124, 106, 139,
		34, 163, 91, 5, 93, 2, 117, 213, 97, 227,
		24, 143, 85, 81, 173, 31, 11, 94, 133, 229,
		194, 87, 99, 202, 61, 108, 180, 197, 204, 112,
		178, 145, 89, 13, 71, 32, 200, 79, 88, 224,
		1, 226, 22, 56, 196, 111, 59, 15, 101, 70,
		190, 126, 45, 123, 130, 249, 64, 181, 29, 115,
		248, 235, 38, 199, 135, 151, 37, 84, 177, 40,
		170, 152, 157, 165, 100, 109, 122, 212, 16, 129,
		68, 239, 73, 214, 174, 46, 221, 118, 92, 47,
		167, 28, 201, 9, 105, 154, 131, 207, 41, 57,
		185, 233, 76, 255, 67, 171
	};

	internal static Rc2CbcParameters Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static Rc2CbcParameters Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Rc2CbcParameters decoded)
	{
		decoded = default(Rc2CbcParameters);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		if (!asnValueReader.TryReadInt32(out decoded.Rc2Version))
		{
			asnValueReader.ThrowIfNotEmpty();
		}
		if (asnValueReader.TryReadPrimitiveOctetString(out var value))
		{
			decoded.Iv = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.Iv = asnValueReader.ReadOctetString();
		}
		asnValueReader.ThrowIfNotEmpty();
	}

	internal int GetEffectiveKeyBits()
	{
		if (Rc2Version > 255)
		{
			return Rc2Version;
		}
		return Array.IndexOf(s_rc2EkbEncoding, (byte)Rc2Version);
	}
}
