using System.Security.Authentication;

namespace System.Net.Security;

public readonly struct SslClientHelloInfo
{
	public string ServerName { get; }

	public SslProtocols SslProtocols { get; }

	internal SslClientHelloInfo(string serverName, SslProtocols sslProtocols)
	{
		ServerName = serverName;
		SslProtocols = sslProtocols;
	}
}
