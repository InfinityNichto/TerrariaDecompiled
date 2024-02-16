using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography;

public readonly struct HashAlgorithmName : IEquatable<HashAlgorithmName>
{
	private readonly string _name;

	public static HashAlgorithmName MD5 => new HashAlgorithmName("MD5");

	public static HashAlgorithmName SHA1 => new HashAlgorithmName("SHA1");

	public static HashAlgorithmName SHA256 => new HashAlgorithmName("SHA256");

	public static HashAlgorithmName SHA384 => new HashAlgorithmName("SHA384");

	public static HashAlgorithmName SHA512 => new HashAlgorithmName("SHA512");

	public string? Name => _name;

	public HashAlgorithmName(string? name)
	{
		_name = name;
	}

	public override string ToString()
	{
		return _name ?? string.Empty;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is HashAlgorithmName)
		{
			return Equals((HashAlgorithmName)obj);
		}
		return false;
	}

	public bool Equals(HashAlgorithmName other)
	{
		return _name == other._name;
	}

	public override int GetHashCode()
	{
		if (_name != null)
		{
			return _name.GetHashCode();
		}
		return 0;
	}

	public static bool operator ==(HashAlgorithmName left, HashAlgorithmName right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(HashAlgorithmName left, HashAlgorithmName right)
	{
		return !(left == right);
	}

	public static bool TryFromOid(string oidValue, out HashAlgorithmName value)
	{
		if (oidValue == null)
		{
			throw new ArgumentNullException("oidValue");
		}
		switch (oidValue)
		{
		case "1.2.840.113549.2.5":
			value = MD5;
			return true;
		case "1.3.14.3.2.26":
			value = SHA1;
			return true;
		case "2.16.840.1.101.3.4.2.1":
			value = SHA256;
			return true;
		case "2.16.840.1.101.3.4.2.2":
			value = SHA384;
			return true;
		case "2.16.840.1.101.3.4.2.3":
			value = SHA512;
			return true;
		default:
			value = default(HashAlgorithmName);
			return false;
		}
	}

	public static HashAlgorithmName FromOid(string oidValue)
	{
		if (TryFromOid(oidValue, out var value))
		{
			return value;
		}
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_InvalidHashAlgorithmOid, oidValue));
	}
}
