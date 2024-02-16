using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public sealed class RSAEncryptionPadding : IEquatable<RSAEncryptionPadding>
{
	private static readonly RSAEncryptionPadding s_pkcs1 = new RSAEncryptionPadding(RSAEncryptionPaddingMode.Pkcs1, default(HashAlgorithmName));

	private static readonly RSAEncryptionPadding s_oaepSHA1 = CreateOaep(HashAlgorithmName.SHA1);

	private static readonly RSAEncryptionPadding s_oaepSHA256 = CreateOaep(HashAlgorithmName.SHA256);

	private static readonly RSAEncryptionPadding s_oaepSHA384 = CreateOaep(HashAlgorithmName.SHA384);

	private static readonly RSAEncryptionPadding s_oaepSHA512 = CreateOaep(HashAlgorithmName.SHA512);

	private readonly RSAEncryptionPaddingMode _mode;

	private readonly HashAlgorithmName _oaepHashAlgorithm;

	public static RSAEncryptionPadding Pkcs1 => s_pkcs1;

	public static RSAEncryptionPadding OaepSHA1 => s_oaepSHA1;

	public static RSAEncryptionPadding OaepSHA256 => s_oaepSHA256;

	public static RSAEncryptionPadding OaepSHA384 => s_oaepSHA384;

	public static RSAEncryptionPadding OaepSHA512 => s_oaepSHA512;

	public RSAEncryptionPaddingMode Mode => _mode;

	public HashAlgorithmName OaepHashAlgorithm => _oaepHashAlgorithm;

	private RSAEncryptionPadding(RSAEncryptionPaddingMode mode, HashAlgorithmName oaepHashAlgorithm)
	{
		_mode = mode;
		_oaepHashAlgorithm = oaepHashAlgorithm;
	}

	public static RSAEncryptionPadding CreateOaep(HashAlgorithmName hashAlgorithm)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		return new RSAEncryptionPadding(RSAEncryptionPaddingMode.Oaep, hashAlgorithm);
	}

	public override int GetHashCode()
	{
		return CombineHashCodes(_mode.GetHashCode(), _oaepHashAlgorithm.GetHashCode());
	}

	private static int CombineHashCodes(int h1, int h2)
	{
		return ((h1 << 5) + h1) ^ h2;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as RSAEncryptionPadding);
	}

	public bool Equals([NotNullWhen(true)] RSAEncryptionPadding? other)
	{
		if ((object)other != null && _mode == other._mode)
		{
			return _oaepHashAlgorithm == other._oaepHashAlgorithm;
		}
		return false;
	}

	public static bool operator ==(RSAEncryptionPadding? left, RSAEncryptionPadding? right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(RSAEncryptionPadding? left, RSAEncryptionPadding? right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return _mode.ToString() + _oaepHashAlgorithm.Name;
	}
}
