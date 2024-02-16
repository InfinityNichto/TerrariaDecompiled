namespace Internal.Cryptography.Pal.Native;

internal struct CERT_TRUST_STATUS
{
	public CertTrustErrorStatus dwErrorStatus;

	public CertTrustInfoStatus dwInfoStatus;
}
