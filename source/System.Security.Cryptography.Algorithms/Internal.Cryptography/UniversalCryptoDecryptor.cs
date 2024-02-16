using System;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal sealed class UniversalCryptoDecryptor : UniversalCryptoTransform
{
	private byte[] _heldoverCipher;

	private bool DepaddingRequired
	{
		get
		{
			switch (base.PaddingMode)
			{
			case PaddingMode.PKCS7:
			case PaddingMode.ANSIX923:
			case PaddingMode.ISO10126:
				return true;
			case PaddingMode.None:
			case PaddingMode.Zeros:
				return false;
			default:
				throw new CryptographicException(System.SR.Cryptography_UnknownPaddingMode);
			}
		}
	}

	public UniversalCryptoDecryptor(PaddingMode paddingMode, BasicSymmetricCipher basicSymmetricCipher)
		: base(paddingMode, basicSymmetricCipher)
	{
	}

	protected override int UncheckedTransformBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
	{
		int num = 0;
		if (DepaddingRequired)
		{
			if (_heldoverCipher != null)
			{
				int num2 = base.BasicSymmetricCipher.Transform(_heldoverCipher, outputBuffer);
				outputBuffer = outputBuffer.Slice(num2);
				num += num2;
			}
			else
			{
				_heldoverCipher = new byte[base.InputBlockSize];
			}
			inputBuffer.Slice(inputBuffer.Length - _heldoverCipher.Length).CopyTo(_heldoverCipher);
			inputBuffer = inputBuffer.Slice(0, inputBuffer.Length - _heldoverCipher.Length);
		}
		if (inputBuffer.Length > 0)
		{
			num += base.BasicSymmetricCipher.Transform(inputBuffer, outputBuffer);
		}
		return num;
	}

	protected unsafe override int UncheckedTransformFinalBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
	{
		if (inputBuffer.Length % base.PaddingSizeBytes != 0)
		{
			throw new CryptographicException(System.SR.Cryptography_PartialBlock);
		}
		byte[] array = null;
		int num = 0;
		try
		{
			Span<byte> span;
			ReadOnlySpan<byte> input;
			if (_heldoverCipher == null)
			{
				num = inputBuffer.Length;
				array = System.Security.Cryptography.CryptoPool.Rent(inputBuffer.Length);
				span = array.AsSpan(0, inputBuffer.Length);
				input = inputBuffer;
			}
			else
			{
				num = _heldoverCipher.Length + inputBuffer.Length;
				array = System.Security.Cryptography.CryptoPool.Rent(num);
				span = array.AsSpan(0, num);
				_heldoverCipher.AsSpan().CopyTo(span);
				inputBuffer.CopyTo(span.Slice(_heldoverCipher.Length));
				input = span;
			}
			int num2 = 0;
			fixed (byte* ptr = span)
			{
				Span<byte> span2 = span[..base.BasicSymmetricCipher.TransformFinal(input, span)];
				if (span2.Length > 0)
				{
					num2 = GetPaddingLength(span2);
					span2.Slice(0, num2).CopyTo(outputBuffer);
				}
			}
			Reset();
			return num2;
		}
		finally
		{
			if (array != null)
			{
				System.Security.Cryptography.CryptoPool.Return(array, num);
			}
		}
	}

	protected unsafe override byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		if (DepaddingRequired)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(inputCount + base.InputBlockSize);
			int num = 0;
			fixed (byte* ptr = array)
			{
				try
				{
					num = UncheckedTransformFinalBlock(inputBuffer.AsSpan(inputOffset, inputCount), array);
					return array.AsSpan(0, num).ToArray();
				}
				finally
				{
					System.Security.Cryptography.CryptoPool.Return(array, num);
				}
			}
		}
		byte[] array2 = GC.AllocateUninitializedArray<byte>(inputCount);
		int num2 = UncheckedTransformFinalBlock(inputBuffer.AsSpan(inputOffset, inputCount), array2);
		return array2;
	}

	protected sealed override void Dispose(bool disposing)
	{
		if (disposing)
		{
			byte[] heldoverCipher = _heldoverCipher;
			_heldoverCipher = null;
			if (heldoverCipher != null)
			{
				Array.Clear(heldoverCipher);
			}
		}
		base.Dispose(disposing);
	}

	public unsafe override bool TransformOneShot(ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
	{
		if (input.Length % base.PaddingSizeBytes != 0)
		{
			throw new CryptographicException(System.SR.Cryptography_PartialBlock);
		}
		if (!DepaddingRequired)
		{
			if (output.Length >= input.Length)
			{
				bytesWritten = base.BasicSymmetricCipher.TransformFinal(input, output);
				return true;
			}
			bytesWritten = 0;
			return false;
		}
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(input.Length);
		Span<byte> output2 = array.AsSpan(0, input.Length);
		Span<byte> span = default(Span<byte>);
		fixed (byte* ptr = output2)
		{
			try
			{
				span = output2[..base.BasicSymmetricCipher.TransformFinal(input, output2)];
				int paddingLength = GetPaddingLength(span);
				if (paddingLength > output.Length)
				{
					bytesWritten = 0;
					return false;
				}
				span.Slice(0, paddingLength).CopyTo(output);
				bytesWritten = paddingLength;
				return true;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(span);
				System.Security.Cryptography.CryptoPool.Return(array, 0);
			}
		}
	}

	private void Reset()
	{
		if (_heldoverCipher != null)
		{
			Array.Clear(_heldoverCipher);
			_heldoverCipher = null;
		}
	}

	private int GetPaddingLength(ReadOnlySpan<byte> block)
	{
		int num = 0;
		switch (base.PaddingMode)
		{
		case PaddingMode.ANSIX923:
		{
			num = block[^1];
			if (num <= 0 || num > base.InputBlockSize)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidPadding);
			}
			for (int j = block.Length - num; j < block.Length - 1; j++)
			{
				if (block[j] != 0)
				{
					throw new CryptographicException(System.SR.Cryptography_InvalidPadding);
				}
			}
			break;
		}
		case PaddingMode.ISO10126:
			num = block[^1];
			if (num <= 0 || num > base.InputBlockSize)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidPadding);
			}
			break;
		case PaddingMode.PKCS7:
		{
			num = block[^1];
			if (num <= 0 || num > base.InputBlockSize)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidPadding);
			}
			for (int i = block.Length - num; i < block.Length - 1; i++)
			{
				if (block[i] != num)
				{
					throw new CryptographicException(System.SR.Cryptography_InvalidPadding);
				}
			}
			break;
		}
		case PaddingMode.None:
		case PaddingMode.Zeros:
			num = 0;
			break;
		default:
			throw new CryptographicException(System.SR.Cryptography_UnknownPaddingMode);
		}
		return block.Length - num;
	}
}
