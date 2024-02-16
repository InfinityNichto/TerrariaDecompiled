namespace Internal.Cryptography.Pal.Native;

internal struct CERT_BASIC_CONSTRAINTS_INFO
{
	public CRYPT_BIT_BLOB SubjectType;

	public int fPathLenConstraint;

	public int dwPathLenConstraint;

	public int cSubtreesConstraint;

	public unsafe CRYPTOAPI_BLOB* rgSubtreesConstraint;
}
