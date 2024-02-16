using System.Net.Sockets;

namespace System.Net.NetworkInformation;

internal sealed class SystemIPInterfaceProperties : IPInterfaceProperties
{
	private readonly bool _dnsEnabled;

	private readonly bool _dynamicDnsEnabled;

	private readonly InternalIPAddressCollection _dnsAddresses;

	private readonly UnicastIPAddressInformationCollection _unicastAddresses;

	private readonly MulticastIPAddressInformationCollection _multicastAddresses;

	private readonly IPAddressInformationCollection _anycastAddresses;

	private readonly global::Interop.IpHlpApi.AdapterFlags _adapterFlags;

	private readonly string _dnsSuffix;

	private readonly SystemIPv4InterfaceProperties _ipv4Properties;

	private readonly SystemIPv6InterfaceProperties _ipv6Properties;

	private readonly InternalIPAddressCollection _winsServersAddresses;

	private readonly GatewayIPAddressInformationCollection _gatewayAddresses;

	private readonly InternalIPAddressCollection _dhcpServers;

	public override bool IsDnsEnabled => _dnsEnabled;

	public override bool IsDynamicDnsEnabled => _dynamicDnsEnabled;

	public override string DnsSuffix => _dnsSuffix;

	public override IPAddressInformationCollection AnycastAddresses => _anycastAddresses;

	public override UnicastIPAddressInformationCollection UnicastAddresses => _unicastAddresses;

	public override MulticastIPAddressInformationCollection MulticastAddresses => _multicastAddresses;

	public override IPAddressCollection DnsAddresses => _dnsAddresses;

	public override GatewayIPAddressInformationCollection GatewayAddresses => _gatewayAddresses;

	public override IPAddressCollection DhcpServerAddresses => _dhcpServers;

	public override IPAddressCollection WinsServersAddresses => _winsServersAddresses;

	internal SystemIPInterfaceProperties(in global::Interop.IpHlpApi.FIXED_INFO fixedInfo, in global::Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses)
	{
		_adapterFlags = ipAdapterAddresses.flags;
		_dnsSuffix = ipAdapterAddresses.dnsSuffix;
		_dnsEnabled = fixedInfo.enableDns;
		_dynamicDnsEnabled = (ipAdapterAddresses.flags & global::Interop.IpHlpApi.AdapterFlags.DnsEnabled) > (global::Interop.IpHlpApi.AdapterFlags)0;
		_multicastAddresses = SystemMulticastIPAddressInformation.ToMulticastIpAddressInformationCollection(global::Interop.IpHlpApi.IpAdapterAddress.MarshalIpAddressInformationCollection(ipAdapterAddresses.firstMulticastAddress));
		_dnsAddresses = global::Interop.IpHlpApi.IpAdapterAddress.MarshalIpAddressCollection(ipAdapterAddresses.firstDnsServerAddress);
		_anycastAddresses = global::Interop.IpHlpApi.IpAdapterAddress.MarshalIpAddressInformationCollection(ipAdapterAddresses.firstAnycastAddress);
		_unicastAddresses = SystemUnicastIPAddressInformation.MarshalUnicastIpAddressInformationCollection(ipAdapterAddresses.firstUnicastAddress);
		_winsServersAddresses = global::Interop.IpHlpApi.IpAdapterAddress.MarshalIpAddressCollection(ipAdapterAddresses.firstWinsServerAddress);
		_gatewayAddresses = SystemGatewayIPAddressInformation.ToGatewayIpAddressInformationCollection(global::Interop.IpHlpApi.IpAdapterAddress.MarshalIpAddressCollection(ipAdapterAddresses.firstGatewayAddress));
		_dhcpServers = new InternalIPAddressCollection();
		if (ipAdapterAddresses.dhcpv4Server.address != IntPtr.Zero)
		{
			_dhcpServers.InternalAdd(ipAdapterAddresses.dhcpv4Server.MarshalIPAddress());
		}
		if (ipAdapterAddresses.dhcpv6Server.address != IntPtr.Zero)
		{
			_dhcpServers.InternalAdd(ipAdapterAddresses.dhcpv6Server.MarshalIPAddress());
		}
		if ((_adapterFlags & global::Interop.IpHlpApi.AdapterFlags.IPv4Enabled) != 0)
		{
			_ipv4Properties = new SystemIPv4InterfaceProperties(fixedInfo, ipAdapterAddresses);
		}
		if ((_adapterFlags & global::Interop.IpHlpApi.AdapterFlags.IPv6Enabled) != 0)
		{
			_ipv6Properties = new SystemIPv6InterfaceProperties(ipAdapterAddresses.ipv6Index, ipAdapterAddresses.mtu, ipAdapterAddresses.zoneIndices);
		}
	}

	public override IPv4InterfaceProperties GetIPv4Properties()
	{
		if (_ipv4Properties == null)
		{
			throw new NetworkInformationException(SocketError.ProtocolNotSupported);
		}
		return _ipv4Properties;
	}

	public override IPv6InterfaceProperties GetIPv6Properties()
	{
		if (_ipv6Properties == null)
		{
			throw new NetworkInformationException(SocketError.ProtocolNotSupported);
		}
		return _ipv6Properties;
	}
}
