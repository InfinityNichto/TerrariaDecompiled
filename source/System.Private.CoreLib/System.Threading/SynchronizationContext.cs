namespace System.Threading;

public class SynchronizationContext
{
	private bool _requireWaitNotification;

	public static SynchronizationContext? Current => Thread.CurrentThread._synchronizationContext;

	private static int InvokeWaitMethodHelper(SynchronizationContext syncContext, IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
	{
		return syncContext.Wait(waitHandles, waitAll, millisecondsTimeout);
	}

	protected void SetWaitNotificationRequired()
	{
		_requireWaitNotification = true;
	}

	public bool IsWaitNotificationRequired()
	{
		return _requireWaitNotification;
	}

	public virtual void Send(SendOrPostCallback d, object? state)
	{
		d(state);
	}

	public virtual void Post(SendOrPostCallback d, object? state)
	{
		ThreadPool.QueueUserWorkItem(delegate((SendOrPostCallback d, object state) s)
		{
			s.d(s.state);
		}, (d, state), preferLocal: false);
	}

	public virtual void OperationStarted()
	{
	}

	public virtual void OperationCompleted()
	{
	}

	[CLSCompliant(false)]
	public virtual int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
	{
		return WaitHelper(waitHandles, waitAll, millisecondsTimeout);
	}

	[CLSCompliant(false)]
	protected static int WaitHelper(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
	{
		if (waitHandles == null)
		{
			throw new ArgumentNullException("waitHandles");
		}
		return WaitHandle.WaitMultipleIgnoringSyncContext(waitHandles, waitAll, millisecondsTimeout);
	}

	public static void SetSynchronizationContext(SynchronizationContext? syncContext)
	{
		Thread.CurrentThread._synchronizationContext = syncContext;
	}

	public virtual SynchronizationContext CreateCopy()
	{
		return new SynchronizationContext();
	}
}
