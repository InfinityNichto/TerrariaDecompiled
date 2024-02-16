using System.Security.Authentication.ExtendedProtection;

namespace System.Net;

internal sealed class HttpListenerRequestContext : TransportContext
{
	private readonly HttpListenerRequest _request;

	internal HttpListenerRequestContext(HttpListenerRequest request)
	{
		_request = request;
	}

	public override ChannelBinding GetChannelBinding(ChannelBindingKind kind)
	{
		if (kind != ChannelBindingKind.Endpoint)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_listener_invalid_cbt_type, kind.ToString()));
		}
		return _request.GetChannelBinding();
	}
}
