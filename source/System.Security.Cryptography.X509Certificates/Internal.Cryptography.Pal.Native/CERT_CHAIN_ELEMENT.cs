using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_CHAIN_ELEMENT
{
	public int cbSize;

	public unsafe CERT_CONTEXT* pCertContext;

	public CERT_TRUST_STATUS TrustStatus;

	public IntPtr pRevocationInfo;

	public IntPtr pIssuanceUsage;

	public IntPtr pApplicationUsage;

	public IntPtr pwszExtendedErrorInfo;
}
