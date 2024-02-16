using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography;

public sealed class CngAlgorithm : IEquatable<CngAlgorithm>
{
	private static CngAlgorithm s_ecdh;

	private static CngAlgorithm s_ecdhp256;

	private static CngAlgorithm s_ecdhp384;

	private static CngAlgorithm s_ecdhp521;

	private static CngAlgorithm s_ecdsa;

	private static CngAlgorithm s_ecdsap256;

	private static CngAlgorithm s_ecdsap384;

	private static CngAlgorithm s_ecdsap521;

	private static CngAlgorithm s_md5;

	private static CngAlgorithm s_sha1;

	private static CngAlgorithm s_sha256;

	private static CngAlgorithm s_sha384;

	private static CngAlgorithm s_sha512;

	private static CngAlgorithm s_rsa;

	private readonly string _algorithm;

	public string Algorithm => _algorithm;

	public static CngAlgorithm Rsa => s_rsa ?? (s_rsa = new CngAlgorithm("RSA"));

	public static CngAlgorithm ECDiffieHellman => s_ecdh ?? (s_ecdh = new CngAlgorithm("ECDH"));

	public static CngAlgorithm ECDiffieHellmanP256 => s_ecdhp256 ?? (s_ecdhp256 = new CngAlgorithm("ECDH_P256"));

	public static CngAlgorithm ECDiffieHellmanP384 => s_ecdhp384 ?? (s_ecdhp384 = new CngAlgorithm("ECDH_P384"));

	public static CngAlgorithm ECDiffieHellmanP521 => s_ecdhp521 ?? (s_ecdhp521 = new CngAlgorithm("ECDH_P521"));

	public static CngAlgorithm ECDsa => s_ecdsa ?? (s_ecdsa = new CngAlgorithm("ECDSA"));

	public static CngAlgorithm ECDsaP256 => s_ecdsap256 ?? (s_ecdsap256 = new CngAlgorithm("ECDSA_P256"));

	public static CngAlgorithm ECDsaP384 => s_ecdsap384 ?? (s_ecdsap384 = new CngAlgorithm("ECDSA_P384"));

	public static CngAlgorithm ECDsaP521 => s_ecdsap521 ?? (s_ecdsap521 = new CngAlgorithm("ECDSA_P521"));

	public static CngAlgorithm MD5 => s_md5 ?? (s_md5 = new CngAlgorithm("MD5"));

	public static CngAlgorithm Sha1 => s_sha1 ?? (s_sha1 = new CngAlgorithm("SHA1"));

	public static CngAlgorithm Sha256 => s_sha256 ?? (s_sha256 = new CngAlgorithm("SHA256"));

	public static CngAlgorithm Sha384 => s_sha384 ?? (s_sha384 = new CngAlgorithm("SHA384"));

	public static CngAlgorithm Sha512 => s_sha512 ?? (s_sha512 = new CngAlgorithm("SHA512"));

	public CngAlgorithm(string algorithm)
	{
		if (algorithm == null)
		{
			throw new ArgumentNullException("algorithm");
		}
		if (algorithm.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_InvalidAlgorithmName, algorithm), "algorithm");
		}
		_algorithm = algorithm;
	}

	public static bool operator ==(CngAlgorithm? left, CngAlgorithm? right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(CngAlgorithm? left, CngAlgorithm? right)
	{
		if ((object)left == null)
		{
			return (object)right != null;
		}
		return !left.Equals(right);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as CngAlgorithm);
	}

	public bool Equals([NotNullWhen(true)] CngAlgorithm? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		return _algorithm.Equals(other.Algorithm);
	}

	public override int GetHashCode()
	{
		return _algorithm.GetHashCode();
	}

	public override string ToString()
	{
		return _algorithm;
	}
}
