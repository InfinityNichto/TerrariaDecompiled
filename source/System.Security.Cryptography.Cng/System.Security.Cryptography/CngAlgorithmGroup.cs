using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography;

public sealed class CngAlgorithmGroup : IEquatable<CngAlgorithmGroup>
{
	private static CngAlgorithmGroup s_dh;

	private static CngAlgorithmGroup s_dsa;

	private static CngAlgorithmGroup s_ecdh;

	private static CngAlgorithmGroup s_ecdsa;

	private static CngAlgorithmGroup s_rsa;

	private readonly string _algorithmGroup;

	public string AlgorithmGroup => _algorithmGroup;

	public static CngAlgorithmGroup DiffieHellman => s_dh ?? (s_dh = new CngAlgorithmGroup("DH"));

	public static CngAlgorithmGroup Dsa => s_dsa ?? (s_dsa = new CngAlgorithmGroup("DSA"));

	public static CngAlgorithmGroup ECDiffieHellman => s_ecdh ?? (s_ecdh = new CngAlgorithmGroup("ECDH"));

	public static CngAlgorithmGroup ECDsa => s_ecdsa ?? (s_ecdsa = new CngAlgorithmGroup("ECDSA"));

	public static CngAlgorithmGroup Rsa => s_rsa ?? (s_rsa = new CngAlgorithmGroup("RSA"));

	public CngAlgorithmGroup(string algorithmGroup)
	{
		if (algorithmGroup == null)
		{
			throw new ArgumentNullException("algorithmGroup");
		}
		if (algorithmGroup.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_InvalidAlgorithmGroup, algorithmGroup), "algorithmGroup");
		}
		_algorithmGroup = algorithmGroup;
	}

	public static bool operator ==(CngAlgorithmGroup? left, CngAlgorithmGroup? right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(CngAlgorithmGroup? left, CngAlgorithmGroup? right)
	{
		if ((object)left == null)
		{
			return (object)right != null;
		}
		return !left.Equals(right);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as CngAlgorithmGroup);
	}

	public bool Equals([NotNullWhen(true)] CngAlgorithmGroup? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		return _algorithmGroup.Equals(other.AlgorithmGroup);
	}

	public override int GetHashCode()
	{
		return _algorithmGroup.GetHashCode();
	}

	public override string ToString()
	{
		return _algorithmGroup;
	}
}
