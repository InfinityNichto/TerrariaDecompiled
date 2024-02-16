using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography;

public sealed class CngKeyBlobFormat : IEquatable<CngKeyBlobFormat>
{
	private static CngKeyBlobFormat s_eccPrivate;

	private static CngKeyBlobFormat s_eccPublic;

	private static CngKeyBlobFormat s_eccFullPrivate;

	private static CngKeyBlobFormat s_eccFullPublic;

	private static CngKeyBlobFormat s_genericPrivate;

	private static CngKeyBlobFormat s_genericPublic;

	private static CngKeyBlobFormat s_opaqueTransport;

	private static CngKeyBlobFormat s_pkcs8Private;

	private readonly string _format;

	public string Format => _format;

	public static CngKeyBlobFormat EccPrivateBlob => s_eccPrivate ?? (s_eccPrivate = new CngKeyBlobFormat("ECCPRIVATEBLOB"));

	public static CngKeyBlobFormat EccPublicBlob => s_eccPublic ?? (s_eccPublic = new CngKeyBlobFormat("ECCPUBLICBLOB"));

	public static CngKeyBlobFormat EccFullPrivateBlob => s_eccFullPrivate ?? (s_eccFullPrivate = new CngKeyBlobFormat("ECCFULLPRIVATEBLOB"));

	public static CngKeyBlobFormat EccFullPublicBlob => s_eccFullPublic ?? (s_eccFullPublic = new CngKeyBlobFormat("ECCFULLPUBLICBLOB"));

	public static CngKeyBlobFormat GenericPrivateBlob => s_genericPrivate ?? (s_genericPrivate = new CngKeyBlobFormat("PRIVATEBLOB"));

	public static CngKeyBlobFormat GenericPublicBlob => s_genericPublic ?? (s_genericPublic = new CngKeyBlobFormat("PUBLICBLOB"));

	public static CngKeyBlobFormat OpaqueTransportBlob => s_opaqueTransport ?? (s_opaqueTransport = new CngKeyBlobFormat("OpaqueTransport"));

	public static CngKeyBlobFormat Pkcs8PrivateBlob => s_pkcs8Private ?? (s_pkcs8Private = new CngKeyBlobFormat("PKCS8_PRIVATEKEY"));

	public CngKeyBlobFormat(string format)
	{
		if (format == null)
		{
			throw new ArgumentNullException("format");
		}
		if (format.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_InvalidKeyBlobFormat, format), "format");
		}
		_format = format;
	}

	public static bool operator ==(CngKeyBlobFormat? left, CngKeyBlobFormat? right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(CngKeyBlobFormat? left, CngKeyBlobFormat? right)
	{
		if ((object)left == null)
		{
			return (object)right != null;
		}
		return !left.Equals(right);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as CngKeyBlobFormat);
	}

	public bool Equals([NotNullWhen(true)] CngKeyBlobFormat? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		return _format.Equals(other.Format);
	}

	public override int GetHashCode()
	{
		return _format.GetHashCode();
	}

	public override string ToString()
	{
		return _format;
	}
}
