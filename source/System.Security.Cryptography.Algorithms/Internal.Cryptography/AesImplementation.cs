using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal sealed class AesImplementation : Aes
{
	public sealed override ICryptoTransform CreateDecryptor()
	{
		return CreateTransform(Key, IV, encrypting: false);
	}

	public sealed override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: false);
	}

	public sealed override ICryptoTransform CreateEncryptor()
	{
		return CreateTransform(Key, IV, encrypting: true);
	}

	public sealed override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: true);
	}

	public sealed override void GenerateIV()
	{
		IV = RandomNumberGenerator.GetBytes(BlockSize / 8);
	}

	public sealed override void GenerateKey()
	{
		Key = RandomNumberGenerator.GetBytes(KeySize / 8);
	}

	protected sealed override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}

	protected override bool TryDecryptEcbCore(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.ECB, paddingMode, Key, null, BlockSize / 8, BlockSize / 8, 0, encrypting: false);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptEcbCore(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.ECB, paddingMode, Key, null, BlockSize / 8, BlockSize / 8, 0, encrypting: true);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptCbcCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.CBC, paddingMode, Key, iv.ToArray(), BlockSize / 8, BlockSize / 8, 0, encrypting: true);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCbcCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.CBC, paddingMode, Key, iv.ToArray(), BlockSize / 8, BlockSize / 8, 0, encrypting: false);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCfbCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		ValidateCFBFeedbackSize(feedbackSizeInBits);
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.CFB, paddingMode, Key, iv.ToArray(), BlockSize / 8, feedbackSizeInBits / 8, feedbackSizeInBits / 8, encrypting: false);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptCfbCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		ValidateCFBFeedbackSize(feedbackSizeInBits);
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.CFB, paddingMode, Key, iv.ToArray(), BlockSize / 8, feedbackSizeInBits / 8, feedbackSizeInBits / 8, encrypting: true);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(plaintext, destination, out bytesWritten);
		}
	}

	private ICryptoTransform CreateTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting)
	{
		if (rgbKey == null)
		{
			throw new ArgumentNullException("rgbKey");
		}
		long num = (long)rgbKey.Length * 8L;
		if (num > int.MaxValue || !((int)num).IsLegalSize(LegalKeySizes))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidKeySize, "rgbKey");
		}
		if (rgbIV != null)
		{
			long num2 = (long)rgbIV.Length * 8L;
			if (num2 != BlockSize)
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidIVSize, "rgbIV");
			}
		}
		if (Mode == CipherMode.CFB)
		{
			ValidateCFBFeedbackSize(FeedbackSize);
		}
		return CreateTransformCore(Mode, Padding, rgbKey, rgbIV, BlockSize / 8, this.GetPaddingSize(Mode, FeedbackSize), FeedbackSize / 8, encrypting);
	}

	private static void ValidateCFBFeedbackSize(int feedback)
	{
		if (feedback != 8 && feedback != 128)
		{
			throw new CryptographicException(string.Format(System.SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
		}
	}

	private static UniversalCryptoTransform CreateTransformCore(CipherMode cipherMode, PaddingMode paddingMode, byte[] key, byte[] iv, int blockSize, int paddingSize, int feedbackSize, bool encrypting)
	{
		SafeAlgorithmHandle sharedHandle = AesBCryptModes.GetSharedHandle(cipherMode, feedbackSize);
		BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(sharedHandle, cipherMode, blockSize, paddingSize, key, ownsParentHandle: false, iv, encrypting);
		return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
	}
}
