using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_SIMPLE_CHAIN
{
	public int cbSize;

	public CERT_TRUST_STATUS TrustStatus;

	public int cElement;

	public unsafe CERT_CHAIN_ELEMENT** rgpElement;

	public IntPtr pTrustListInfo;

	public int fHasRevocationFreshnessTime;

	public int dwRevocationFreshnessTime;
}
