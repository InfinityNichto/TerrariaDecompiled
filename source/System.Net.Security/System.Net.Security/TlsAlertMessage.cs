namespace System.Net.Security;

internal enum TlsAlertMessage
{
	CloseNotify = 0,
	UnexpectedMessage = 10,
	BadRecordMac = 20,
	DecryptionFailed = 21,
	RecordOverflow = 22,
	DecompressionFail = 30,
	HandshakeFailure = 40,
	BadCertificate = 42,
	UnsupportedCert = 43,
	CertificateRevoked = 44,
	CertificateExpired = 45,
	CertificateUnknown = 46,
	IllegalParameter = 47,
	UnknownCA = 48,
	AccessDenied = 49,
	DecodeError = 50,
	DecryptError = 51,
	ExportRestriction = 60,
	ProtocolVersion = 70,
	InsuffientSecurity = 71,
	InternalError = 80,
	UserCanceled = 90,
	NoRenegotiation = 100,
	UnsupportedExt = 110
}
