using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public abstract class AsymmetricSignatureFormatter
{
	public abstract void SetKey(AsymmetricAlgorithm key);

	public abstract void SetHashAlgorithm(string strName);

	public virtual byte[] CreateSignature(HashAlgorithm hash)
	{
		if (hash == null)
		{
			throw new ArgumentNullException("hash");
		}
		SetHashAlgorithm(hash.ToAlgorithmName());
		return CreateSignature(hash.Hash);
	}

	public abstract byte[] CreateSignature(byte[] rgbHash);
}
