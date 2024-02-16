using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeLocalAllocHandle : SafeBuffer
{
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

	protected override bool ReleaseHandle()
	{
		Marshal.FreeHGlobal(handle);
		return true;
	}
}
