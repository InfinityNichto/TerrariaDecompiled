namespace Internal.Cryptography.Pal.Native;

internal struct CERT_CHAIN_PARA
{
	public int cbSize;

	public CERT_USAGE_MATCH RequestedUsage;

	public CERT_USAGE_MATCH RequestedIssuancePolicy;

	public int dwUrlRetrievalTimeout;

	public int fCheckRevocationFreshnessTime;

	public int dwRevocationFreshnessTime;

	public unsafe FILETIME* pftCacheResync;

	public int pStrongSignPara;

	public int dwStrongSignFlags;
}
