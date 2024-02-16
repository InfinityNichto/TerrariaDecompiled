using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CRYPT_ALGORITHM_IDENTIFIER
{
	public IntPtr pszObjId;

	public CRYPTOAPI_BLOB Parameters;
}
