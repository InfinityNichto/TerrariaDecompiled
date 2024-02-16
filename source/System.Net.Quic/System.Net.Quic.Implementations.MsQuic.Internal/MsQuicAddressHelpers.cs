using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal static class MsQuicAddressHelpers
{
	internal unsafe static IPEndPoint INetToIPEndPoint(ref MsQuicNativeMethods.SOCKADDR_INET inetAddress)
	{
		if (inetAddress.si_family == 2)
		{
			return new IPEndPoint(new IPAddress(MemoryMarshal.CreateReadOnlySpan(ref inetAddress.Ipv4.sin_addr[0], 4)), (ushort)IPAddress.NetworkToHostOrder((short)inetAddress.Ipv4.sin_port));
		}
		return new IPEndPoint(new IPAddress(MemoryMarshal.CreateReadOnlySpan(ref inetAddress.Ipv6.sin6_addr[0], 16)), (ushort)IPAddress.NetworkToHostOrder((short)inetAddress.Ipv6.sin6_port));
	}

	internal unsafe static MsQuicNativeMethods.SOCKADDR_INET IPEndPointToINet(IPEndPoint endpoint)
	{
		MsQuicNativeMethods.SOCKADDR_INET socketAddrInet = default(MsQuicNativeMethods.SOCKADDR_INET);
		if (!endpoint.Address.Equals(IPAddress.Any) && !endpoint.Address.Equals(IPAddress.IPv6Any))
		{
			int bytesWritten;
			switch (endpoint.Address.AddressFamily)
			{
			case AddressFamily.InterNetwork:
				endpoint.Address.TryWriteBytes(MemoryMarshal.CreateSpan(ref socketAddrInet.Ipv4.sin_addr[0], 4), out bytesWritten);
				socketAddrInet.Ipv4.sin_family = 2;
				break;
			case AddressFamily.InterNetworkV6:
				endpoint.Address.TryWriteBytes(MemoryMarshal.CreateSpan(ref socketAddrInet.Ipv6.sin6_addr[0], 16), out bytesWritten);
				socketAddrInet.Ipv6.sin6_family = 23;
				break;
			default:
				throw new ArgumentException(System.SR.net_quic_addressfamily_notsupported);
			}
		}
		SetPort(endpoint.Address.AddressFamily, ref socketAddrInet, endpoint.Port);
		return socketAddrInet;
	}

	private static void SetPort(AddressFamily addressFamily, ref MsQuicNativeMethods.SOCKADDR_INET socketAddrInet, int originalPort)
	{
		ushort sin_port = (ushort)IPAddress.HostToNetworkOrder((short)originalPort);
		socketAddrInet.Ipv4.sin_port = sin_port;
	}
}
