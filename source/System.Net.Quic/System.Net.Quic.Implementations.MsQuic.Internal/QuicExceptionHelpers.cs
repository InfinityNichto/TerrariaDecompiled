namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal static class QuicExceptionHelpers
{
	internal static void ThrowIfFailed(uint status, string message = null, Exception innerException = null)
	{
		if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
		{
			throw CreateExceptionForHResult(status, message, innerException);
		}
	}

	internal static Exception CreateExceptionForHResult(uint status, string message = null, Exception innerException = null)
	{
		return new QuicException(message + " Error Code: " + MsQuicStatusCodes.GetError(status), innerException, MapMsQuicStatusToHResult(status));
	}

	internal static int MapMsQuicStatusToHResult(uint status)
	{
		return status switch
		{
			2147943625u => 10061, 
			2151743494u => 10060, 
			2147943632u => 10065, 
			_ => 0, 
		};
	}
}
