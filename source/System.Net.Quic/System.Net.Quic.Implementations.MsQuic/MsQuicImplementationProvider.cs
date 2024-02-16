using System.Net.Quic.Implementations.MsQuic.Internal;

namespace System.Net.Quic.Implementations.MsQuic;

internal sealed class MsQuicImplementationProvider : QuicImplementationProvider
{
	public override bool IsSupported => MsQuicApi.IsQuicSupported;

	internal override QuicListenerProvider CreateListener(QuicListenerOptions options)
	{
		return new MsQuicListener(options);
	}

	internal override QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options)
	{
		return new MsQuicConnection(options);
	}
}
