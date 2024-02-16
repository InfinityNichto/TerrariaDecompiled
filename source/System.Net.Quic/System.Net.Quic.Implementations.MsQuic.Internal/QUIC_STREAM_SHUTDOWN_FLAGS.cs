namespace System.Net.Quic.Implementations.MsQuic.Internal;

[Flags]
internal enum QUIC_STREAM_SHUTDOWN_FLAGS : uint
{
	NONE = 0u,
	GRACEFUL = 1u,
	ABORT_SEND = 2u,
	ABORT_RECEIVE = 4u,
	ABORT = 6u,
	IMMEDIATE = 8u
}
