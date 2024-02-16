using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public abstract class AsymmetricKeyExchangeDeformatter
{
	public abstract string? Parameters { get; set; }

	public abstract void SetKey(AsymmetricAlgorithm key);

	public abstract byte[] DecryptKeyExchange(byte[] rgb);
}
