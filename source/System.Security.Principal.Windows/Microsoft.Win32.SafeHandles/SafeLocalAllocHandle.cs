using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeLocalAllocHandle : SafeBuffer
{
	internal static SafeLocalAllocHandle InvalidHandle => new SafeLocalAllocHandle(IntPtr.Zero);

	public SafeLocalAllocHandle()
		: base(ownsHandle: true)
	{
	}

	internal static SafeLocalAllocHandle LocalAlloc(int cb)
	{
		SafeLocalAllocHandle safeLocalAllocHandle = new SafeLocalAllocHandle();
		safeLocalAllocHandle.SetHandle(Marshal.AllocHGlobal(cb));
		safeLocalAllocHandle.Initialize((ulong)cb);
		return safeLocalAllocHandle;
	}

	internal SafeLocalAllocHandle(IntPtr handle)
		: base(ownsHandle: true)
	{
		SetHandle(handle);
	}

	protected override bool ReleaseHandle()
	{
		Marshal.FreeHGlobal(handle);
		return true;
	}
}
