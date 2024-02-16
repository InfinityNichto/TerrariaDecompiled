using Microsoft.Win32.SafeHandles;

namespace System.Threading;

public static class WaitHandleExtensions
{
	public static SafeWaitHandle GetSafeWaitHandle(this WaitHandle waitHandle)
	{
		if (waitHandle == null)
		{
			throw new ArgumentNullException("waitHandle");
		}
		return waitHandle.SafeWaitHandle;
	}

	public static void SetSafeWaitHandle(this WaitHandle waitHandle, SafeWaitHandle? value)
	{
		if (waitHandle == null)
		{
			throw new ArgumentNullException("waitHandle");
		}
		waitHandle.SafeWaitHandle = value;
	}
}
