using System.Collections.Generic;
using System.Net.Internals;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net.NetworkInformation;

internal sealed class SystemNetworkInterface : NetworkInterface
{
	private readonly string _name;

	private readonly string _id;

	private readonly string _description;

	private readonly byte[] _physicalAddress;

	private readonly uint _addressLength;

	private readonly NetworkInterfaceType _type;

	private readonly OperationalStatus _operStatus;

	private readonly long _speed;

	private readonly uint _index;

	private readonly uint _ipv6Index;

	private readonly global::Interop.IpHlpApi.AdapterFlags _adapterFlags;

	private readonly SystemIPInterfaceProperties _interfaceProperties;

	internal static int InternalLoopbackInterfaceIndex => GetBestInterfaceForAddress(IPAddress.Loopback);

	internal static int InternalIPv6LoopbackInterfaceIndex => GetBestInterfaceForAddress(IPAddress.IPv6Loopback);

	public override string Id => _id;

	public override string Name => _name;

	public override string Description => _description;

	public override NetworkInterfaceType NetworkInterfaceType => _type;

	public override OperationalStatus OperationalStatus => _operStatus;

	public override long Speed => _speed;

	public override bool IsReceiveOnly => (_adapterFlags & global::Interop.IpHlpApi.AdapterFlags.ReceiveOnly) > (global::Interop.IpHlpApi.AdapterFlags)0;

	public override bool SupportsMulticast => (_adapterFlags & global::Interop.IpHlpApi.AdapterFlags.NoMulticast) == 0;

	private static int GetBestInterfaceForAddress(IPAddress addr)
	{
		System.Net.Internals.SocketAddress socketAddress = new System.Net.Internals.SocketAddress(addr);
		int index;
		int bestInterfaceEx = (int)global::Interop.IpHlpApi.GetBestInterfaceEx(socketAddress.Buffer, out index);
		if (bestInterfaceEx != 0)
		{
			throw new NetworkInformationException(bestInterfaceEx);
		}
		return index;
	}

	internal static bool InternalGetIsNetworkAvailable()
	{
		try
		{
			NetworkInterface[] networkInterfaces = GetNetworkInterfaces();
			NetworkInterface[] array = networkInterfaces;
			foreach (NetworkInterface networkInterface in array)
			{
				if (networkInterface.OperationalStatus == OperationalStatus.Up && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
				{
					return true;
				}
			}
		}
		catch (NetworkInformationException message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, message, "InternalGetIsNetworkAvailable");
			}
		}
		return false;
	}

	internal static NetworkInterface[] GetNetworkInterfaces()
	{
		AddressFamily family = AddressFamily.Unspecified;
		uint outBufLen = 0u;
		ref readonly global::Interop.IpHlpApi.FIXED_INFO fixedInfo = ref System.Net.NetworkInformation.HostInformationPal.FixedInfo;
		List<SystemNetworkInterface> list = new List<SystemNetworkInterface>();
		global::Interop.IpHlpApi.GetAdaptersAddressesFlags flags = global::Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeWins | global::Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeGateways;
		uint adaptersAddresses = global::Interop.IpHlpApi.GetAdaptersAddresses(family, (uint)flags, IntPtr.Zero, IntPtr.Zero, ref outBufLen);
		while (true)
		{
			switch (adaptersAddresses)
			{
			case 111u:
			{
				IntPtr intPtr = Marshal.AllocHGlobal((int)outBufLen);
				try
				{
					adaptersAddresses = global::Interop.IpHlpApi.GetAdaptersAddresses(family, (uint)flags, IntPtr.Zero, intPtr, ref outBufLen);
					if (adaptersAddresses == 0)
					{
						IntPtr intPtr2 = intPtr;
						while (intPtr2 != IntPtr.Zero)
						{
							global::Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses = Marshal.PtrToStructure<global::Interop.IpHlpApi.IpAdapterAddresses>(intPtr2);
							list.Add(new SystemNetworkInterface(in fixedInfo, in ipAdapterAddresses));
							intPtr2 = ipAdapterAddresses.next;
						}
					}
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
				break;
			}
			case 87u:
			case 232u:
				return Array.Empty<SystemNetworkInterface>();
			default:
				throw new NetworkInformationException((int)adaptersAddresses);
			case 0u:
				return list.ToArray();
			}
		}
	}

	internal SystemNetworkInterface(in global::Interop.IpHlpApi.FIXED_INFO fixedInfo, in global::Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses)
	{
		_id = ipAdapterAddresses.AdapterName;
		_name = ipAdapterAddresses.friendlyName;
		_description = ipAdapterAddresses.description;
		_index = ipAdapterAddresses.index;
		_physicalAddress = ipAdapterAddresses.address;
		_addressLength = ipAdapterAddresses.addressLength;
		_type = ipAdapterAddresses.type;
		_operStatus = ipAdapterAddresses.operStatus;
		_speed = (long)ipAdapterAddresses.receiveLinkSpeed;
		_ipv6Index = ipAdapterAddresses.ipv6Index;
		_adapterFlags = ipAdapterAddresses.flags;
		_interfaceProperties = new SystemIPInterfaceProperties(in fixedInfo, in ipAdapterAddresses);
	}

	public override PhysicalAddress GetPhysicalAddress()
	{
		byte[] array = new byte[_addressLength];
		Buffer.BlockCopy(_physicalAddress, 0, array, 0, checked((int)_addressLength));
		return new PhysicalAddress(array);
	}

	public override IPInterfaceProperties GetIPProperties()
	{
		return _interfaceProperties;
	}

	public override IPv4InterfaceStatistics GetIPv4Statistics()
	{
		return new SystemIPv4InterfaceStatistics(_index);
	}

	public override IPInterfaceStatistics GetIPStatistics()
	{
		return new SystemIPInterfaceStatistics(_index);
	}

	public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
	{
		if (networkInterfaceComponent == NetworkInterfaceComponent.IPv6 && (_adapterFlags & global::Interop.IpHlpApi.AdapterFlags.IPv6Enabled) != 0)
		{
			return true;
		}
		if (networkInterfaceComponent == NetworkInterfaceComponent.IPv4 && (_adapterFlags & global::Interop.IpHlpApi.AdapterFlags.IPv4Enabled) != 0)
		{
			return true;
		}
		return false;
	}
}
