using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public class RSAOAEPKeyExchangeFormatter : AsymmetricKeyExchangeFormatter
{
	private byte[] ParameterValue;

	private RSA _rsaKey;

	private RandomNumberGenerator RngValue;

	public byte[]? Parameter
	{
		get
		{
			if (ParameterValue != null)
			{
				return (byte[])ParameterValue.Clone();
			}
			return null;
		}
		set
		{
			if (value != null)
			{
				ParameterValue = (byte[])value.Clone();
			}
			else
			{
				ParameterValue = null;
			}
		}
	}

	public override string? Parameters => null;

	public RandomNumberGenerator? Rng
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

	public RSAOAEPKeyExchangeFormatter()
	{
	}

	public RSAOAEPKeyExchangeFormatter(AsymmetricAlgorithm key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		_rsaKey = (RSA)key;
	}

	public override void SetKey(AsymmetricAlgorithm key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		_rsaKey = (RSA)key;
	}

	public override byte[] CreateKeyExchange(byte[] rgbData, Type? symAlgType)
	{
		return CreateKeyExchange(rgbData);
	}

	public override byte[] CreateKeyExchange(byte[] rgbData)
	{
		if (_rsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_MissingKey);
		}
		return _rsaKey.Encrypt(rgbData, RSAEncryptionPadding.OaepSHA1);
	}
}
