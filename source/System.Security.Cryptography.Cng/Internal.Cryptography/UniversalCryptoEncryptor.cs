using System;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal sealed class UniversalCryptoEncryptor : Internal.Cryptography.UniversalCryptoTransform
{
	public UniversalCryptoEncryptor(PaddingMode paddingMode, Internal.Cryptography.BasicSymmetricCipher basicSymmetricCipher)
		: base(paddingMode, basicSymmetricCipher)
	{
	}

	protected override int UncheckedTransformBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
	{
		return base.BasicSymmetricCipher.Transform(inputBuffer, outputBuffer);
	}

	protected override int UncheckedTransformFinalBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
	{
		int length = PadBlock(inputBuffer, outputBuffer);
		return base.BasicSymmetricCipher.TransformFinal(outputBuffer.Slice(0, length), outputBuffer);
	}

	protected override byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		byte[] array = GC.AllocateUninitializedArray<byte>(GetCiphertextLength(inputCount));
		int num = UncheckedTransformFinalBlock(inputBuffer.AsSpan(inputOffset, inputCount), array);
		return array;
	}

	public override bool TransformOneShot(ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
	{
		int ciphertextLength = GetCiphertextLength(input.Length);
		if (output.Length < ciphertextLength)
		{
			bytesWritten = 0;
			return false;
		}
		Span<byte> span = output[..PadBlock(input, output)];
		bytesWritten = base.BasicSymmetricCipher.TransformFinal(span, span);
		return true;
	}

	private int GetCiphertextLength(int plaintextLength)
	{
		int result;
		int num = Math.DivRem(plaintextLength, base.PaddingSizeBytes, out result) * base.PaddingSizeBytes;
		switch (base.PaddingMode)
		{
		case PaddingMode.None:
			if (result != 0)
			{
				throw new CryptographicException(System.SR.Cryptography_PartialBlock);
			}
			goto IL_004c;
		case PaddingMode.Zeros:
			if (result == 0)
			{
				goto IL_004c;
			}
			goto case PaddingMode.PKCS7;
		case PaddingMode.PKCS7:
		case PaddingMode.ANSIX923:
		case PaddingMode.ISO10126:
			return checked(num + base.PaddingSizeBytes);
		default:
			{
				throw new CryptographicException(System.SR.Cryptography_UnknownPaddingMode);
			}
			IL_004c:
			return plaintextLength;
		}
	}

	private int PadBlock(ReadOnlySpan<byte> block, Span<byte> destination)
	{
		int length = block.Length;
		int num = length % base.PaddingSizeBytes;
		int num2 = base.PaddingSizeBytes - num;
		switch (base.PaddingMode)
		{
		case PaddingMode.None:
			if (num != 0)
			{
				throw new CryptographicException(System.SR.Cryptography_PartialBlock);
			}
			if (destination.Length < length)
			{
				throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
			}
			block.CopyTo(destination);
			return length;
		case PaddingMode.ANSIX923:
		{
			int num4 = length + num2;
			if (destination.Length < num4)
			{
				throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
			}
			block.CopyTo(destination);
			destination.Slice(length, num2 - 1).Clear();
			destination[length + num2 - 1] = (byte)num2;
			return num4;
		}
		case PaddingMode.ISO10126:
		{
			int num6 = length + num2;
			if (destination.Length < num6)
			{
				throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
			}
			block.CopyTo(destination);
			RandomNumberGenerator.Fill(destination.Slice(length, num2 - 1));
			destination[length + num2 - 1] = (byte)num2;
			return num6;
		}
		case PaddingMode.PKCS7:
		{
			int num5 = length + num2;
			if (destination.Length < num5)
			{
				throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
			}
			block.CopyTo(destination);
			destination.Slice(length, num2).Fill((byte)num2);
			return num5;
		}
		case PaddingMode.Zeros:
		{
			if (num2 == base.PaddingSizeBytes)
			{
				num2 = 0;
			}
			int num3 = length + num2;
			if (destination.Length < num3)
			{
				throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
			}
			block.CopyTo(destination);
			destination.Slice(length, num2).Clear();
			return num3;
		}
		default:
			throw new CryptographicException(System.SR.Cryptography_UnknownPaddingMode);
		}
	}
}
