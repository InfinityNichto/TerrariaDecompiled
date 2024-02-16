using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net;

internal static class SocketProtocolSupportPal
{
	public static bool OSSupportsIPv6 { get; } = IsSupported(AddressFamily.InterNetworkV6) && !IsIPv6Disabled();


	public static bool OSSupportsIPv4 { get; } = IsSupported(AddressFamily.InterNetwork);


	public static bool OSSupportsUnixDomainSockets { get; } = IsSupported(AddressFamily.Unix);


	private static bool IsIPv6Disabled()
	{
		if (AppContext.TryGetSwitch("System.Net.DisableIPv6", out var isEnabled))
		{
			return isEnabled;
		}
		string environmentVariable = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_DISABLEIPV6");
		if (environmentVariable != null)
		{
			if (!(environmentVariable == "1"))
			{
				return environmentVariable.Equals("true", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	private static bool IsSupported(AddressFamily af)
	{
		global::Interop.Winsock.EnsureInitialized();
		IntPtr intPtr = (IntPtr)(-1);
		IntPtr intPtr2 = intPtr;
		try
		{
			intPtr2 = global::Interop.Winsock.WSASocketW(af, SocketType.Stream, 0, IntPtr.Zero, 0, 128);
			return intPtr2 != intPtr || Marshal.GetLastWin32Error() != 10047;
		}
		finally
		{
			if (intPtr2 != intPtr)
			{
				global::Interop.Winsock.closesocket(intPtr2);
			}
		}
	}
}
