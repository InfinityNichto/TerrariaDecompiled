namespace System.Net.Quic.Implementations.MsQuic.Internal;

[Flags]
internal enum QUIC_SEND_FLAGS : uint
{
	NONE = 0u,
	ALLOW_0_RTT = 1u,
	START = 2u,
	FIN = 4u,
	DGRAM_PRIORITY = 8u,
	DELAY_SEND = 0x10u
}
