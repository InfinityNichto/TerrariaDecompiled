using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_CHAIN_POLICY_STATUS
{
	public int cbSize;

	public int dwError;

	public IntPtr lChainIndex;

	public IntPtr lElementIndex;

	public IntPtr pvExtraPolicyStatus;
}
