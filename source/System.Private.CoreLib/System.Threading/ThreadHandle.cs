namespace System.Threading;

internal readonly struct ThreadHandle
{
	private readonly IntPtr _ptr;

	internal ThreadHandle(IntPtr pThread)
	{
		_ptr = pThread;
	}
}
