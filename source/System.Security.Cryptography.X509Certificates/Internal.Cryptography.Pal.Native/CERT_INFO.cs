namespace Internal.Cryptography.Pal.Native;

internal struct CERT_INFO
{
	public int dwVersion;

	public CRYPTOAPI_BLOB SerialNumber;

	public CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;

	public CRYPTOAPI_BLOB Issuer;

	public FILETIME NotBefore;

	public FILETIME NotAfter;

	public CRYPTOAPI_BLOB Subject;

	public CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;

	public CRYPT_BIT_BLOB IssuerUniqueId;

	public CRYPT_BIT_BLOB SubjectUniqueId;

	public int cExtension;

	public unsafe CERT_EXTENSION* rgExtension;
}
