using System;
using Microsoft.Win32.SafeHandles;

namespace Internal.Win32.SafeHandles;

internal sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafeRegistryHandle()
		: base(ownsHandle: true)
	{
	}

	public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle)
		: base(ownsHandle)
	{
		SetHandle(preexistingHandle);
	}

	protected override bool ReleaseHandle()
	{
		return Interop.Advapi32.RegCloseKey(handle) == 0;
	}
}
