namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal enum QUIC_CREDENTIAL_TYPE : uint
{
	NONE,
	HASH,
	HASH_STORE,
	CONTEXT,
	FILE,
	FILE_PROTECTED,
	PKCS12
}
