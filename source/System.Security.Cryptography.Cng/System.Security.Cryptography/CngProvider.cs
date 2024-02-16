using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography;

public sealed class CngProvider : IEquatable<CngProvider>
{
	private static CngProvider s_msPlatformKsp;

	private static CngProvider s_msSmartCardKsp;

	private static CngProvider s_msSoftwareKsp;

	private readonly string _provider;

	public string Provider => _provider;

	public static CngProvider MicrosoftPlatformCryptoProvider => s_msPlatformKsp ?? (s_msPlatformKsp = new CngProvider("Microsoft Platform Crypto Provider"));

	public static CngProvider MicrosoftSmartCardKeyStorageProvider => s_msSmartCardKsp ?? (s_msSmartCardKsp = new CngProvider("Microsoft Smart Card Key Storage Provider"));

	public static CngProvider MicrosoftSoftwareKeyStorageProvider => s_msSoftwareKsp ?? (s_msSoftwareKsp = new CngProvider("Microsoft Software Key Storage Provider"));

	public CngProvider(string provider)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (provider.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_InvalidProviderName, provider), "provider");
		}
		_provider = provider;
	}

	public static bool operator ==(CngProvider? left, CngProvider? right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(CngProvider? left, CngProvider? right)
	{
		if ((object)left == null)
		{
			return (object)right != null;
		}
		return !left.Equals(right);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as CngProvider);
	}

	public bool Equals([NotNullWhen(true)] CngProvider? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		return _provider.Equals(other.Provider);
	}

	public override int GetHashCode()
	{
		return _provider.GetHashCode();
	}

	public override string ToString()
	{
		return _provider.ToString();
	}
}
