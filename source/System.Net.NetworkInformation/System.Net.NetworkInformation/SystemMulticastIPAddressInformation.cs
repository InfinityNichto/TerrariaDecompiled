namespace System.Net.NetworkInformation;

internal sealed class SystemMulticastIPAddressInformation : MulticastIPAddressInformation
{
	private readonly SystemIPAddressInformation _innerInfo;

	public override IPAddress Address => _innerInfo.Address;

	public override bool IsTransient => _innerInfo.IsTransient;

	public override bool IsDnsEligible => _innerInfo.IsDnsEligible;

	public override PrefixOrigin PrefixOrigin => PrefixOrigin.Other;

	public override SuffixOrigin SuffixOrigin => SuffixOrigin.Other;

	public override DuplicateAddressDetectionState DuplicateAddressDetectionState => DuplicateAddressDetectionState.Invalid;

	public override long AddressValidLifetime => 0L;

	public override long AddressPreferredLifetime => 0L;

	public override long DhcpLeaseLifetime => 0L;

	public SystemMulticastIPAddressInformation(SystemIPAddressInformation addressInfo)
	{
		_innerInfo = addressInfo;
	}

	internal static MulticastIPAddressInformationCollection ToMulticastIpAddressInformationCollection(IPAddressInformationCollection addresses)
	{
		MulticastIPAddressInformationCollection multicastIPAddressInformationCollection = new MulticastIPAddressInformationCollection();
		foreach (IPAddressInformation address in addresses)
		{
			multicastIPAddressInformationCollection.InternalAdd(new SystemMulticastIPAddressInformation((SystemIPAddressInformation)address));
		}
		return multicastIPAddressInformationCollection;
	}
}
