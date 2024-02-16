using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal sealed class RC2Implementation : RC2
{
	public override int EffectiveKeySize
	{
		get
		{
			return KeySizeValue;
		}
		set
		{
			if (value != KeySizeValue)
			{
				throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_RC2_EKSKS2);
			}
		}
	}

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
		Key = RandomNumberGenerator.GetBytes(KeySize / 8);
	}

	private ICryptoTransform CreateTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting)
	{
		if (rgbKey == null)
		{
			throw new ArgumentNullException("rgbKey");
		}
		if (!ValidKeySize(rgbKey.Length, out var keySizeBits))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidKeySize, "rgbKey");
		}
		if (rgbIV != null)
		{
			long num = (long)rgbIV.Length * 8L;
			if (num != BlockSize)
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidIVSize, "rgbIV");
			}
		}
		if (Mode == CipherMode.CFB)
		{
			ValidateCFBFeedbackSize(FeedbackSize);
		}
		int effectiveKeyLength = ((EffectiveKeySizeValue == 0) ? keySizeBits : EffectiveKeySize);
		return CreateTransformCore(Mode, Padding, rgbKey, effectiveKeyLength, rgbIV, BlockSize / 8, FeedbackSize / 8, GetPaddingSize(), encrypting);
	}

	protected override bool TryDecryptEcbCore(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		if (!ValidKeySize(Key.Length, out var keySizeBits))
		{
			throw new InvalidOperationException(System.SR.Cryptography_InvalidKeySize);
		}
		int effectiveKeyLength = ((EffectiveKeySizeValue == 0) ? keySizeBits : EffectiveKeySize);
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.ECB, paddingMode, Key, effectiveKeyLength, null, BlockSize / 8, 0, BlockSize / 8, encrypting: false);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptEcbCore(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		if (!ValidKeySize(Key.Length, out var keySizeBits))
		{
			throw new InvalidOperationException(System.SR.Cryptography_InvalidKeySize);
		}
		int effectiveKeyLength = ((EffectiveKeySizeValue == 0) ? keySizeBits : EffectiveKeySize);
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.ECB, paddingMode, Key, effectiveKeyLength, null, BlockSize / 8, 0, BlockSize / 8, encrypting: true);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptCbcCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		if (!ValidKeySize(Key.Length, out var keySizeBits))
		{
			throw new InvalidOperationException(System.SR.Cryptography_InvalidKeySize);
		}
		int effectiveKeyLength = ((EffectiveKeySizeValue == 0) ? keySizeBits : EffectiveKeySize);
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.CBC, paddingMode, Key, effectiveKeyLength, iv.ToArray(), BlockSize / 8, 0, BlockSize / 8, encrypting: true);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCbcCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		if (!ValidKeySize(Key.Length, out var keySizeBits))
		{
			throw new InvalidOperationException(System.SR.Cryptography_InvalidKeySize);
		}
		int effectiveKeyLength = ((EffectiveKeySizeValue == 0) ? keySizeBits : EffectiveKeySize);
		UniversalCryptoTransform universalCryptoTransform = CreateTransformCore(CipherMode.CBC, paddingMode, Key, effectiveKeyLength, iv.ToArray(), BlockSize / 8, 0, BlockSize / 8, encrypting: false);
		using (universalCryptoTransform)
		{
			return universalCryptoTransform.TransformOneShot(ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCfbCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CipherModeNotSupported, CipherMode.CFB));
	}

	protected override bool TryEncryptCfbCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CipherModeNotSupported, CipherMode.CFB));
	}

	private static void ValidateCFBFeedbackSize(int feedback)
	{
		throw new CryptographicException(string.Format(System.SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
	}

	private int GetPaddingSize()
	{
		return BlockSize / 8;
	}

	private bool ValidKeySize(int keySizeBytes, out int keySizeBits)
	{
		if (keySizeBytes > 268435455)
		{
			keySizeBits = 0;
			return false;
		}
		keySizeBits = keySizeBytes << 3;
		return keySizeBits.IsLegalSize(LegalKeySizes);
	}

	private static UniversalCryptoTransform CreateTransformCore(CipherMode cipherMode, PaddingMode paddingMode, byte[] key, int effectiveKeyLength, byte[] iv, int blockSize, int feedbackSize, int paddingSize, bool encrypting)
	{
		using SafeAlgorithmHandle algorithm = RC2BCryptModes.GetHandle(cipherMode, effectiveKeyLength);
		BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(algorithm, cipherMode, blockSize, paddingSize, key, ownsParentHandle: true, iv, encrypting);
		return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
	}
}
