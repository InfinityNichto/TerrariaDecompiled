namespace System.Net.Quic.Implementations.MsQuic.Internal;

[Flags]
internal enum QUIC_CONNECTION_SHUTDOWN_FLAGS : uint
{
	NONE = 0u,
	SILENT = 1u
}
