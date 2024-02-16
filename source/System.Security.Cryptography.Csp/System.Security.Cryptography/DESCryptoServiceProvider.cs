using System.ComponentModel;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[Obsolete("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId = "SYSLIB0021", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DESCryptoServiceProvider : DES
{
	public DESCryptoServiceProvider()
	{
		FeedbackSizeValue = 8;
	}

	public override void GenerateKey()
	{
		byte[] array = new byte[8];
		RandomNumberGenerator.Fill(array);
		while (DES.IsWeakKey(array) || DES.IsSemiWeakKey(array))
		{
			RandomNumberGenerator.Fill(array);
		}
		KeyValue = array;
	}

	public override void GenerateIV()
	{
		IVValue = RandomNumberGenerator.GetBytes(8);
	}

	public override ICryptoTransform CreateDecryptor()
	{
		return CreateTransform(Key, IV, encrypting: false);
	}

	public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
	{
		return CreateTransform(rgbKey, rgbIV?.CloneByteArray(), encrypting: false);
	}

	public override ICryptoTransform CreateEncryptor()
	{
		return CreateTransform(Key, IV, encrypting: true);
	}

	public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
	{
		return CreateTransform(rgbKey, rgbIV?.CloneByteArray(), encrypting: true);
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
		Internal.Cryptography.BasicSymmetricCipher cipher = new BasicSymmetricCipherCsp(26113, Mode, BlockSize / 8, rgbKey, 0, addNoSaltFlag: false, rgbIV, encrypting, FeedbackSize, this.GetPaddingSize(Mode, FeedbackSize));
		return Internal.Cryptography.UniversalCryptoTransform.Create(Padding, cipher, encrypting);
	}
}
