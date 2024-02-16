namespace System.Net.NetworkInformation;

internal static class NetworkInterfacePal
{
	public static int LoopbackInterfaceIndex => SystemNetworkInterface.InternalLoopbackInterfaceIndex;

	public static int IPv6LoopbackInterfaceIndex => SystemNetworkInterface.InternalIPv6LoopbackInterfaceIndex;

	public static NetworkInterface[] GetAllNetworkInterfaces()
	{
		return SystemNetworkInterface.GetNetworkInterfaces();
	}

	public static bool GetIsNetworkAvailable()
	{
		return SystemNetworkInterface.InternalGetIsNetworkAvailable();
	}
}
