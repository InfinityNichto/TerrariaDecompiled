using System.Runtime.Versioning;

namespace System.Net.NetworkInformation;

public abstract class NetworkInterface
{
	[UnsupportedOSPlatform("illumos")]
	[UnsupportedOSPlatform("solaris")]
	public static int IPv6LoopbackInterfaceIndex => NetworkInterfacePal.IPv6LoopbackInterfaceIndex;

	[UnsupportedOSPlatform("illumos")]
	[UnsupportedOSPlatform("solaris")]
	public static int LoopbackInterfaceIndex => NetworkInterfacePal.LoopbackInterfaceIndex;

	public virtual string Id
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual string Name
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual string Description
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual OperationalStatus OperationalStatus
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual long Speed
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual bool IsReceiveOnly
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual bool SupportsMulticast
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual NetworkInterfaceType NetworkInterfaceType
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	[UnsupportedOSPlatform("illumos")]
	[UnsupportedOSPlatform("solaris")]
	public static NetworkInterface[] GetAllNetworkInterfaces()
	{
		return NetworkInterfacePal.GetAllNetworkInterfaces();
	}

	[UnsupportedOSPlatform("illumos")]
	[UnsupportedOSPlatform("solaris")]
	public static bool GetIsNetworkAvailable()
	{
		return NetworkInterfacePal.GetIsNetworkAvailable();
	}

	public virtual IPInterfaceProperties GetIPProperties()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual IPInterfaceStatistics GetIPStatistics()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual IPv4InterfaceStatistics GetIPv4Statistics()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual PhysicalAddress GetPhysicalAddress()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}
}
