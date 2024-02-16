namespace Internal.Cryptography.Pal.Native;

internal struct CERT_PUBLIC_KEY_INFO
{
	public CRYPT_ALGORITHM_IDENTIFIER Algorithm;

	public CRYPT_BIT_BLOB PublicKey;
}
