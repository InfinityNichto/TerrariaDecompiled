using System.Net.Security;

namespace System.Net.Quic;

public class QuicListenerOptions : QuicOptions
{
	public SslServerAuthenticationOptions? ServerAuthenticationOptions { get; set; }

	public IPEndPoint? ListenEndPoint { get; set; }

	public int ListenBacklog { get; set; } = 512;


	public QuicListenerOptions()
	{
		base.IdleTimeout = TimeSpan.FromTicks(6000000000L);
	}
}
