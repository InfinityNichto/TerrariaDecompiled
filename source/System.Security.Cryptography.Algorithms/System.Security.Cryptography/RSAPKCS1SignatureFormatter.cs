using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public class RSAPKCS1SignatureFormatter : AsymmetricSignatureFormatter
{
	private RSA _rsaKey;

	private string _algName;

	public RSAPKCS1SignatureFormatter()
	{
	}

	public RSAPKCS1SignatureFormatter(AsymmetricAlgorithm key)
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

	public override void SetHashAlgorithm(string strName)
	{
		if (CryptoConfig.MapNameToOID(strName) != null)
		{
			_algName = HashAlgorithmNames.ToUpper(strName);
		}
		else
		{
			_algName = null;
		}
	}

	public override byte[] CreateSignature(byte[] rgbHash)
	{
		if (rgbHash == null)
		{
			throw new ArgumentNullException("rgbHash");
		}
		if (_algName == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_MissingOID);
		}
		if (_rsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_MissingKey);
		}
		return _rsaKey.SignHash(rgbHash, new HashAlgorithmName(_algName), RSASignaturePadding.Pkcs1);
	}
}
