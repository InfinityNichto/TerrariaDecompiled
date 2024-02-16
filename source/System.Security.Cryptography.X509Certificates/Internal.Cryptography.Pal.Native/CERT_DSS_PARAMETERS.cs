namespace Internal.Cryptography.Pal.Native;

internal struct CERT_DSS_PARAMETERS
{
	public CRYPTOAPI_BLOB p;

	public CRYPTOAPI_BLOB q;

	public CRYPTOAPI_BLOB g;
}
