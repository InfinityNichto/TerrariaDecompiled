namespace System.Net.Quic.Implementations.MsQuic.Internal;

[Flags]
internal enum QUIC_RECEIVE_FLAGS : uint
{
	NONE = 0u,
	ZERO_RTT = 1u,
	FIN = 2u
}
