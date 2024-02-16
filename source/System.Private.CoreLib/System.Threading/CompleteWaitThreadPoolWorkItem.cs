namespace System.Threading;

internal sealed class CompleteWaitThreadPoolWorkItem : IThreadPoolWorkItem
{
	private RegisteredWaitHandle _registeredWaitHandle;

	private bool _timedOut;

	void IThreadPoolWorkItem.Execute()
	{
		CompleteWait();
	}

	private void CompleteWait()
	{
		PortableThreadPool.CompleteWait(_registeredWaitHandle, _timedOut);
	}

	public CompleteWaitThreadPoolWorkItem(RegisteredWaitHandle registeredWaitHandle, bool timedOut)
	{
		_registeredWaitHandle = registeredWaitHandle;
		_timedOut = timedOut;
	}
}
