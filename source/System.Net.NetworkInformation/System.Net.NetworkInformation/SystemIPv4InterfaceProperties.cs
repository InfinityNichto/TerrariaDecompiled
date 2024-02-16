using System.Runtime.InteropServices;

namespace System.Net.NetworkInformation;

internal sealed class SystemIPv4InterfaceProperties : IPv4InterfaceProperties
{
	private readonly bool _haveWins;

	private readonly bool _dhcpEnabled;

	private readonly bool _routingEnabled;

	private readonly uint _index;

	private readonly uint _mtu;

	private bool _autoConfigEnabled;

	private bool _autoConfigActive;

	public override bool UsesWins => _haveWins;

	public override bool IsDhcpEnabled => _dhcpEnabled;

	public override bool IsForwardingEnabled => _routingEnabled;

	public override bool IsAutomaticPrivateAddressingEnabled => _autoConfigEnabled;

	public override bool IsAutomaticPrivateAddressingActive => _autoConfigActive;

	public override int Mtu => (int)_mtu;

	public override int Index => (int)_index;

	internal SystemIPv4InterfaceProperties(global::Interop.IpHlpApi.FIXED_INFO fixedInfo, global::Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses)
	{
		_index = ipAdapterAddresses.index;
		_routingEnabled = fixedInfo.enableRouting;
		_dhcpEnabled = (ipAdapterAddresses.flags & global::Interop.IpHlpApi.AdapterFlags.DhcpEnabled) != 0;
		_haveWins = ipAdapterAddresses.firstWinsServerAddress != IntPtr.Zero;
		_mtu = ipAdapterAddresses.mtu;
		GetPerAdapterInfo(ipAdapterAddresses.index);
	}

	private void GetPerAdapterInfo(uint index)
	{
		if (index == 0)
		{
			return;
		}
		uint pOutBufLen = 0u;
		uint perAdapterInfo = global::Interop.IpHlpApi.GetPerAdapterInfo(index, IntPtr.Zero, ref pOutBufLen);
		while (true)
		{
			switch (perAdapterInfo)
			{
			case 111u:
			{
				IntPtr intPtr = Marshal.AllocHGlobal((int)pOutBufLen);
				try
				{
					perAdapterInfo = global::Interop.IpHlpApi.GetPerAdapterInfo(index, intPtr, ref pOutBufLen);
					if (perAdapterInfo == 0)
					{
						global::Interop.IpHlpApi.IpPerAdapterInfo ipPerAdapterInfo = Marshal.PtrToStructure<global::Interop.IpHlpApi.IpPerAdapterInfo>(intPtr);
						_autoConfigEnabled = ipPerAdapterInfo.autoconfigEnabled;
						_autoConfigActive = ipPerAdapterInfo.autoconfigActive;
					}
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
				break;
			}
			default:
				throw new NetworkInformationException((int)perAdapterInfo);
			case 0u:
				return;
			}
		}
	}
}
