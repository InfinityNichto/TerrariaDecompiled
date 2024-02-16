using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Internal.NativeCrypto;

internal sealed class SafeAlgorithmHandle : SafeBCryptHandle
{
	protected sealed override bool ReleaseHandle()
	{
		uint num = BCryptCloseAlgorithmProvider(handle, 0);
		return num == 0;
	}

	[DllImport("BCrypt.dll")]
	private static extern uint BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, int dwFlags);
}
