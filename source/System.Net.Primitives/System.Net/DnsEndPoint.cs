using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace System.Net;

public class DnsEndPoint : EndPoint
{
	private readonly string _host;

	private readonly int _port;

	private readonly AddressFamily _family;

	public string Host => _host;

	public override AddressFamily AddressFamily => _family;

	public int Port => _port;

	public DnsEndPoint(string host, int port)
		: this(host, port, AddressFamily.Unspecified)
	{
	}

	public DnsEndPoint(string host, int port, AddressFamily addressFamily)
	{
		if (host == null)
		{
			throw new ArgumentNullException("host");
		}
		if (string.IsNullOrEmpty(host))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "host"));
		}
		if (port < 0 || port > 65535)
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6 && addressFamily != 0)
		{
			throw new ArgumentException(System.SR.net_sockets_invalid_optionValue_all, "addressFamily");
		}
		_host = host;
		_port = port;
		_family = addressFamily;
	}

	public override bool Equals([NotNullWhen(true)] object? comparand)
	{
		if (!(comparand is DnsEndPoint dnsEndPoint))
		{
			return false;
		}
		if (_family == dnsEndPoint._family && _port == dnsEndPoint._port)
		{
			return _host == dnsEndPoint._host;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(ToString());
	}

	public override string ToString()
	{
		return _family.ToString() + "/" + _host + ":" + _port;
	}
}
