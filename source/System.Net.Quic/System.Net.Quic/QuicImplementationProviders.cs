using System.Net.Quic.Implementations;
using System.Net.Quic.Implementations.Mock;
using System.Net.Quic.Implementations.MsQuic;

namespace System.Net.Quic;

public static class QuicImplementationProviders
{
	public static QuicImplementationProvider Mock { get; } = new MockImplementationProvider();


	public static QuicImplementationProvider MsQuic { get; } = new MsQuicImplementationProvider();


	public static QuicImplementationProvider Default => MsQuic;
}
