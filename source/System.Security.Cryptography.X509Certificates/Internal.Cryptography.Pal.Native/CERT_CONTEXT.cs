using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_CONTEXT
{
	public CertEncodingType dwCertEncodingType;

	public unsafe byte* pbCertEncoded;

	public int cbCertEncoded;

	public unsafe CERT_INFO* pCertInfo;

	public IntPtr hCertStore;
}
