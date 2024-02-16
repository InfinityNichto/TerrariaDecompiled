using System.Net.Security;

namespace System.Net.Quic;

public class QuicClientConnectionOptions : QuicOptions
{
	public SslClientAuthenticationOptions? ClientAuthenticationOptions { get; set; }

	public IPEndPoint? LocalEndPoint { get; set; }

	public EndPoint? RemoteEndPoint { get; set; }

	public QuicClientConnectionOptions()
	{
		base.IdleTimeout = TimeSpan.FromTicks(1200000000L);
	}
}
