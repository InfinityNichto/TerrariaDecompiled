namespace System.Net.Quic.Implementations;

public abstract class QuicImplementationProvider
{
	public abstract bool IsSupported { get; }

	internal QuicImplementationProvider()
	{
	}

	internal abstract QuicListenerProvider CreateListener(QuicListenerOptions options);

	internal abstract QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options);
}
