using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography;

public abstract class SymmetricAlgorithm : IDisposable
{
	protected CipherMode ModeValue;

	protected PaddingMode PaddingValue;

	protected byte[]? KeyValue;

	protected byte[]? IVValue;

	protected int BlockSizeValue;

	protected int FeedbackSizeValue;

	protected int KeySizeValue;

	[MaybeNull]
	protected KeySizes[] LegalBlockSizesValue;

	[MaybeNull]
	protected KeySizes[] LegalKeySizesValue;

	public virtual int FeedbackSize
	{
		get
		{
			return FeedbackSizeValue;
		}
		set
		{
			if (value <= 0 || value > BlockSizeValue || value % 8 != 0)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidFeedbackSize);
			}
			FeedbackSizeValue = value;
		}
	}

	public virtual int BlockSize
	{
		get
		{
			return BlockSizeValue;
		}
		set
		{
			if (!value.IsLegalSize(LegalBlockSizes, out var validatedByZeroSkipSizeKeySizes))
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidBlockSize);
			}
			if (BlockSizeValue != value || validatedByZeroSkipSizeKeySizes)
			{
				BlockSizeValue = value;
				IVValue = null;
			}
		}
	}

	public virtual byte[] IV
	{
		get
		{
			if (IVValue == null)
			{
				GenerateIV();
			}
			return IVValue.CloneByteArray();
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length != BlockSize / 8)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidIVSize);
			}
			IVValue = value.CloneByteArray();
		}
	}

	public virtual byte[] Key
	{
		get
		{
			if (KeyValue == null)
			{
				GenerateKey();
			}
			return KeyValue.CloneByteArray();
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			long num = (long)value.Length * 8L;
			if (num > int.MaxValue || !ValidKeySize((int)num))
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidKeySize);
			}
			KeySize = (int)num;
			KeyValue = value.CloneByteArray();
		}
	}

	public virtual int KeySize
	{
		get
		{
			return KeySizeValue;
		}
		set
		{
			if (!ValidKeySize(value))
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidKeySize);
			}
			KeySizeValue = value;
			KeyValue = null;
		}
	}

	public virtual KeySizes[] LegalBlockSizes => (KeySizes[])LegalBlockSizesValue.Clone();

	public virtual KeySizes[] LegalKeySizes => (KeySizes[])LegalKeySizesValue.Clone();

	public virtual CipherMode Mode
	{
		get
		{
			return ModeValue;
		}
		set
		{
			if (value != CipherMode.CBC && value != CipherMode.ECB && value != CipherMode.CFB)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidCipherMode);
			}
			ModeValue = value;
		}
	}

	public virtual PaddingMode Padding
	{
		get
		{
			return PaddingValue;
		}
		set
		{
			if (value < PaddingMode.None || value > PaddingMode.ISO10126)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidPaddingMode);
			}
			PaddingValue = value;
		}
	}

	protected SymmetricAlgorithm()
	{
		ModeValue = CipherMode.CBC;
		PaddingValue = PaddingMode.PKCS7;
	}

	[Obsolete("The default implementation of this cryptography algorithm is not supported.", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static SymmetricAlgorithm Create()
	{
		throw new PlatformNotSupportedException(System.SR.Cryptography_DefaultAlgorithm_NotSupported);
	}

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public static SymmetricAlgorithm? Create(string algName)
	{
		return (SymmetricAlgorithm)CryptoConfigForwarder.CreateFromName(algName);
	}

	public virtual ICryptoTransform CreateDecryptor()
	{
		return CreateDecryptor(Key, IV);
	}

	public abstract ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV);

	public virtual ICryptoTransform CreateEncryptor()
	{
		return CreateEncryptor(Key, IV);
	}

	public abstract ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV);

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void Clear()
	{
		((IDisposable)this).Dispose();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (KeyValue != null)
			{
				Array.Clear(KeyValue);
				KeyValue = null;
			}
			if (IVValue != null)
			{
				Array.Clear(IVValue);
				IVValue = null;
			}
		}
	}

	public abstract void GenerateIV();

	public abstract void GenerateKey();

	public bool ValidKeySize(int bitLength)
	{
		KeySizes[] legalKeySizes = LegalKeySizes;
		if (legalKeySizes == null)
		{
			return false;
		}
		return bitLength.IsLegalSize(legalKeySizes);
	}

	public int GetCiphertextLengthEcb(int plaintextLength, PaddingMode paddingMode)
	{
		return GetCiphertextLengthBlockAligned(plaintextLength, paddingMode);
	}

	public int GetCiphertextLengthCbc(int plaintextLength, PaddingMode paddingMode = PaddingMode.PKCS7)
	{
		return GetCiphertextLengthBlockAligned(plaintextLength, paddingMode);
	}

	private int GetCiphertextLengthBlockAligned(int plaintextLength, PaddingMode paddingMode)
	{
		if (plaintextLength < 0)
		{
			throw new ArgumentOutOfRangeException("plaintextLength", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		int blockSize = BlockSize;
		if (blockSize <= 0 || ((uint)blockSize & 7u) != 0)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_UnsupportedBlockSize);
		}
		int num = blockSize >> 3;
		int result;
		int num2 = Math.DivRem(plaintextLength, num, out result) * num;
		switch (paddingMode)
		{
		case PaddingMode.None:
			if (result != 0)
			{
				throw new ArgumentException(System.SR.Cryptography_MatchBlockSize, "plaintextLength");
			}
			goto IL_0077;
		case PaddingMode.Zeros:
			if (result == 0)
			{
				goto IL_0077;
			}
			goto case PaddingMode.PKCS7;
		case PaddingMode.PKCS7:
		case PaddingMode.ANSIX923:
		case PaddingMode.ISO10126:
			if (int.MaxValue - num2 < num)
			{
				throw new ArgumentOutOfRangeException("plaintextLength", System.SR.Cryptography_PlaintextTooLarge);
			}
			return num2 + num;
		default:
			{
				throw new ArgumentOutOfRangeException("paddingMode", System.SR.Cryptography_InvalidPaddingMode);
			}
			IL_0077:
			return plaintextLength;
		}
	}

	public int GetCiphertextLengthCfb(int plaintextLength, PaddingMode paddingMode = PaddingMode.None, int feedbackSizeInBits = 8)
	{
		if (plaintextLength < 0)
		{
			throw new ArgumentOutOfRangeException("plaintextLength", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (feedbackSizeInBits <= 0)
		{
			throw new ArgumentOutOfRangeException("feedbackSizeInBits", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (((uint)feedbackSizeInBits & 7u) != 0)
		{
			throw new ArgumentException(System.SR.Argument_BitsMustBeWholeBytes, "feedbackSizeInBits");
		}
		int num = feedbackSizeInBits >> 3;
		int result;
		int num2 = Math.DivRem(plaintextLength, num, out result) * num;
		switch (paddingMode)
		{
		case PaddingMode.None:
			if (result != 0)
			{
				throw new ArgumentException(System.SR.Cryptography_MatchFeedbackSize, "plaintextLength");
			}
			goto IL_0083;
		case PaddingMode.Zeros:
			if (result == 0)
			{
				goto IL_0083;
			}
			goto case PaddingMode.PKCS7;
		case PaddingMode.PKCS7:
		case PaddingMode.ANSIX923:
		case PaddingMode.ISO10126:
			if (int.MaxValue - num2 < num)
			{
				throw new ArgumentOutOfRangeException("plaintextLength", System.SR.Cryptography_PlaintextTooLarge);
			}
			return num2 + num;
		default:
			{
				throw new ArgumentOutOfRangeException("paddingMode", System.SR.Cryptography_InvalidPaddingMode);
			}
			IL_0083:
			return plaintextLength;
		}
	}

	public byte[] DecryptEcb(byte[] ciphertext, PaddingMode paddingMode)
	{
		if (ciphertext == null)
		{
			throw new ArgumentNullException("ciphertext");
		}
		return DecryptEcb(new ReadOnlySpan<byte>(ciphertext), paddingMode);
	}

	public byte[] DecryptEcb(ReadOnlySpan<byte> ciphertext, PaddingMode paddingMode)
	{
		CheckPaddingMode(paddingMode);
		byte[] array = GC.AllocateUninitializedArray<byte>(ciphertext.Length);
		if (!TryDecryptEcbCore(ciphertext, array, paddingMode, out var bytesWritten) || (uint)bytesWritten > array.Length)
		{
			throw new CryptographicException(System.SR.Argument_DestinationTooShort);
		}
		Array.Resize(ref array, bytesWritten);
		return array;
	}

	public int DecryptEcb(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode)
	{
		CheckPaddingMode(paddingMode);
		if (!TryDecryptEcbCore(ciphertext, destination, paddingMode, out var bytesWritten))
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return bytesWritten;
	}

	public bool TryDecryptEcb(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		CheckPaddingMode(paddingMode);
		return TryDecryptEcbCore(ciphertext, destination, paddingMode, out bytesWritten);
	}

	public byte[] EncryptEcb(byte[] plaintext, PaddingMode paddingMode)
	{
		if (plaintext == null)
		{
			throw new ArgumentNullException("plaintext");
		}
		return EncryptEcb(new ReadOnlySpan<byte>(plaintext), paddingMode);
	}

	public byte[] EncryptEcb(ReadOnlySpan<byte> plaintext, PaddingMode paddingMode)
	{
		CheckPaddingMode(paddingMode);
		int ciphertextLengthEcb = GetCiphertextLengthEcb(plaintext.Length, paddingMode);
		byte[] array = GC.AllocateUninitializedArray<byte>(ciphertextLengthEcb);
		if (!TryEncryptEcbCore(plaintext, array, paddingMode, out var bytesWritten) || bytesWritten != ciphertextLengthEcb)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_EncryptedIncorrectLength, "TryEncryptEcbCore"));
		}
		return array;
	}

	public int EncryptEcb(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode)
	{
		CheckPaddingMode(paddingMode);
		if (!TryEncryptEcbCore(plaintext, destination, paddingMode, out var bytesWritten))
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return bytesWritten;
	}

	public bool TryEncryptEcb(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		CheckPaddingMode(paddingMode);
		return TryEncryptEcbCore(plaintext, destination, paddingMode, out bytesWritten);
	}

	public byte[] DecryptCbc(byte[] ciphertext, byte[] iv, PaddingMode paddingMode = PaddingMode.PKCS7)
	{
		if (ciphertext == null)
		{
			throw new ArgumentNullException("ciphertext");
		}
		if (iv == null)
		{
			throw new ArgumentNullException("iv");
		}
		return DecryptCbc(new ReadOnlySpan<byte>(ciphertext), new ReadOnlySpan<byte>(iv), paddingMode);
	}

	public byte[] DecryptCbc(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, PaddingMode paddingMode = PaddingMode.PKCS7)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		byte[] array = ArrayPool<byte>.Shared.Rent(ciphertext.Length);
		Span<byte> destination = array.AsSpan(0, ciphertext.Length);
		if (!TryDecryptCbcCore(ciphertext, iv, destination, paddingMode, out var bytesWritten) || (uint)bytesWritten > destination.Length)
		{
			throw new CryptographicException(System.SR.Argument_DestinationTooShort);
		}
		byte[] result = destination.Slice(0, bytesWritten).ToArray();
		CryptographicOperations.ZeroMemory(destination.Slice(0, bytesWritten));
		ArrayPool<byte>.Shared.Return(array);
		return result;
	}

	public int DecryptCbc(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode = PaddingMode.PKCS7)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		if (!TryDecryptCbcCore(ciphertext, iv, destination, paddingMode, out var bytesWritten))
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return bytesWritten;
	}

	public bool TryDecryptCbc(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten, PaddingMode paddingMode = PaddingMode.PKCS7)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		return TryDecryptCbcCore(ciphertext, iv, destination, paddingMode, out bytesWritten);
	}

	public byte[] EncryptCbc(byte[] plaintext, byte[] iv, PaddingMode paddingMode = PaddingMode.PKCS7)
	{
		if (plaintext == null)
		{
			throw new ArgumentNullException("plaintext");
		}
		if (iv == null)
		{
			throw new ArgumentNullException("iv");
		}
		return EncryptCbc(new ReadOnlySpan<byte>(plaintext), new ReadOnlySpan<byte>(iv), paddingMode);
	}

	public byte[] EncryptCbc(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, PaddingMode paddingMode = PaddingMode.PKCS7)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		int ciphertextLengthCbc = GetCiphertextLengthCbc(plaintext.Length, paddingMode);
		byte[] array = GC.AllocateUninitializedArray<byte>(ciphertextLengthCbc);
		if (!TryEncryptCbcCore(plaintext, iv, array, paddingMode, out var bytesWritten) || bytesWritten != ciphertextLengthCbc)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_EncryptedIncorrectLength, "TryEncryptCbcCore"));
		}
		return array;
	}

	public int EncryptCbc(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode = PaddingMode.PKCS7)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		if (!TryEncryptCbcCore(plaintext, iv, destination, paddingMode, out var bytesWritten))
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return bytesWritten;
	}

	public bool TryEncryptCbc(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten, PaddingMode paddingMode = PaddingMode.PKCS7)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		return TryEncryptCbcCore(plaintext, iv, destination, paddingMode, out bytesWritten);
	}

	public byte[] DecryptCfb(byte[] ciphertext, byte[] iv, PaddingMode paddingMode = PaddingMode.None, int feedbackSizeInBits = 8)
	{
		if (ciphertext == null)
		{
			throw new ArgumentNullException("ciphertext");
		}
		if (iv == null)
		{
			throw new ArgumentNullException("iv");
		}
		return DecryptCfb(new ReadOnlySpan<byte>(ciphertext), new ReadOnlySpan<byte>(iv), paddingMode, feedbackSizeInBits);
	}

	public byte[] DecryptCfb(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, PaddingMode paddingMode = PaddingMode.None, int feedbackSizeInBits = 8)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		CheckFeedbackSize(feedbackSizeInBits);
		byte[] array = GC.AllocateUninitializedArray<byte>(ciphertext.Length);
		if (!TryDecryptCfbCore(ciphertext, iv, array, paddingMode, feedbackSizeInBits, out var bytesWritten) || (uint)bytesWritten > array.Length)
		{
			throw new CryptographicException(System.SR.Argument_DestinationTooShort);
		}
		Array.Resize(ref array, bytesWritten);
		return array;
	}

	public int DecryptCfb(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode = PaddingMode.None, int feedbackSizeInBits = 8)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		CheckFeedbackSize(feedbackSizeInBits);
		if (!TryDecryptCfbCore(ciphertext, iv, destination, paddingMode, feedbackSizeInBits, out var bytesWritten))
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return bytesWritten;
	}

	public bool TryDecryptCfb(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten, PaddingMode paddingMode = PaddingMode.None, int feedbackSizeInBits = 8)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		CheckFeedbackSize(feedbackSizeInBits);
		return TryDecryptCfbCore(ciphertext, iv, destination, paddingMode, feedbackSizeInBits, out bytesWritten);
	}

	public byte[] EncryptCfb(byte[] plaintext, byte[] iv, PaddingMode paddingMode = PaddingMode.None, int feedbackSizeInBits = 8)
	{
		return EncryptCfb(new ReadOnlySpan<byte>(plaintext), new ReadOnlySpan<byte>(iv), paddingMode, feedbackSizeInBits);
	}

	public byte[] EncryptCfb(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, PaddingMode paddingMode = PaddingMode.None, int feedbackSizeInBits = 8)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		CheckFeedbackSize(feedbackSizeInBits);
		int ciphertextLengthCfb = GetCiphertextLengthCfb(plaintext.Length, paddingMode, feedbackSizeInBits);
		byte[] array = GC.AllocateUninitializedArray<byte>(ciphertextLengthCfb);
		if (!TryEncryptCfbCore(plaintext, iv, array, paddingMode, feedbackSizeInBits, out var bytesWritten) || bytesWritten != ciphertextLengthCfb)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_EncryptedIncorrectLength, "TryEncryptCfbCore"));
		}
		return array;
	}

	public int EncryptCfb(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode = PaddingMode.None, int feedbackSizeInBits = 8)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		CheckFeedbackSize(feedbackSizeInBits);
		if (!TryEncryptCfbCore(plaintext, iv, destination, paddingMode, feedbackSizeInBits, out var bytesWritten))
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return bytesWritten;
	}

	public bool TryEncryptCfb(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten, PaddingMode paddingMode = PaddingMode.None, int feedbackSizeInBits = 8)
	{
		CheckPaddingMode(paddingMode);
		CheckInitializationVectorSize(iv);
		CheckFeedbackSize(feedbackSizeInBits);
		return TryEncryptCfbCore(plaintext, iv, destination, paddingMode, feedbackSizeInBits, out bytesWritten);
	}

	protected virtual bool TryEncryptEcbCore(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	protected virtual bool TryDecryptEcbCore(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	protected virtual bool TryEncryptCbcCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	protected virtual bool TryDecryptCbcCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	protected virtual bool TryDecryptCfbCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	protected virtual bool TryEncryptCfbCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	private static void CheckPaddingMode(PaddingMode paddingMode)
	{
		if (paddingMode < PaddingMode.None || paddingMode > PaddingMode.ISO10126)
		{
			throw new ArgumentOutOfRangeException("paddingMode", System.SR.Cryptography_InvalidPaddingMode);
		}
	}

	private void CheckInitializationVectorSize(ReadOnlySpan<byte> iv)
	{
		if (iv.Length != BlockSize >> 3)
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidIVSize, "iv");
		}
	}

	private void CheckFeedbackSize(int feedbackSizeInBits)
	{
		if (feedbackSizeInBits < 8 || ((uint)feedbackSizeInBits & 7u) != 0 || feedbackSizeInBits > BlockSize)
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidFeedbackSize, "feedbackSizeInBits");
		}
	}
}
