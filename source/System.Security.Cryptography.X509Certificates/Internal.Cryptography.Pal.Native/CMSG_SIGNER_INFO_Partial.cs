namespace Internal.Cryptography.Pal.Native;

internal struct CMSG_SIGNER_INFO_Partial
{
	public int dwVersion;

	public CRYPTOAPI_BLOB Issuer;

	public CRYPTOAPI_BLOB SerialNumber;
}
