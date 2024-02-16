using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

[UnsupportedOSPlatform("browser")]
public sealed class RegisteredWaitHandle : MarshalByRefObject
{
	private IntPtr _nativeRegisteredWaitHandle = InvalidHandleValue;

	private bool _releaseHandle;

	private static AutoResetEvent s_cachedEvent;

	private static readonly LowLevelLock s_callbackLock = new LowLevelLock();

	private int _numRequestedCallbacks;

	private bool _signalAfterCallbacksComplete;

	private bool _unregisterCalled;

	private bool _unregistered;

	private AutoResetEvent _callbacksComplete;

	private AutoResetEvent _removed;

	internal _ThreadPoolWaitOrTimerCallback Callback { get; }

	internal SafeWaitHandle Handle { get; }

	internal int TimeoutTimeMs { get; private set; }

	internal int TimeoutDurationMs { get; }

	internal bool IsInfiniteTimeout => TimeoutDurationMs == -1;

	internal bool Repeating { get; }

	private SafeWaitHandle? UserUnregisterWaitHandle { get; set; }

	private IntPtr UserUnregisterWaitHandleValue { get; set; }

	private static IntPtr InvalidHandleValue => new IntPtr(-1);

	internal bool IsBlocking => UserUnregisterWaitHandleValue == InvalidHandleValue;

	internal PortableThreadPool.WaitThread? WaitThread { get; set; }

	private static bool IsValidHandle(IntPtr handle)
	{
		if (handle != InvalidHandleValue)
		{
			return handle != IntPtr.Zero;
		}
		return false;
	}

	internal void SetNativeRegisteredWaitHandle(IntPtr nativeRegisteredWaitHandle)
	{
		_nativeRegisteredWaitHandle = nativeRegisteredWaitHandle;
	}

	internal void OnBeforeRegister()
	{
		if (ThreadPool.UsePortableThreadPool)
		{
			GC.SuppressFinalize(this);
		}
		else
		{
			Handle.DangerousAddRef(ref _releaseHandle);
		}
	}

	public bool Unregister(WaitHandle waitObject)
	{
		if (ThreadPool.UsePortableThreadPool)
		{
			return UnregisterPortable(waitObject);
		}
		s_callbackLock.Acquire();
		try
		{
			if (!IsValidHandle(_nativeRegisteredWaitHandle) || !UnregisterWaitNative(_nativeRegisteredWaitHandle, waitObject?.SafeWaitHandle))
			{
				return false;
			}
			_nativeRegisteredWaitHandle = InvalidHandleValue;
			if (_releaseHandle)
			{
				Handle.DangerousRelease();
				_releaseHandle = false;
			}
		}
		finally
		{
			s_callbackLock.Release();
		}
		GC.SuppressFinalize(this);
		return true;
	}

