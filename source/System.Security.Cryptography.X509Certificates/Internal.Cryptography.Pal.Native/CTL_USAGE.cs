using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CTL_USAGE
{
	public int cUsageIdentifier;

	public IntPtr rgpszUsageIdentifier;
}
