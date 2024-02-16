using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_CHAIN_CONTEXT
{
	public int cbSize;

	public CERT_TRUST_STATUS TrustStatus;

	public int cChain;

	public unsafe CERT_SIMPLE_CHAIN** rgpChain;

	public int cLowerQualityChainContext;

	public unsafe CERT_CHAIN_CONTEXT** rgpLowerQualityChainContext;

	public int fHasRevocationFreshnessTime;

	public int dwRevocationFreshnessTime;

	public int dwCreateFlags;

	public Guid ChainId;
}
