using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http;

internal sealed class HttpAuthority : IEquatable<HttpAuthority>
{
	public string IdnHost { get; }

	public int Port { get; }

	public HttpAuthority(string host, int port)
	{
		UriBuilder uriBuilder = new UriBuilder(Uri.UriSchemeHttp, host, port);
		Uri uri = uriBuilder.Uri;
		IdnHost = ((uri.HostNameType == UriHostNameType.IPv6) ? ("[" + uri.IdnHost + "]") : uri.IdnHost);
		Port = port;
	}

	public bool Equals([NotNullWhen(true)] HttpAuthority other)
	{
		if (other != null && string.Equals(IdnHost, other.IdnHost))
		{
			return Port == other.Port;
		}
		return false;
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj is HttpAuthority other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(IdnHost, Port);
	}

	public override string ToString()
	{
		if (IdnHost == null)
		{
			return "<empty>";
		}
		return $"{IdnHost}:{Port}";
	}
}
