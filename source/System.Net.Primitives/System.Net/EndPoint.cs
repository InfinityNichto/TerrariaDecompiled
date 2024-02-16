using System.Net.Sockets;

namespace System.Net;

public abstract class EndPoint
{
	public virtual AddressFamily AddressFamily
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual SocketAddress Serialize()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual EndPoint Create(SocketAddress socketAddress)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}
}
