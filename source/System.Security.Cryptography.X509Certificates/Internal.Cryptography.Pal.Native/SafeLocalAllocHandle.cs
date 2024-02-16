using System.Runtime.InteropServices;

namespace Internal.Cryptography.Pal.Native;

internal sealed class SafeLocalAllocHandle : SafePointerHandle<SafeLocalAllocHandle>
{
	public static SafeLocalAllocHandle Create(int cb)
	{
		SafeLocalAllocHandle safeLocalAllocHandle = new SafeLocalAllocHandle();
		safeLocalAllocHandle.SetHandle(Marshal.AllocHGlobal(cb));
		return safeLocalAllocHandle;
	}

	protected sealed override bool ReleaseHandle()
	{
		Marshal.FreeHGlobal(handle);
		return true;
	}
}
