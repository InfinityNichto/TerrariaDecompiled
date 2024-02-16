using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

internal class LazyAsyncResult : IAsyncResult
{
	private class ThreadContext
	{
		internal int _nestedIOCount;
	}

	[ThreadStatic]
	private static ThreadContext t_threadContext;

	private readonly object _asyncObject;

	private readonly object _asyncState;

	private AsyncCallback _asyncCallback;

	private object _result;

	private int _intCompleted;

	private bool _endCalled;

	private bool _userEvent;

	private object _event;

	private static ThreadContext CurrentThreadContext
	{
		get
		{
			ThreadContext threadContext = t_threadContext;
			if (threadContext == null)
			{
				threadContext = (t_threadContext = new ThreadContext());
			}
			return threadContext;
		}
	}

	public object AsyncState => _asyncState;

	protected AsyncCallback AsyncCallback => _asyncCallback;

	public WaitHandle AsyncWaitHandle
	{
		get
		{
			_userEvent = true;
			if (_intCompleted == 0)
			{
				Interlocked.CompareExchange(ref _intCompleted, int.MinValue, 0);
			}
			ManualResetEvent waitHandle = (ManualResetEvent)_event;
			while (waitHandle == null)
			{
				LazilyCreateEvent(out waitHandle);
			}
			return waitHandle;
		}
	}

	public bool CompletedSynchronously
	{
		get
		{
			int num = _intCompleted;
			if (num == 0)
			{
				num = Interlocked.CompareExchange(ref _intCompleted, int.MinValue, 0);
			}
			return num > 0;
		}
	}

	public bool IsCompleted
	{
		get
		{
			int num = _intCompleted;
			if (num == 0)
			{
				num = Interlocked.CompareExchange(ref _intCompleted, int.MinValue, 0);
			}
			return (num & 0x7FFFFFFF) != 0;
		}
	}

	internal bool InternalPeekCompleted => (_intCompleted & 0x7FFFFFFF) != 0;

	internal bool EndCalled
	{
		get
		{
			return _endCalled;
		}
		set
		{
			_endCalled = value;
		}
	}

	internal LazyAsyncResult(object myObject, object myState, AsyncCallback myCallBack)
	{
		_asyncObject = myObject;
		_asyncState = myState;
		_asyncCallback = myCallBack;
		_result = DBNull.Value;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, ".ctor");
		}
	}

	private bool LazilyCreateEvent(out ManualResetEvent waitHandle)
	{
		waitHandle = new ManualResetEvent(initialState: false);
		try
		{
			if (Interlocked.CompareExchange(ref _event, waitHandle, null) == null)
			{
				if (InternalPeekCompleted)
				{
					waitHandle.Set();
				}
				return true;
			}
			waitHandle.Dispose();
			waitHandle = (ManualResetEvent)_event;
			return false;
		}
		catch
		{
			_event = null;
			waitHandle?.Dispose();
			throw;
		}
	}

	protected void ProtectedInvokeCallback(object result, IntPtr userToken)
	{
		if (result == DBNull.Value)
		{
			throw new ArgumentNullException("result");
		}
		if (((uint)_intCompleted & 0x7FFFFFFFu) != 0 || (Interlocked.Increment(ref _intCompleted) & 0x7FFFFFFF) != 1)
		{
			return;
		}
		if (_result == DBNull.Value)
		{
			_result = result;
		}
		ManualResetEvent manualResetEvent = (ManualResetEvent)_event;
		if (manualResetEvent != null)
		{
			try
			{
				manualResetEvent.Set();
			}
			catch (ObjectDisposedException)
			{
			}
		}
		Complete(userToken);
	}

	internal void InvokeCallback(object result)
	{
		ProtectedInvokeCallback(result, IntPtr.Zero);
	}

	internal void InvokeCallback()
	{
		ProtectedInvokeCallback(null, IntPtr.Zero);
	}

	protected virtual void Complete(IntPtr userToken)
	{
		bool flag = false;
		ThreadContext currentThreadContext = CurrentThreadContext;
		try
		{
			currentThreadContext._nestedIOCount++;
			if (_asyncCallback != null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Invoking callback", "Complete");
				}
				if (currentThreadContext._nestedIOCount >= 50)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, "*** OFFLOADED the user callback ****", "Complete");
					}
					Task.Factory.StartNew(delegate(object s)
					{
						WorkerThreadComplete(s);
					}, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
					flag = true;
				}
				else
				{
					_asyncCallback(this);
				}
			}
			else if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "No callback to invoke", "Complete");
			}
		}
		finally
		{
			currentThreadContext._nestedIOCount--;
			if (!flag)
			{
				Cleanup();
			}
		}
	}

	private static void WorkerThreadComplete(object state)
	{
		LazyAsyncResult lazyAsyncResult = (LazyAsyncResult)state;
		try
		{
			lazyAsyncResult._asyncCallback(lazyAsyncResult);
		}
		finally
		{
			lazyAsyncResult.Cleanup();
		}
	}

	protected virtual void Cleanup()
	{
	}

	internal object InternalWaitForCompletion()
	{
		return WaitForCompletion(snap: true);
	}

	private object WaitForCompletion(bool snap)
	{
		ManualResetEvent waitHandle = null;
		bool flag = false;
		if (!(snap ? IsCompleted : InternalPeekCompleted))
		{
			waitHandle = (ManualResetEvent)_event;
			if (waitHandle == null)
			{
				flag = LazilyCreateEvent(out waitHandle);
			}
		}
		if (waitHandle != null)
		{
			try
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"Waiting for completion event {waitHandle}", "WaitForCompletion");
				}
				waitHandle.WaitOne(-1);
			}
			catch (ObjectDisposedException)
			{
			}
			finally
			{
				if (flag && !_userEvent)
				{
					ManualResetEvent manualResetEvent = (ManualResetEvent)_event;
					_event = null;
					if (!_userEvent)
					{
						manualResetEvent?.Dispose();
					}
				}
			}
		}
		SpinWait spinWait = default(SpinWait);
		while (_result == DBNull.Value)
		{
			spinWait.SpinOnce();
		}
		return _result;
	}
}
