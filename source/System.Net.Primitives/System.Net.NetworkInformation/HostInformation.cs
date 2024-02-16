namespace System.Net.NetworkInformation;

internal static class HostInformation
{
	internal static string DomainName => HostInformationPal.GetDomainName();
}
