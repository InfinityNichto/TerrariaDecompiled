namespace System.Net.NetworkInformation;

internal sealed class SystemIPAddressInformation : IPAddressInformation
{
	private readonly IPAddress _address;

	internal readonly bool Transient;

	internal readonly bool DnsEligible;

	public override IPAddress Address => _address;

	public override bool IsTransient => Transient;

	public override bool IsDnsEligible => DnsEligible;

	internal SystemIPAddressInformation(IPAddress address, global::Interop.IpHlpApi.AdapterAddressFlags flags)
	{
		_address = address;
		Transient = (flags & global::Interop.IpHlpApi.AdapterAddressFlags.Transient) > (global::Interop.IpHlpApi.AdapterAddressFlags)0;
		DnsEligible = (flags & global::Interop.IpHlpApi.AdapterAddressFlags.DnsEligible) > (global::Interop.IpHlpApi.AdapterAddressFlags)0;
	}
}
