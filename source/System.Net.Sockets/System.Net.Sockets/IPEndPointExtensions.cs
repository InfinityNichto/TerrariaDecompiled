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

	public static EndPoint Create(this EndPoint thisObj, System.Net.Internals.SocketAddress socketAddress)
	{
		AddressFamily family = socketAddress.Family;
		if (family != thisObj.AddressFamily)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidAddressFamily, family.ToString(), thisObj.GetType().FullName, thisObj.AddressFamily.ToString()), "socketAddress");
		}
		switch (family)
		{
		case AddressFamily.InterNetwork:
		case AddressFamily.InterNetworkV6:
			if (socketAddress.Size < 8)
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_InvalidSocketAddressSize, socketAddress.GetType().FullName, thisObj.GetType().FullName), "socketAddress");
			}
			return socketAddress.GetIPEndPoint();
		case AddressFamily.Unknown:
			return thisObj;
		default:
		{
			SocketAddress netSocketAddress = GetNetSocketAddress(socketAddress);
			return thisObj.Create(netSocketAddress);
		}
		}
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

	internal static SocketAddress GetNetSocketAddress(System.Net.Internals.SocketAddress address)
	{
		SocketAddress socketAddress = new SocketAddress(address.Family, address.Size);
		for (int i = 0; i < address.Size; i++)
		{
			socketAddress[i] = address[i];
		}
		return socketAddress;
	}
}
