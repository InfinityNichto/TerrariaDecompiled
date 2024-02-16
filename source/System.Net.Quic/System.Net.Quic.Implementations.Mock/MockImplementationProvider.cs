namespace System.Net.Quic.Implementations.Mock;

internal sealed class MockImplementationProvider : QuicImplementationProvider
{
	public override bool IsSupported => true;

	internal override QuicListenerProvider CreateListener(QuicListenerOptions options)
	{
		return new MockListener(options);
	}

	internal override QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options)
	{
		return new MockConnection(options.RemoteEndPoint, options.ClientAuthenticationOptions, options.LocalEndPoint, options.MaxUnidirectionalStreams, options.MaxBidirectionalStreams);
	}
}
