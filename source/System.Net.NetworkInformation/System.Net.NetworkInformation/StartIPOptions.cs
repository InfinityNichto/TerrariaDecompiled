namespace System.Net.NetworkInformation;

[Flags]
internal enum StartIPOptions
{
	None = 0,
	StartIPv4 = 1,
	StartIPv6 = 2,
	Both = 3
}
