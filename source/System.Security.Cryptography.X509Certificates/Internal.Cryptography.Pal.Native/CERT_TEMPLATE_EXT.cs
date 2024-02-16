using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_TEMPLATE_EXT
{
	public IntPtr pszObjId;

	public int dwMajorVersion;

	public int fMinorVersion;

	public int dwMinorVersion;
}
