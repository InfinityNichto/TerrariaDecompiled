using Microsoft.Win32.SafeHandles;

namespace System.Net.Security;

internal abstract class SafeFreeContextBuffer : SafeHandleZeroOrMinusOneIsInvalid
{
	protected SafeFreeContextBuffer()
		: base(ownsHandle: true)
	{
	}

	internal void Set(IntPtr value)
	{
		handle = value;
	}

	internal static int EnumeratePackages(out int pkgnum, out System.Net.Security.SafeFreeContextBuffer pkgArray)
	{
		int num = -1;
		System.Net.Security.SafeFreeContextBuffer_SECURITY safeFreeContextBuffer_SECURITY = null;
		num = global::Interop.SspiCli.EnumerateSecurityPackagesW(out pkgnum, out safeFreeContextBuffer_SECURITY);
		pkgArray = safeFreeContextBuffer_SECURITY;
		if (num != 0)
		{
			pkgArray?.SetHandleAsInvalid();
		}
		return num;
	}
}
