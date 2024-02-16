using System.Net.Security;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net;

internal sealed class SslStreamContext : TransportContext
{
	private readonly SslStream _sslStream;

	internal SslStreamContext(SslStream sslStream)
	{
		_sslStream = sslStream;
	}

	public override ChannelBinding GetChannelBinding(ChannelBindingKind kind)
	{
		return _sslStream.GetChannelBinding(kind);
	}
}
