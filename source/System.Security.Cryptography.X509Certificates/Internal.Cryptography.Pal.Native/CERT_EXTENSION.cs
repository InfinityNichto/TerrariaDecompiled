using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_EXTENSION
{
	public IntPtr pszObjId;

	public int fCritical;

	public CRYPTOAPI_BLOB Value;
}
