namespace System.Net.Security;

internal enum ExtensionType : ushort
{
	ServerName = 0,
	MaximumFagmentLength = 1,
	ClientCertificateUrl = 2,
	TrustedCaKeys = 3,
	TruncatedHmac = 4,
	CertificateStatusRequest = 5,
	ApplicationProtocols = 16,
	SupportedVersions = 43
}
