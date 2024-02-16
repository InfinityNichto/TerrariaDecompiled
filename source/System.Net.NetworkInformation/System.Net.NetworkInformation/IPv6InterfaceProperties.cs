namespace System.Net.NetworkInformation;

public abstract class IPv6InterfaceProperties
{
	public abstract int Index { get; }

	public abstract int Mtu { get; }

	public virtual long GetScopeId(ScopeLevel scopeLevel)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}
}