	~RegisteredWaitHandle()
	{
		if (ThreadPool.UsePortableThreadPool)
		{
			return;
		}
		s_callbackLock.Acquire();
		try
		{
			if (IsValidHandle(_nativeRegisteredWaitHandle))
			{
				WaitHandleCleanupNative(_nativeRegisteredWaitHandle);
				_nativeRegisteredWaitHandle = InvalidHandleValue;
				if (_releaseHandle)
				{
					Handle.DangerousRelease();
					_releaseHandle = false;
				}
			}
		}
		finally
		{
			s_callbackLock.Release();
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void WaitHandleCleanupNative(IntPtr handle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool UnregisterWaitNative(IntPtr handle, SafeHandle waitObject);

	internal RegisteredWaitHandle(WaitHandle waitHandle, _ThreadPoolWaitOrTimerCallback callbackHelper, int millisecondsTimeout, bool repeating)
	{
		Handle = waitHandle.SafeWaitHandle;
		Callback = callbackHelper;
		TimeoutDurationMs = millisecondsTimeout;
		Repeating = repeating;
		if (!IsInfiniteTimeout)
		{
			RestartTimeout();
		}
	}

	private static AutoResetEvent RentEvent()
	{
		return Interlocked.Exchange(ref s_cachedEvent, null) ?? new AutoResetEvent(initialState: false);
	}

	private static void ReturnEvent(AutoResetEvent resetEvent)
	{
		if (Interlocked.CompareExchange(ref s_cachedEvent, resetEvent, null) != null)
		{
			resetEvent.Dispose();
		}
	}

	internal void RestartTimeout()
	{
		TimeoutTimeMs = Environment.TickCount + TimeoutDurationMs;
	}

	private bool UnregisterPortable(WaitHandle waitObject)
	{
		s_callbackLock.Acquire();
		bool success = false;
		try
		{
			if (_unregisterCalled)
			{
				return false;
			}
			UserUnregisterWaitHandle = waitObject?.SafeWaitHandle;
			UserUnregisterWaitHandle?.DangerousAddRef(ref success);
			UserUnregisterWaitHandleValue = UserUnregisterWaitHandle?.DangerousGetHandle() ?? IntPtr.Zero;
			if (_unregistered)
			{
				SignalUserWaitHandle();
				return true;
			}
			if (IsBlocking)
			{
				_callbacksComplete = RentEvent();
			}
			else
			{
				_removed = RentEvent();
			}
		}
		catch (Exception)
		{
			if (_removed != null)
			{
				ReturnEvent(_removed);
				_removed = null;
			}
			else if (_callbacksComplete != null)
			{
				ReturnEvent(_callbacksComplete);
				_callbacksComplete = null;
			}
			UserUnregisterWaitHandleValue = IntPtr.Zero;
			if (success)
			{
				UserUnregisterWaitHandle?.DangerousRelease();
			}
			UserUnregisterWaitHandle = null;
			throw;
		}
		finally
		{
			_unregisterCalled = true;
			s_callbackLock.Release();
		}
		WaitThread.UnregisterWait(this);
		return true;
	}

	private void SignalUserWaitHandle()
	{
		SafeWaitHandle userUnregisterWaitHandle = UserUnregisterWaitHandle;
		IntPtr userUnregisterWaitHandleValue = UserUnregisterWaitHandleValue;
		try
		{
			if (userUnregisterWaitHandleValue != IntPtr.Zero && userUnregisterWaitHandleValue != InvalidHandleValue)
			{
				EventWaitHandle.Set(userUnregisterWaitHandle);
			}
		}
		finally
		{
			userUnregisterWaitHandle?.DangerousRelease();
			_callbacksComplete?.Set();
			_unregistered = true;
		}
	}

	internal void PerformCallback(bool timedOut)
	{
		_ThreadPoolWaitOrTimerCallback.PerformWaitOrTimerCallback(Callback, timedOut);
		CompleteCallbackRequest();
	}

	internal void RequestCallback()
	{
		s_callbackLock.Acquire();
		try
		{
			_numRequestedCallbacks++;
		}
		finally
		{
			s_callbackLock.Release();
		}
	}

	internal void OnRemoveWait()
	{
		s_callbackLock.Acquire();
		try
		{
			_removed?.Set();
			if (_numRequestedCallbacks == 0)
			{
				SignalUserWaitHandle();
			}
			else
			{
				_signalAfterCallbacksComplete = true;
			}
		}
		finally
		{
			s_callbackLock.Release();
		}
	}

	private void CompleteCallbackRequest()
	{
		s_callbackLock.Acquire();
		try
		{
			_numRequestedCallbacks--;
			if (_numRequestedCallbacks == 0 && _signalAfterCallbacksComplete)
			{
				SignalUserWaitHandle();
			}
		}
		finally
		{
			s_callbackLock.Release();
		}
	}

	internal void WaitForCallbacks()
	{
		_callbacksComplete.WaitOne();
		ReturnEvent(_callbacksComplete);
		_callbacksComplete = null;
	}

	internal void WaitForRemoval()
	{
		_removed.WaitOne();
		ReturnEvent(_removed);
		_removed = null;
	}
}
