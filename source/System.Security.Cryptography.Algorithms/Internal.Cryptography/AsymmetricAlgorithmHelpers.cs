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

	internal static bool TryConvertIeee1363ToDer(ReadOnlySpan<byte> input, Span<byte> destination, out int bytesWritten)
	{
		AsnWriter asnWriter = WriteIeee1363ToDer(input);
		return asnWriter.TryEncode(destination, out bytesWritten);
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

	public static byte[] ConvertDerToIeee1363(ReadOnlySpan<byte> input, int fieldSizeBits)
	{
		int num = BitsToBytes(fieldSizeBits);
		int num2 = 2 * num;
		byte[] array = new byte[num2];
		ConvertDerToIeee1363(input, fieldSizeBits, array);
		return array;
	}

	internal static int ConvertDerToIeee1363(ReadOnlySpan<byte> input, int fieldSizeBits, Span<byte> destination)
	{
		int num = BitsToBytes(fieldSizeBits);
		int result = 2 * num;
		try
		{
			AsnValueReader asnValueReader = new AsnValueReader(input, AsnEncodingRules.DER);
			AsnValueReader asnValueReader2 = asnValueReader.ReadSequence();
			asnValueReader.ThrowIfNotEmpty();
			ReadOnlySpan<byte> signatureField = asnValueReader2.ReadIntegerBytes();
			ReadOnlySpan<byte> signatureField2 = asnValueReader2.ReadIntegerBytes();
			asnValueReader2.ThrowIfNotEmpty();
			CopySignatureField(signatureField, destination.Slice(0, num));
			CopySignatureField(signatureField2, destination.Slice(num, num));
			return result;
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	internal static int GetMaxDerSignatureSize(int fieldSizeBits)
	{
		int num = BitsToBytes(fieldSizeBits + 1);
		if (num <= 61)
		{
			return 2 * num + 6;
		}
		if (num <= 127)
		{
			return 2 * num + 7;
		}
		int num2 = 2 + GetDerLengthLength(num) + num;
		int num3 = 2 * num2;
		return 2 + GetDerLengthLength(num3) + num3;
		static int GetDerLengthLength(int payloadLength)
		{
			if (payloadLength <= 127)
			{
				return 0;
			}
			if (payloadLength <= 255)
			{
				return 1;
			}
			if (payloadLength <= 65535)
			{
				return 2;
			}
			if (payloadLength <= 16777215)
			{
				return 3;
			}
			return 4;
		}
	}

	internal static byte[] ConvertFromIeeeP1363Signature(byte[] signature, DSASignatureFormat targetFormat)
	{
		return targetFormat switch
		{
			DSASignatureFormat.IeeeP1363FixedFieldConcatenation => signature, 
			DSASignatureFormat.Rfc3279DerSequence => ConvertIeee1363ToDer(signature), 
			_ => throw new CryptographicException(System.SR.Cryptography_UnknownSignatureFormat, targetFormat.ToString()), 
		};
	}

	internal static byte[] ConvertSignatureToIeeeP1363(DSASignatureFormat currentFormat, ReadOnlySpan<byte> signature, int fieldSizeBits)
	{
		return currentFormat switch
		{
			DSASignatureFormat.IeeeP1363FixedFieldConcatenation => signature.ToArray(), 
			DSASignatureFormat.Rfc3279DerSequence => ConvertDerToIeee1363(signature, fieldSizeBits), 
			_ => throw new CryptographicException(System.SR.Cryptography_UnknownSignatureFormat, currentFormat.ToString()), 
		};
	}

	public static int BitsToBytes(int bitLength)
	{
		return (bitLength + 7) / 8;
	}

	private static void CopySignatureField(ReadOnlySpan<byte> signatureField, Span<byte> response)
	{
		if (signatureField.Length > response.Length)
		{
			if (signatureField.Length != response.Length + 1 || signatureField[0] != 0 || signatureField[1] <= 127)
			{
				throw new CryptographicException();
			}
			signatureField = signatureField.Slice(1);
		}
		int num = response.Length - signatureField.Length;
		response.Slice(0, num).Clear();
		signatureField.CopyTo(response.Slice(num));
	}

	internal static byte[] ConvertSignatureToIeeeP1363(this DSA dsa, DSASignatureFormat currentFormat, ReadOnlySpan<byte> signature, int fieldSizeBits = 0)
	{
		try
		{
			if (fieldSizeBits == 0)
			{
				fieldSizeBits = dsa.ExportParameters(includePrivateParameters: false).Q.Length * 8;
			}
			return ConvertSignatureToIeeeP1363(currentFormat, signature, fieldSizeBits);
		}
		catch (CryptographicException)
		{
			return null;
		}
	}

	internal static byte[] ConvertSignatureToIeeeP1363(this ECDsa ecdsa, DSASignatureFormat currentFormat, ReadOnlySpan<byte> signature)
	{
		try
		{
			return ConvertSignatureToIeeeP1363(currentFormat, signature, ecdsa.KeySize);
		}
		catch (CryptographicException)
		{
			return null;
		}
	}
}
