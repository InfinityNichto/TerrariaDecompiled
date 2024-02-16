namespace System.Net.Quic.Implementations.MsQuic.Internal;

[Flags]
internal enum QUIC_STREAM_START_FLAGS : uint
{
	NONE = 0u,
	FAIL_BLOCKED = 1u,
	IMMEDIATE = 2u,
	ASYNC = 4u,
	SHUTDOWN_ON_FAIL = 8u
}
