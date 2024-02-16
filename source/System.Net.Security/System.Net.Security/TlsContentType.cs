namespace System.Net.Security;

internal enum TlsContentType : byte
{
	ChangeCipherSpec = 20,
	Alert,
	Handshake,
	AppData
}
