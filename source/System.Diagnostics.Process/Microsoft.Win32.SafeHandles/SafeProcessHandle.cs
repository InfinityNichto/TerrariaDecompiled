using System;

namespace Microsoft.Win32.SafeHandles;

public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	internal static readonly SafeProcessHandle InvalidHandle = new SafeProcessHandle();

	public SafeProcessHandle()
		: this(IntPtr.Zero)
	{
	}

	internal SafeProcessHandle(IntPtr handle)
		: this(handle, ownsHandle: true)
	{
	}

	public SafeProcessHandle(IntPtr existingHandle, bool ownsHandle)
		: base(ownsHandle)
	{
		SetHandle(existingHandle);
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.Kernel32.CloseHandle(handle);
	}
}
