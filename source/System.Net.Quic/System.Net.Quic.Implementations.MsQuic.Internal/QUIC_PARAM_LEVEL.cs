namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal enum QUIC_PARAM_LEVEL : uint
{
	GLOBAL,
	REGISTRATION,
	CONFIGURATION,
	LISTENER,
	CONNECTION,
	TLS,
	STREAM
}
