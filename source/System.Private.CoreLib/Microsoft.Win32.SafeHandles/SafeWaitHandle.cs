using System;

namespace Microsoft.Win32.SafeHandles;

public sealed class SafeWaitHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafeWaitHandle()
		: base(ownsHandle: true)
	{
	}

	public SafeWaitHandle(IntPtr existingHandle, bool ownsHandle)
		: base(ownsHandle)
	{
		SetHandle(existingHandle);
	}

	protected override bool ReleaseHandle()
	{
		return Interop.Kernel32.CloseHandle(handle);
	}
}
