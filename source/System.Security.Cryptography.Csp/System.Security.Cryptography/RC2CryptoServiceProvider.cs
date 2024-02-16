using System.ComponentModel;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[Obsolete("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId = "SYSLIB0021", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RC2CryptoServiceProvider : RC2
{
	private bool _use40bitSalt;

	private static readonly KeySizes[] s_legalKeySizes = new KeySizes[1]
	{
		new KeySizes(40, 128, 8)
	};

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

	public bool UseSalt
	{
		get
		{
			return _use40bitSalt;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			_use40bitSalt = value;
		}
	}

	public RC2CryptoServiceProvider()
	{
		LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();
		FeedbackSizeValue = 8;
	}

	public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
	{
		return CreateTransform(rgbKey, rgbIV?.CloneByteArray(), encrypting: true);
	}

	public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
	{
		return CreateTransform(rgbKey, rgbIV?.CloneByteArray(), encrypting: false);
	}

	public override void GenerateKey()
	{
		KeyValue = RandomNumberGenerator.GetBytes(KeySizeValue / 8);
	}

	public override void GenerateIV()
	{
		IVValue = RandomNumberGenerator.GetBytes(8);
	}

	private ICryptoTransform CreateTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting)
	{
		long num = (long)rgbKey.Length * 8L;
		if (num > int.MaxValue || !((int)num).IsLegalSize(LegalKeySizes))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidKeySize, "rgbKey");
		}
		if (rgbIV == null)
		{
			if (Mode.UsesIv())
			{
				rgbIV = RandomNumberGenerator.GetBytes(8);
			}
		}
		else if (rgbIV.Length < 8)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidIVSize);
		}
		int effectiveKeyLength = (int)((EffectiveKeySizeValue == 0) ? num : EffectiveKeySize);
		Internal.Cryptography.BasicSymmetricCipher cipher = new BasicSymmetricCipherCsp(26114, Mode, BlockSize / 8, rgbKey, effectiveKeyLength, !UseSalt, rgbIV, encrypting, 0, 0);
		return Internal.Cryptography.UniversalCryptoTransform.Create(Padding, cipher, encrypting);
	}
}
