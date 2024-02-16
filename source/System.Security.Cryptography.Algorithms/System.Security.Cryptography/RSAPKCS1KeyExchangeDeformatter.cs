using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public class RSAPKCS1KeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter
{
	private RSA _rsaKey;

	private RandomNumberGenerator RngValue;

	public RandomNumberGenerator? RNG
	{
		get
		{
			return RngValue;
		}
		set
		{
			RngValue = value;
		}
	}

	public override string? Parameters
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public RSAPKCS1KeyExchangeDeformatter()
	{
	}

	public RSAPKCS1KeyExchangeDeformatter(AsymmetricAlgorithm key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		_rsaKey = (RSA)key;
	}

	public override byte[] DecryptKeyExchange(byte[] rgbIn)
	{
		if (_rsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_MissingKey);
		}
		return _rsaKey.Decrypt(rgbIn, RSAEncryptionPadding.Pkcs1);
	}

	public override void SetKey(AsymmetricAlgorithm key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		_rsaKey = (RSA)key;
	}
}
