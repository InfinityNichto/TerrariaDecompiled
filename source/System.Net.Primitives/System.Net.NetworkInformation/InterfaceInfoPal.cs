namespace System.Net.NetworkInformation;

internal static class InterfaceInfoPal
{
	public static uint InterfaceNameToIndex(string interfaceName)
	{
		return global::Interop.IpHlpApi.if_nametoindex(interfaceName);
	}
}
