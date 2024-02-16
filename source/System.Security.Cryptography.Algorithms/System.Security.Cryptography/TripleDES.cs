using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public abstract class TripleDES : SymmetricAlgorithm
{
	private static readonly KeySizes[] s_legalBlockSizes = new KeySizes[1]
	{
		new KeySizes(64, 64, 0)
	};

	private static readonly KeySizes[] s_legalKeySizes = new KeySizes[1]
	{
		new KeySizes(128, 192, 64)
	};

	public override byte[] Key
	{
		get
		{
			byte[] key = base.Key;
			while (IsWeakKey(key))
			{
				GenerateKey();
				key = base.Key;
			}
			return key;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (!(value.Length * 8).IsLegalSize(s_legalKeySizes))
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidKeySize);
			}
			if (IsWeakKey(value))
			{
				throw new CryptographicException(System.SR.Format(System.SR.Cryptography_InvalidKey_Weak, "TripleDES"));
			}
			base.Key = value;
		}
	}

	protected TripleDES()
	{
		KeySizeValue = 192;
		BlockSizeValue = 64;
		FeedbackSizeValue = BlockSizeValue;
		LegalBlockSizesValue = s_legalBlockSizes.CloneKeySizesArray();
		LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();
	}

	public new static TripleDES Create()
	{
		return new TripleDesImplementation();
	}

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static TripleDES? Create(string str)
	{
		return (TripleDES)CryptoConfig.CreateFromName(str);
	}

	public static bool IsWeakKey(byte[] rgbKey)
	{
		if (rgbKey == null)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidKeySize);
		}
		if (!(rgbKey.Length * 8).IsLegalSize(s_legalKeySizes))
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidKeySize);
		}
		byte[] array = rgbKey.FixupKeyParity();
		if (EqualBytes(array, 0, 8, 8))
		{
			return true;
		}
		if (array.Length == 24 && EqualBytes(array, 8, 16, 8))
		{
			return true;
		}
		return false;
	}

	private static bool EqualBytes(byte[] rgbKey, int start1, int start2, int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (rgbKey[start1 + i] != rgbKey[start2 + i])
			{
				return false;
			}
		}
		return true;
	}
}
