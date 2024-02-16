namespace System.Net;

internal static class GlobalSSPI
{
	internal static readonly SSPIAuthType SSPIAuth = new SSPIAuthType();

	internal static readonly SSPISecureChannelType SSPISecureChannel = new SSPISecureChannelType();
}
