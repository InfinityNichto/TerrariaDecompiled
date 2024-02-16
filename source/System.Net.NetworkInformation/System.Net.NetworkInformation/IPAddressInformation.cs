namespace System.Net.NetworkInformation;

public abstract class IPAddressInformation
{
	public abstract IPAddress Address { get; }

	public abstract bool IsDnsEligible { get; }

	public abstract bool IsTransient { get; }
}
