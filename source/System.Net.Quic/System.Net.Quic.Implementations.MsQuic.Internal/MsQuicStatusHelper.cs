namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal static class MsQuicStatusHelper
{
	internal static bool SuccessfulStatusCode(uint status)
	{
		if (OperatingSystem.IsWindows())
		{
			return status < 2147483648u;
		}
		if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
		{
			return (int)status <= 0;
		}
		return false;
	}
}
