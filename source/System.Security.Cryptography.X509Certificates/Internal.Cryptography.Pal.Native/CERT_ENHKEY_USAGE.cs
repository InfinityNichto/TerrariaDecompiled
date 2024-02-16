using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_ENHKEY_USAGE
{
	public int cUsageIdentifier;

	public unsafe IntPtr* rgpszUsageIdentifier;
}
