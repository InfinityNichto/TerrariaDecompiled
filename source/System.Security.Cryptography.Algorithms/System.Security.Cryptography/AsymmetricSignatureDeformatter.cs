using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public abstract class AsymmetricSignatureDeformatter
{
	public abstract void SetKey(AsymmetricAlgorithm key);

	public abstract void SetHashAlgorithm(string strName);

	public virtual bool VerifySignature(HashAlgorithm hash, byte[] rgbSignature)
	{
		if (hash == null)
		{
			throw new ArgumentNullException("hash");
		}
		SetHashAlgorithm(hash.ToAlgorithmName());
		return VerifySignature(hash.Hash, rgbSignature);
	}

	public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
}
