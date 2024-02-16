using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_CHAIN_POLICY_PARA
{
	public int cbSize;

	public int dwFlags;

	public IntPtr pvExtraPolicyPara;
}
