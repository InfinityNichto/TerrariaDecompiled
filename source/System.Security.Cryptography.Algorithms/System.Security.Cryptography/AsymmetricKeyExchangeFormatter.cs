using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public abstract class AsymmetricKeyExchangeFormatter
{
	public abstract string? Parameters { get; }

	public abstract void SetKey(AsymmetricAlgorithm key);

	public abstract byte[] CreateKeyExchange(byte[] data);

	public abstract byte[] CreateKeyExchange(byte[] data, Type? symAlgType);
}
