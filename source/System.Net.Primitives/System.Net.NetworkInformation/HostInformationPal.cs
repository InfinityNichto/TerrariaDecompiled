using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.NetworkInformation;

internal static class HostInformationPal
{
	private static global::Interop.IpHlpApi.FIXED_INFO s_fixedInfo;

	private static bool s_fixedInfoInitialized;

	private static object s_syncObject = new object();

	public static ref readonly global::Interop.IpHlpApi.FIXED_INFO FixedInfo
	{
		get
		{
			LazyInitializer.EnsureInitialized(ref s_fixedInfo, ref s_fixedInfoInitialized, ref s_syncObject, () => GetFixedInfo());
			return ref s_fixedInfo;
		}
	}

	public static string GetDomainName()
	{
		return FixedInfo.domainName;
	}

	private static global::Interop.IpHlpApi.FIXED_INFO GetFixedInfo()
	{
		uint pOutBufLen = 0u;
		global::Interop.IpHlpApi.FIXED_INFO result = default(global::Interop.IpHlpApi.FIXED_INFO);
		uint networkParams = global::Interop.IpHlpApi.GetNetworkParams(IntPtr.Zero, ref pOutBufLen);
		while (true)
		{
			switch (networkParams)
			{
			case 111u:
			{
				IntPtr intPtr = Marshal.AllocHGlobal((int)pOutBufLen);
				try
				{
					networkParams = global::Interop.IpHlpApi.GetNetworkParams(intPtr, ref pOutBufLen);
					if (networkParams == 0)
					{
						result = Marshal.PtrToStructure<global::Interop.IpHlpApi.FIXED_INFO>(intPtr);
					}
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
				break;
			}
			default:
				throw new Win32Exception((int)networkParams);
			case 0u:
				return result;
			}
		}
	}
}
