using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal sealed class DesImplementation : DES
{
	public override ICryptoTransform CreateDecryptor()
	{
		return CreateTransform(Key, IV, encrypting: false);
	}

	public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: false);
	}

	public override ICryptoTransform CreateEncryptor()
	{
		return CreateTransform(Key, IV, encrypting: true);
	}

	public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: true);
	}

	public override void GenerateIV()
	{
		IV = RandomNumberGenerator.GetBytes(BlockSize / 8);
	}

	public sealed override void GenerateKey()
	{
		byte[] array = new byte[KeySize / 8];
		RandomNumberGenerator.Fill(array);
		while (DES.IsWeakKey(array) || DES.IsSemiWeakKey(array))
		{
			RandomNumberGenerator.Fill(array);
		}
		KeyValue = array;
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
		if (DES.IsWeakKey(rgbKey))
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidKey_Weak, "DES");
		}
		if (DES.IsSemiWeakKey(rgbKey))
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidKey_SemiWeak, "DES");
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
		return CreateTransformCore(Mode, Padding, rgbKey, rgbIV, BlockSize / 8, FeedbackSize / 8, this.GetPaddingSize(Mode, FeedbackSize), encrypting);
	}

	protected override bool TryDecryptEcbCore(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.ECB, paddingMode, Key, null, BlockSize / 8, 0, BlockSize / 8, encrypting: false);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptEcbCore(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.ECB, paddingMode, Key, null, BlockSize / 8, 0, BlockSize / 8, encrypting: true);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptCbcCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.CBC, paddingMode, Key, iv.ToArray(), BlockSize / 8, 0, BlockSize / 8, encrypting: true);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCbcCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.CBC, paddingMode, Key, iv.ToArray(), BlockSize / 8, 0, BlockSize / 8, encrypting: false);
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

	private static void ValidateCFBFeedbackSize(int feedback)
	{
		if (feedback != 8)
		{
			throw new CryptographicException(string.Format(System.SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
		}
	}

	private static UniversalCryptoTransform CreateTransformCore(CipherMode cipherMode, PaddingMode paddingMode, byte[] key, byte[] iv, int blockSize, int feedbackSize, int paddingSize, bool encrypting)
	{
		SafeAlgorithmHandle sharedHandle = DesBCryptModes.GetSharedHandle(cipherMode, feedbackSize);
		BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(sharedHandle, cipherMode, blockSize, paddingSize, key, ownsParentHandle: false, iv, encrypting);
		return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
	}
}
