namespace System.Net.Security;

internal enum TlsHandshakeType : byte
{
	HelloRequest = 0,
	ClientHello = 1,
	ServerHello = 2,
	NewSessionTicket = 4,
	EndOfEarlyData = 5,
	EncryptedExtensions = 8,
	Certificate = 11,
	ServerKeyExchange = 12,
	CertificateRequest = 13,
	ServerHelloDone = 14,
	CertificateVerify = 15,
	ClientKeyExchange = 16,
	Finished = 20,
	KeyEpdate = 24,
	MessageHash = 254
}
