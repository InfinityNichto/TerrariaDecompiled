using System.Security.Principal;
using System.Threading;

namespace System.Net;

internal class ContextAwareResult : System.Net.LazyAsyncResult
{
	[Flags]
	private enum StateFlags : byte
	{
		None = 0,
		CaptureIdentity = 1,
		CaptureContext = 2,
		ThreadSafeContextCopy = 4,
		PostBlockStarted = 8,
		PostBlockFinished = 0x10
	}

	private volatile ExecutionContext _context;

	private object _lock;

	private StateFlags _flags;

	private WindowsIdentity _windowsIdentity;

	internal ExecutionContext ContextCopy
	{
		get
		{
			if (base.InternalPeekCompleted)
			{
				throw new InvalidOperationException(System.SR.net_completed_result);
			}
			ExecutionContext context = _context;
			if (context != null)
			{
				return context;
			}
			if ((_flags & StateFlags.PostBlockFinished) == 0)
			{
				lock (_lock)
				{
				}
			}
			if (base.InternalPeekCompleted)
			{
				throw new InvalidOperationException(System.SR.net_completed_result);
			}
			return _context;
		}
	}

	internal ContextAwareResult(bool captureIdentity, bool forceCaptureContext, object myObject, object myState, AsyncCallback myCallBack)
		: this(captureIdentity, forceCaptureContext, threadSafeContextCopy: false, myObject, myState, myCallBack)
	{
	}

	internal ContextAwareResult(bool captureIdentity, bool forceCaptureContext, bool threadSafeContextCopy, object myObject, object myState, AsyncCallback myCallBack)
		: base(myObject, myState, myCallBack)
	{
		if (forceCaptureContext)
		{
			_flags = StateFlags.CaptureContext;
		}
		if (captureIdentity)
		{
			_flags |= StateFlags.CaptureIdentity;
		}
		if (threadSafeContextCopy)
		{
			_flags |= StateFlags.ThreadSafeContextCopy;
		}
	}

	internal object StartPostingAsyncOp()
	{
		return StartPostingAsyncOp(lockCapture: true);
	}

	internal object StartPostingAsyncOp(bool lockCapture)
	{
		_lock = (lockCapture ? new object() : null);
		_flags |= StateFlags.PostBlockStarted;
		return _lock;
	}

	internal bool FinishPostingAsyncOp()
	{
		if ((_flags & (StateFlags.PostBlockStarted | StateFlags.PostBlockFinished)) != StateFlags.PostBlockStarted)
		{
			return false;
		}
		_flags |= StateFlags.PostBlockFinished;
		ExecutionContext cachedContext = null;
		return CaptureOrComplete(ref cachedContext, returnContext: false);
	}

	protected override void Cleanup()
	{
		base.Cleanup();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "Cleanup");
		}
		CleanupInternal();
	}

	private bool CaptureOrComplete(ref ExecutionContext cachedContext, bool returnContext)
	{
		bool flag = base.AsyncCallback != null || (_flags & StateFlags.CaptureContext) != 0;
		if ((_flags & StateFlags.CaptureIdentity) != 0 && !base.InternalPeekCompleted && !flag)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "starting identity capture", "CaptureOrComplete");
			}
			SafeCaptureIdentity();
		}
		if (flag && !base.InternalPeekCompleted)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "starting capture", "CaptureOrComplete");
			}
			if (cachedContext == null)
			{
				cachedContext = ExecutionContext.Capture();
			}
			if (cachedContext != null)
			{
				if (!returnContext)
				{
					_context = cachedContext;
					cachedContext = null;
				}
				else
				{
					_context = cachedContext;
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"_context:{_context}", "CaptureOrComplete");
			}
		}
		else
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "Skipping capture", "CaptureOrComplete");
			}
			cachedContext = null;
		}
		if (base.CompletedSynchronously)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "Completing synchronously", "CaptureOrComplete");
			}
			base.Complete(IntPtr.Zero);
			return true;
		}
		return false;
	}

	protected override void Complete(IntPtr userToken)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"_context(set):{_context != null} userToken:{userToken}", "Complete");
		}
		if ((_flags & StateFlags.PostBlockStarted) == 0)
		{
			base.Complete(userToken);
		}
		else
		{
			if (base.CompletedSynchronously)
			{
				return;
			}
			ExecutionContext context = _context;
			if (userToken != IntPtr.Zero || context == null)
			{
				base.Complete(userToken);
				return;
			}
			ExecutionContext.Run(context, delegate(object s)
			{
				((System.Net.ContextAwareResult)s).CompleteCallback();
			}, this);
		}
	}

	private void CompleteCallback()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "Context set, calling callback.", "CompleteCallback");
		}
		base.Complete(IntPtr.Zero);
	}

	private void SafeCaptureIdentity()
	{
		_windowsIdentity = WindowsIdentity.GetCurrent();
	}

	private void CleanupInternal()
	{
		if (_windowsIdentity != null)
		{
			_windowsIdentity.Dispose();
			_windowsIdentity = null;
		}
	}
}
