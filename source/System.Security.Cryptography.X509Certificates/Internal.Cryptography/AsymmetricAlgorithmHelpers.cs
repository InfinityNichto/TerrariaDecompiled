using System;
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal static class AsymmetricAlgorithmHelpers
{
	public static byte[] ConvertIeee1363ToDer(ReadOnlySpan<byte> input)
	{
		AsnWriter asnWriter = WriteIeee1363ToDer(input);
		return asnWriter.Encode();
	}

	private static AsnWriter WriteIeee1363ToDer(ReadOnlySpan<byte> input)
	{
		int num = input.Length / 2;
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.PushSequence();
		asnWriter.WriteKeyParameterInteger(input.Slice(0, num));
		asnWriter.WriteKeyParameterInteger(input.Slice(num, num));
		asnWriter.PopSequence();
		return asnWriter;
	}
}
