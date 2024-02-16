using System;
using System.Net;
using System.Net.Internals;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static class Interop
{
	internal static class IpHlpApi
	{
		public struct FIXED_INFO
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 132)]
			public string hostName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 132)]
			public string domainName;

			public IntPtr currentDnsServer;

			public IP_ADDR_STRING DnsServerList;

			public uint nodeType;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string scopeId;

			public bool enableRouting;

			public bool enableProxy;

			public bool enableDns;
		}

		public struct IP_ADDR_STRING
		{
			public IntPtr Next;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
			public string IpAddress;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
			public string IpMask;

			public uint Context;
		}

		[Flags]
		internal enum AdapterFlags
		{
			DnsEnabled = 1,
			RegisterAdapterSuffix = 2,
			DhcpEnabled = 4,
			ReceiveOnly = 8,
			NoMulticast = 0x10,
			Ipv6OtherStatefulConfig = 0x20,
			NetBiosOverTcp = 0x40,
			IPv4Enabled = 0x80,
			IPv6Enabled = 0x100,
			IPv6ManagedAddressConfigurationSupported = 0x200
		}

		[Flags]
		internal enum AdapterAddressFlags
		{
			DnsEligible = 1,
			Transient = 2
		}

		[Flags]
		internal enum GetAdaptersAddressesFlags
		{
			SkipUnicast = 1,
			SkipAnycast = 2,
			SkipMulticast = 4,
			SkipDnsServer = 8,
			IncludePrefix = 0x10,
			SkipFriendlyName = 0x20,
			IncludeWins = 0x40,
			IncludeGateways = 0x80,
			IncludeAllInterfaces = 0x100,
			IncludeAllCompartments = 0x200,
			IncludeTunnelBindingOrder = 0x400
		}

		internal struct IpSocketAddress
		{
			internal IntPtr address;

			internal int addressLength;

			internal IPAddress MarshalIPAddress()
			{
				AddressFamily family = ((addressLength > System.Net.Internals.SocketAddress.IPv4AddressSize) ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);
				System.Net.Internals.SocketAddress socketAddress = new System.Net.Internals.SocketAddress(family, addressLength);
				Marshal.Copy(address, socketAddress.Buffer, 0, addressLength);
				return socketAddress.GetIPAddress();
			}
		}

		internal struct IpAdapterAddress
		{
			internal uint length;

			internal AdapterAddressFlags flags;

			internal IntPtr next;

			internal IpSocketAddress address;

			internal static InternalIPAddressCollection MarshalIpAddressCollection(IntPtr ptr)
			{
				InternalIPAddressCollection internalIPAddressCollection = new InternalIPAddressCollection();
				while (ptr != IntPtr.Zero)
				{
					IpAdapterAddress ipAdapterAddress = Marshal.PtrToStructure<IpAdapterAddress>(ptr);
					IPAddress iPAddress = ipAdapterAddress.address.MarshalIPAddress();
					internalIPAddressCollection.InternalAdd(iPAddress);
					ptr = ipAdapterAddress.next;
				}
				return internalIPAddressCollection;
			}

			internal static IPAddressInformationCollection MarshalIpAddressInformationCollection(IntPtr ptr)
			{
				IPAddressInformationCollection iPAddressInformationCollection = new IPAddressInformationCollection();
				while (ptr != IntPtr.Zero)
				{
					IpAdapterAddress ipAdapterAddress = Marshal.PtrToStructure<IpAdapterAddress>(ptr);
					IPAddress iPAddress = ipAdapterAddress.address.MarshalIPAddress();
					iPAddressInformationCollection.InternalAdd(new SystemIPAddressInformation(iPAddress, ipAdapterAddress.flags));
					ptr = ipAdapterAddress.next;
				}
				return iPAddressInformationCollection;
			}
		}

		internal struct IpAdapterUnicastAddress
		{
			internal uint length;

			internal AdapterAddressFlags flags;

			internal IntPtr next;

			internal IpSocketAddress address;

			internal PrefixOrigin prefixOrigin;

			internal SuffixOrigin suffixOrigin;

			internal DuplicateAddressDetectionState dadState;

			internal uint validLifetime;

			internal uint preferredLifetime;

			internal uint leaseLifetime;

			internal byte prefixLength;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct IpAdapterAddresses
		{
			internal uint length;

			internal uint index;

			internal IntPtr next;

			[MarshalAs(UnmanagedType.LPStr)]
			internal string AdapterName;

			internal IntPtr firstUnicastAddress;

			internal IntPtr firstAnycastAddress;

			internal IntPtr firstMulticastAddress;

			internal IntPtr firstDnsServerAddress;

			internal string dnsSuffix;

			internal string description;

			internal string friendlyName;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			internal byte[] address;

			internal uint addressLength;

			internal AdapterFlags flags;

			internal uint mtu;

			internal NetworkInterfaceType type;

			internal OperationalStatus operStatus;

			internal uint ipv6Index;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			internal uint[] zoneIndices;

			internal IntPtr firstPrefix;

			internal ulong transmitLinkSpeed;

			internal ulong receiveLinkSpeed;

			internal IntPtr firstWinsServerAddress;

			internal IntPtr firstGatewayAddress;

			internal uint ipv4Metric;

			internal uint ipv6Metric;

			internal ulong luid;

			internal IpSocketAddress dhcpv4Server;

			internal uint compartmentId;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			internal byte[] networkGuid;

			internal InterfaceConnectionType connectionType;

			internal InterfaceTunnelType tunnelType;

			internal IpSocketAddress dhcpv6Server;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 130)]
			internal byte[] dhcpv6ClientDuid;

			internal uint dhcpv6ClientDuidLength;

			internal uint dhcpV6Iaid;
		}

		internal enum InterfaceConnectionType
		{
			Dedicated = 1,
			Passive,
			Demand,
			Maximum
		}

		internal enum InterfaceTunnelType
		{
			None = 0,
			Other = 1,
			Direct = 2,
			SixToFour = 11,
			Isatap = 13,
			Teredo = 14,
			IpHttps = 15
		}

		internal struct IpPerAdapterInfo
		{
			internal bool autoconfigEnabled;

			internal bool autoconfigActive;

			internal IntPtr currentDnsServer;

			internal IpAddrString dnsServerList;
		}

		internal struct IpAddrString
		{
			internal IntPtr Next;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
			internal string IpAddress;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
			internal string IpMask;

			internal uint Context;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct MibIfRow2
		{
			internal ulong interfaceLuid;

			internal uint interfaceIndex;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			internal byte[] interfaceGuid;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 257)]
			internal char[] alias;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 257)]
			internal char[] description;

			internal uint physicalAddressLength;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			internal byte[] physicalAddress;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			internal byte[] permanentPhysicalAddress;

			internal uint mtu;

			internal NetworkInterfaceType type;

			internal InterfaceTunnelType tunnelType;

			internal uint mediaType;

			internal uint physicalMediumType;

			internal uint accessType;

			internal uint directionType;

			internal byte interfaceAndOperStatusFlags;

			internal OperationalStatus operStatus;

			internal uint adminStatus;

			internal uint mediaConnectState;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			internal byte[] networkGuid;

			internal InterfaceConnectionType connectionType;

			internal ulong transmitLinkSpeed;

			internal ulong receiveLinkSpeed;

			internal ulong inOctets;

			internal ulong inUcastPkts;

			internal ulong inNUcastPkts;

			internal ulong inDiscards;

			internal ulong inErrors;

			internal ulong inUnknownProtos;

			internal ulong inUcastOctets;

			internal ulong inMulticastOctets;

			internal ulong inBroadcastOctets;

			internal ulong outOctets;

			internal ulong outUcastPkts;

			internal ulong outNUcastPkts;

			internal ulong outDiscards;

			internal ulong outErrors;

			internal ulong outUcastOctets;

			internal ulong outMulticastOctets;

			internal ulong outBroadcastOctets;

			internal ulong outQLen;
		}

		internal struct MibUdpStats
		{
			internal uint datagramsReceived;

			internal uint incomingDatagramsDiscarded;

			internal uint incomingDatagramsWithErrors;

			internal uint datagramsSent;

			internal uint udpListeners;
		}

		internal struct MibTcpStats
		{
			internal uint reTransmissionAlgorithm;

			internal uint minimumRetransmissionTimeOut;

			internal uint maximumRetransmissionTimeOut;

			internal uint maximumConnections;

			internal uint activeOpens;

			internal uint passiveOpens;

			internal uint failedConnectionAttempts;

			internal uint resetConnections;

			internal uint currentConnections;

			internal uint segmentsReceived;

			internal uint segmentsSent;

			internal uint segmentsResent;

			internal uint errorsReceived;

			internal uint segmentsSentWithReset;

			internal uint cumulativeConnections;
		}

		internal struct MibIpStats
		{
			internal bool forwardingEnabled;

			internal uint defaultTtl;

			internal uint packetsReceived;

			internal uint receivedPacketsWithHeaderErrors;

			internal uint receivedPacketsWithAddressErrors;

			internal uint packetsForwarded;

			internal uint receivedPacketsWithUnknownProtocols;

			internal uint receivedPacketsDiscarded;

			internal uint receivedPacketsDelivered;

			internal uint packetOutputRequests;

			internal uint outputPacketRoutingDiscards;

			internal uint outputPacketsDiscarded;

			internal uint outputPacketsWithNoRoute;

			internal uint packetReassemblyTimeout;

			internal uint packetsReassemblyRequired;

			internal uint packetsReassembled;

			internal uint packetsReassemblyFailed;

			internal uint packetsFragmented;

			internal uint packetsFragmentFailed;

			internal uint packetsFragmentCreated;

			internal uint interfaces;

			internal uint ipAddresses;

			internal uint routes;
		}

		internal struct MibIcmpInfo
		{
			internal MibIcmpStats inStats;

			internal MibIcmpStats outStats;
		}

		internal struct MibIcmpStats
		{
			internal uint messages;

			internal uint errors;

			internal uint destinationUnreachables;

			internal uint timeExceeds;

			internal uint parameterProblems;

			internal uint sourceQuenches;

			internal uint redirects;

			internal uint echoRequests;

			internal uint echoReplies;

			internal uint timestampRequests;

			internal uint timestampReplies;

			internal uint addressMaskRequests;

			internal uint addressMaskReplies;
		}

		internal struct MibIcmpInfoEx
		{
			internal MibIcmpStatsEx inStats;

			internal MibIcmpStatsEx outStats;
		}

		internal struct MibIcmpStatsEx
		{
			internal uint dwMsgs;

			internal uint dwErrors;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			internal uint[] rgdwTypeCount;
		}

		internal struct MibTcpTable
		{
			internal uint numberOfEntries;
		}

		internal struct MibTcpRow
		{
			internal TcpState state;

			internal uint localAddr;

			internal byte localPort1;

			internal byte localPort2;

			internal byte ignoreLocalPort3;

			internal byte ignoreLocalPort4;

			internal uint remoteAddr;

			internal byte remotePort1;

			internal byte remotePort2;

			internal byte ignoreRemotePort3;

			internal byte ignoreRemotePort4;
		}

		internal struct MibTcp6TableOwnerPid
		{
			internal uint numberOfEntries;
		}

		internal struct MibTcp6RowOwnerPid
		{
			internal unsafe fixed byte localAddr[16];

			internal uint localScopeId;

			internal byte localPort1;

			internal byte localPort2;

			internal byte ignoreLocalPort3;

			internal byte ignoreLocalPort4;

			internal unsafe fixed byte remoteAddr[16];

			internal uint remoteScopeId;

			internal byte remotePort1;

			internal byte remotePort2;

			internal byte ignoreRemotePort3;

			internal byte ignoreRemotePort4;

			internal TcpState state;

			internal uint owningPid;

			internal unsafe ReadOnlySpan<byte> localAddrAsSpan => MemoryMarshal.CreateSpan(ref localAddr[0], 16);

			internal unsafe ReadOnlySpan<byte> remoteAddrAsSpan => MemoryMarshal.CreateSpan(ref remoteAddr[0], 16);
		}

		internal enum TcpTableClass
		{
			TcpTableBasicListener,
			TcpTableBasicConnections,
			TcpTableBasicAll,
			TcpTableOwnerPidListener,
			TcpTableOwnerPidConnections,
			TcpTableOwnerPidAll,
			TcpTableOwnerModuleListener,
			TcpTableOwnerModuleConnections,
			TcpTableOwnerModuleAll
		}

		internal struct MibUdpTable
		{
			internal uint numberOfEntries;
		}

		internal struct MibUdpRow
		{
			internal uint localAddr;

			internal byte localPort1;

			internal byte localPort2;

			internal byte ignoreLocalPort3;

			internal byte ignoreLocalPort4;
		}

		internal enum UdpTableClass
		{
			UdpTableBasic,
			UdpTableOwnerPid,
			UdpTableOwnerModule
		}

		internal struct MibUdp6TableOwnerPid
		{
			internal uint numberOfEntries;
		}

		internal struct MibUdp6RowOwnerPid
		{
			internal unsafe fixed byte localAddr[16];

			internal uint localScopeId;

			internal byte localPort1;

			internal byte localPort2;

			internal byte ignoreLocalPort3;

			internal byte ignoreLocalPort4;

			internal uint owningPid;

			internal unsafe ReadOnlySpan<byte> localAddrAsSpan => MemoryMarshal.CreateSpan(ref localAddr[0], 16);
		}

		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		internal static extern uint GetNetworkParams(IntPtr pFixedInfo, ref uint pOutBufLen);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetAdaptersAddresses(AddressFamily family, uint flags, IntPtr pReserved, IntPtr adapterAddresses, ref uint outBufLen);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetBestInterfaceEx(byte[] ipAddress, out int index);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetIfEntry2(ref MibIfRow2 pIfRow);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetIpStatisticsEx(out MibIpStats statistics, AddressFamily family);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetTcpStatisticsEx(out MibTcpStats statistics, AddressFamily family);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetUdpStatisticsEx(out MibUdpStats statistics, AddressFamily family);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetIcmpStatistics(out MibIcmpInfo statistics);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetIcmpStatisticsEx(out MibIcmpInfoEx statistics, AddressFamily family);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetTcpTable(IntPtr pTcpTable, ref uint dwOutBufLen, bool order);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref uint dwOutBufLen, bool order, uint IPVersion, TcpTableClass tableClass, uint reserved);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetUdpTable(IntPtr pUdpTable, ref uint dwOutBufLen, bool order);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref uint dwOutBufLen, bool order, uint IPVersion, UdpTableClass tableClass, uint reserved);

		[DllImport("iphlpapi.dll")]
		internal static extern uint GetPerAdapterInfo(uint IfIndex, IntPtr pPerAdapterInfo, ref uint pOutBufLen);

		[DllImport("iphlpapi.dll")]
		internal static extern void FreeMibTable(IntPtr handle);

		[DllImport("iphlpapi.dll")]
		internal static extern uint CancelMibChangeNotify2(IntPtr notificationHandle);

		[DllImport("iphlpapi.dll")]
		internal unsafe static extern uint NotifyStableUnicastIpAddressTable(AddressFamily addressFamily, out SafeFreeMibTable table, delegate* unmanaged<IntPtr, IntPtr, void> callback, IntPtr context, out SafeCancelMibChangeNotify notificationHandle);
	}

	internal static class Winsock
	{
		[Flags]
		internal enum AsyncEventBits
		{
			FdNone = 0,
			FdRead = 1,
			FdWrite = 2,
			FdOob = 4,
			FdAccept = 8,
			FdConnect = 0x10,
			FdClose = 0x20,
			FdQos = 0x40,
			FdGroupQos = 0x80,
			FdRoutingInterfaceChange = 0x100,
			FdAddressListChange = 0x200,
			FdAllEvents = 0x3FF
		}

		[DllImport("ws2_32.dll", SetLastError = true)]
		internal static extern SocketError WSAEventSelect([In] SafeSocketHandle socketHandle, [In] SafeHandle Event, [In] AsyncEventBits NetworkEvents);

		[DllImport("ws2_32.dll", EntryPoint = "WSAIoctl", SetLastError = true)]
		internal static extern SocketError WSAIoctl_Blocking(SafeSocketHandle socketHandle, [In] int ioControlCode, [In] byte[] inBuffer, [In] int inBufferSize, [Out] byte[] outBuffer, [In] int outBufferSize, out int bytesTransferred, [In] IntPtr overlapped, [In] IntPtr completionRoutine);
	}
}
