using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public sealed class RSASignaturePadding : IEquatable<RSASignaturePadding>
{
	private static readonly RSASignaturePadding s_pkcs1 = new RSASignaturePadding(RSASignaturePaddingMode.Pkcs1);

	private static readonly RSASignaturePadding s_pss = new RSASignaturePadding(RSASignaturePaddingMode.Pss);

	private readonly RSASignaturePaddingMode _mode;

	public static RSASignaturePadding Pkcs1 => s_pkcs1;

	public static RSASignaturePadding Pss => s_pss;

	public RSASignaturePaddingMode Mode => _mode;

	private RSASignaturePadding(RSASignaturePaddingMode mode)
	{
		_mode = mode;
	}

	public override int GetHashCode()
	{
		return _mode.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as RSASignaturePadding);
	}

	public bool Equals([NotNullWhen(true)] RSASignaturePadding? other)
	{
		if ((object)other != null)
		{
			return _mode == other._mode;
		}
		return false;
	}

	public static bool operator ==(RSASignaturePadding? left, RSASignaturePadding? right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(RSASignaturePadding? left, RSASignaturePadding? right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return _mode.ToString();
	}
}
