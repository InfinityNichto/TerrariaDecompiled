namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal static class MsQuicStatusCodes
{
	public static string GetError(uint status)
	{
		return status switch
		{
			0u => "SUCCESS", 
			459749u => "PENDING", 
			459998u => "CONTINUE", 
			2147942414u => "OUT_OF_MEMORY", 
			2147942487u => "INVALID_PARAMETER", 
			2147947423u => "INVALID_STATE", 
			2147500034u => "NOT_SUPPORTED", 
			2147943568u => "NOT_FOUND", 
			2147942522u => "BUFFER_TOO_SMALL", 
			2151743488u => "HANDSHAKE_FAILURE", 
			2147500036u => "ABORTED", 
			2147952448u => "ADDRESS_IN_USE", 
			2151743494u => "CONNECTION_TIMEOUT", 
			2151743493u => "CONNECTION_IDLE", 
			2147943632u => "UNREACHABLE", 
			2151743491u => "INTERNAL_ERROR", 
			2147943625u => "CONNECTION_REFUSED", 
			2151743492u => "PROTOCOL_ERROR", 
			2151743489u => "VER_NEG_ERROR", 
			2147953432u => "TLS_ERROR", 
			2151743490u => "USER_CANCELED", 
			2151743495u => "ALPN_NEG_FAILURE", 
			2151743496u => "STREAM_LIMIT_REACHED", 
			_ => $"0x{status:X8}", 
		};
	}
}
