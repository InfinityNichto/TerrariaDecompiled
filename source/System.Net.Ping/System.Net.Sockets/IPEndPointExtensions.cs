using System.Net.Internals;

namespace System.Net.Sockets;

internal static class IPEndPointExtensions
{
	public static System.Net.Internals.SocketAddress Serialize(EndPoint endpoint)
	{
		if (endpoint is IPEndPoint iPEndPoint)
		{
			return new System.Net.Internals.SocketAddress(iPEndPoint.Address, iPEndPoint.Port);
		}
		SocketAddress address = endpoint.Serialize();
		return GetInternalSocketAddress(address);
	}

	private static System.Net.Internals.SocketAddress GetInternalSocketAddress(SocketAddress address)
	{
		System.Net.Internals.SocketAddress socketAddress = new System.Net.Internals.SocketAddress(address.Family, address.Size);
		for (int i = 0; i < address.Size; i++)
		{
			socketAddress[i] = address[i];
		}
		return socketAddress;
	}
}
