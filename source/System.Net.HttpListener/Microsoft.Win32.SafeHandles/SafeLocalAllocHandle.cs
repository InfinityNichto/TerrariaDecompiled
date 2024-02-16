using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeLocalAllocHandle : SafeBuffer
{
	internal static readonly Microsoft.Win32.SafeHandles.SafeLocalAllocHandle Zero = new Microsoft.Win32.SafeHandles.SafeLocalAllocHandle();

	public SafeLocalAllocHandle()
		: base(ownsHandle: true)
	{
	}

	internal static Microsoft.Win32.SafeHandles.SafeLocalAllocHandle LocalAlloc(int cb)
	{
		Microsoft.Win32.SafeHandles.SafeLocalAllocHandle safeLocalAllocHandle = new Microsoft.Win32.SafeHandles.SafeLocalAllocHandle();
		safeLocalAllocHandle.SetHandle(Marshal.AllocHGlobal(cb));
		safeLocalAllocHandle.Initialize((ulong)cb);
		return safeLocalAllocHandle;
	}

	protected override bool ReleaseHandle()
	{
		Marshal.FreeHGlobal(handle);
		return true;
	}
}
