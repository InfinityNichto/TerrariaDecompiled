using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Net.Security;

public readonly struct SslApplicationProtocol : IEquatable<SslApplicationProtocol>
{
	private static readonly Encoding s_utf8 = Encoding.GetEncoding(Encoding.UTF8.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

	private static readonly byte[] s_http3Utf8 = new byte[2] { 104, 51 };

	private static readonly byte[] s_http2Utf8 = new byte[2] { 104, 50 };

	private static readonly byte[] s_http11Utf8 = new byte[8] { 104, 116, 116, 112, 47, 49, 46, 49 };

	public static readonly SslApplicationProtocol Http3 = new SslApplicationProtocol(s_http3Utf8, copy: false);

	public static readonly SslApplicationProtocol Http2 = new SslApplicationProtocol(s_http2Utf8, copy: false);

	public static readonly SslApplicationProtocol Http11 = new SslApplicationProtocol(s_http11Utf8, copy: false);

	private readonly byte[] _readOnlyProtocol;

	public ReadOnlyMemory<byte> Protocol => _readOnlyProtocol;

	internal SslApplicationProtocol(byte[] protocol, bool copy)
	{
		if (protocol.Length == 0 || protocol.Length > 255)
		{
			throw new ArgumentException(System.SR.net_ssl_app_protocol_invalid, "protocol");
		}
		_readOnlyProtocol = (copy ? protocol.AsSpan().ToArray() : protocol);
	}

	public SslApplicationProtocol(byte[] protocol)
		: this(protocol ?? throw new ArgumentNullException("protocol"), copy: true)
	{
	}

	public SslApplicationProtocol(string protocol)
		: this(s_utf8.GetBytes(protocol ?? throw new ArgumentNullException("protocol")), copy: false)
	{
	}

	public bool Equals(SslApplicationProtocol other)
	{
		return ((ReadOnlySpan<byte>)_readOnlyProtocol).SequenceEqual((ReadOnlySpan<byte>)other._readOnlyProtocol);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is SslApplicationProtocol other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		byte[] readOnlyProtocol = _readOnlyProtocol;
		if (readOnlyProtocol == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < readOnlyProtocol.Length; i++)
		{
			num = ((num << 5) + num) ^ readOnlyProtocol[i];
		}
		return num;
	}

	public override string ToString()
	{
		byte[] readOnlyProtocol = _readOnlyProtocol;
		try
		{
			return (readOnlyProtocol == null) ? string.Empty : ((readOnlyProtocol == s_http3Utf8) ? "h3" : ((readOnlyProtocol == s_http2Utf8) ? "h2" : ((readOnlyProtocol == s_http11Utf8) ? "http/1.1" : s_utf8.GetString(readOnlyProtocol))));
		}
		catch
		{
			char[] array = new char[readOnlyProtocol.Length * 5];
			int num = 0;
			for (int i = 0; i < array.Length; i += 5)
			{
				byte b = readOnlyProtocol[num++];
				array[i] = '0';
				array[i + 1] = 'x';
				array[i + 2] = System.HexConverter.ToCharLower(b >> 4);
				array[i + 3] = System.HexConverter.ToCharLower(b);
				array[i + 4] = ' ';
			}
			return new string(array, 0, array.Length - 1);
		}
	}

	public static bool operator ==(SslApplicationProtocol left, SslApplicationProtocol right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(SslApplicationProtocol left, SslApplicationProtocol right)
	{
		return !(left == right);
	}
}
