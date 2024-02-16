namespace System.Net.Internals;

internal enum SocketType
{
	Stream = 1,
	Dgram = 2,
	Raw = 3,
	Rdm = 4,
	Seqpacket = 5,
	Unknown = -1
}
