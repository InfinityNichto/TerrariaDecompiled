namespace System.Net.NetworkInformation;

internal sealed class SystemGatewayIPAddressInformation : GatewayIPAddressInformation
{
	private readonly IPAddress _address;

	public override IPAddress Address => _address;

	private SystemGatewayIPAddressInformation(IPAddress address)
	{
		_address = address;
	}

	internal static GatewayIPAddressInformationCollection ToGatewayIpAddressInformationCollection(IPAddressCollection addresses)
	{
		GatewayIPAddressInformationCollection gatewayIPAddressInformationCollection = new GatewayIPAddressInformationCollection();
		foreach (IPAddress address in addresses)
		{
			gatewayIPAddressInformationCollection.InternalAdd(new SystemGatewayIPAddressInformation(address));
		}
		return gatewayIPAddressInformationCollection;
	}
}
