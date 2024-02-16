namespace System.Net.Quic.Implementations.MsQuic.Internal;

[Flags]
internal enum QUIC_STREAM_OPEN_FLAGS : uint
{
	NONE = 0u,
	UNIDIRECTIONAL = 1u,
	ZERO_RTT = 2u
}
