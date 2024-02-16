using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace System.Runtime.InteropServices;

public abstract class SafeHandle : CriticalFinalizerObject, IDisposable
{
	protected IntPtr handle;

	private volatile int _state;

	private readonly bool _ownsHandle;

	private volatile bool _fullyInitialized;

	internal bool OwnsHandle => _ownsHandle;

	public bool IsClosed => (_state & 1) == 1;

	public abstract bool IsInvalid { get; }

	protected SafeHandle(IntPtr invalidHandleValue, bool ownsHandle)
	{
		handle = invalidHandleValue;
		_state = 4;
		_ownsHandle = ownsHandle;
		if (!ownsHandle)
		{
			GC.SuppressFinalize(this);
		}
		_fullyInitialized = true;
	}

	~SafeHandle()
	{
		if (_fullyInitialized)
		{
			Dispose(disposing: false);
		}
	}

	protected internal void SetHandle(IntPtr handle)
	{
		this.handle = handle;
	}

	public IntPtr DangerousGetHandle()
	{
		return handle;
	}

	public void Close()
	{
		Dispose();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		InternalRelease(disposeOrFinalizeOperation: true);
	}

	public void SetHandleAsInvalid()
	{
		Interlocked.Or(ref _state, 1);
		GC.SuppressFinalize(this);
	}

	protected abstract bool ReleaseHandle();

	public void DangerousAddRef(ref bool success)
	{
		int state;
		int value;
		do
		{
			state = _state;
			if (((uint)state & (true ? 1u : 0u)) != 0)
			{
				throw new ObjectDisposedException("SafeHandle", SR.ObjectDisposed_SafeHandleClosed);
			}
			value = state + 4;
		}
		while (Interlocked.CompareExchange(ref _state, value, state) != state);
		success = true;
	}

	public void DangerousRelease()
	{
		InternalRelease(disposeOrFinalizeOperation: false);
	}

	private void InternalRelease(bool disposeOrFinalizeOperation)
	{
		bool flag = false;
		int state;
		int num;
		do
		{
			state = _state;
			if (disposeOrFinalizeOperation && ((uint)state & 2u) != 0)
			{
				return;
			}
			if ((state & -4) == 0)
			{
				throw new ObjectDisposedException("SafeHandle", SR.ObjectDisposed_SafeHandleClosed);
			}
			flag = (state & -3) == 4 && _ownsHandle && !IsInvalid;
			num = state - 4;
			if ((state & -4) == 4)
			{
				num |= 1;
			}
			if (disposeOrFinalizeOperation)
			{
				num |= 2;
			}
		}
		while (Interlocked.CompareExchange(ref _state, num, state) != state);
		if (flag)
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			ReleaseHandle();
			Marshal.SetLastPInvokeError(lastPInvokeError);
		}
	}
}
