using System;

namespace Microsoft.Win32.SafeHandles;

public sealed class SafePipeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafePipeHandle()
		: this(new IntPtr(0), ownsHandle: true)
	{
	}

	public SafePipeHandle(IntPtr preexistingHandle, bool ownsHandle)
		: base(ownsHandle)
	{
		SetHandle(preexistingHandle, ownsHandle);
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.Kernel32.CloseHandle(handle);
	}

	internal void SetHandle(IntPtr descriptor, bool ownsHandle = true)
	{
		base.SetHandle(descriptor);
	}
}
