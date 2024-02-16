using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_CHAIN_ENGINE_CONFIG
{
	public int cbSize;

	public IntPtr hRestrictedRoot;

	public IntPtr hRestrictedTrust;

	public IntPtr hRestrictedOther;

	public int cAdditionalStore;

	public IntPtr rghAdditionalStore;

	public ChainEngineConfigFlags dwFlags;

	public int dwUrlRetrievalTimeout;

	public int MaximumCachedCertificates;

	public int CycleDetectionModulus;

	public IntPtr hExclusiveRoot;

	public IntPtr hExclusiveTrustedPeople;
}
