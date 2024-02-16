namespace System.Net;

internal static class IPEndPointStatics
{
	internal static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, 0);

	internal static readonly IPEndPoint IPv6Any = new IPEndPoint(IPAddress.IPv6Any, 0);
}
