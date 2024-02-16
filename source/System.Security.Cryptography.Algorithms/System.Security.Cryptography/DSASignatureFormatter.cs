using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public class DSASignatureFormatter : AsymmetricSignatureFormatter
{
	private DSA _dsaKey;

	public DSASignatureFormatter()
	{
	}

	public DSASignatureFormatter(AsymmetricAlgorithm key)
		: this()
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		_dsaKey = (DSA)key;
	}

	public override void SetKey(AsymmetricAlgorithm key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		_dsaKey = (DSA)key;
	}

	public override void SetHashAlgorithm(string strName)
	{
		if (strName.ToUpperInvariant() != "SHA1")
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_InvalidOperation);
		}
	}

	public override byte[] CreateSignature(byte[] rgbHash)
	{
		if (rgbHash == null)
		{
			throw new ArgumentNullException("rgbHash");
		}
		if (_dsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_MissingKey);
		}
		return _dsaKey.CreateSignature(rgbHash);
	}
}
