namespace System.Net.Sockets;

[Flags]
internal enum AddressInfoHints
{
	AI_PASSIVE = 1,
	AI_CANONNAME = 2,
	AI_NUMERICHOST = 4,
	AI_FQDN = 0x20000
}
