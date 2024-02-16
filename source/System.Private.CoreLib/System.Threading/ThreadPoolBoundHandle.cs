using System.Runtime.InteropServices;

namespace System.Threading;

public sealed class ThreadPoolBoundHandle : IDisposable
{
	private readonly SafeHandle _handle;

	private bool _isDisposed;

	public SafeHandle Handle => _handle;

	private ThreadPoolBoundHandle(SafeHandle handle)
	{
		_handle = handle;
	}

	public static ThreadPoolBoundHandle BindHandle(SafeHandle handle)
	{
		if (handle == null)
		{
			throw new ArgumentNullException("handle");
		}
		if (handle.IsClosed || handle.IsInvalid)
		{
			throw new ArgumentException(SR.Argument_InvalidHandle, "handle");
		}
		return BindHandleCore(handle);
	}

	[CLSCompliant(false)]
	public unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData)
	{
		return AllocateNativeOverlapped(callback, state, pinData, flowExecutionContext: true);
	}

	[CLSCompliant(false)]
	public unsafe NativeOverlapped* UnsafeAllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData)
	{
		return AllocateNativeOverlapped(callback, state, pinData, flowExecutionContext: false);
	}

	private unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object state, object pinData, bool flowExecutionContext)
	{
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		EnsureNotDisposed();
		ThreadPoolBoundHandleOverlapped threadPoolBoundHandleOverlapped = new ThreadPoolBoundHandleOverlapped(callback, state, pinData, null, flowExecutionContext);
		threadPoolBoundHandleOverlapped._boundHandle = this;
		return threadPoolBoundHandleOverlapped._nativeOverlapped;
	}

	[CLSCompliant(false)]
	public unsafe NativeOverlapped* AllocateNativeOverlapped(PreAllocatedOverlapped preAllocated)
	{
		if (preAllocated == null)
		{
			throw new ArgumentNullException("preAllocated");
		}
		EnsureNotDisposed();
		preAllocated.AddRef();
		try
		{
			ThreadPoolBoundHandleOverlapped overlapped = preAllocated._overlapped;
			if (overlapped._boundHandle != null)
			{
				throw new ArgumentException(SR.Argument_PreAllocatedAlreadyAllocated, "preAllocated");
			}
			overlapped._boundHandle = this;
			return overlapped._nativeOverlapped;
		}
		catch
		{
			preAllocated.Release();
			throw;
		}
	}

	[CLSCompliant(false)]
	public unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
	{
		if (overlapped == null)
		{
			throw new ArgumentNullException("overlapped");
		}
		ThreadPoolBoundHandleOverlapped overlappedWrapper = GetOverlappedWrapper(overlapped);
		if (overlappedWrapper._boundHandle != this)
		{
			throw new ArgumentException(SR.Argument_NativeOverlappedWrongBoundHandle, "overlapped");
		}
		if (overlappedWrapper._preAllocated != null)
		{
			overlappedWrapper._preAllocated.Release();
		}
		else
		{
			Overlapped.Free(overlapped);
		}
	}

	[CLSCompliant(false)]
	public unsafe static object? GetNativeOverlappedState(NativeOverlapped* overlapped)
	{
		if (overlapped == null)
		{
			throw new ArgumentNullException("overlapped");
		}
		ThreadPoolBoundHandleOverlapped overlappedWrapper = GetOverlappedWrapper(overlapped);
		return overlappedWrapper._userState;
	}

	private unsafe static ThreadPoolBoundHandleOverlapped GetOverlappedWrapper(NativeOverlapped* overlapped)
	{
		try
		{
			return (ThreadPoolBoundHandleOverlapped)Overlapped.Unpack(overlapped);
		}
		catch (NullReferenceException innerException)
		{
			throw new ArgumentException(SR.Argument_NativeOverlappedAlreadyFree, "overlapped", innerException);
		}
	}

	public void Dispose()
	{
		_isDisposed = true;
	}

	private void EnsureNotDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(GetType().ToString());
		}
	}

	private static ThreadPoolBoundHandle BindHandleCore(SafeHandle handle)
	{
		try
		{
			bool flag = ThreadPool.BindHandle(handle);
		}
		catch (Exception ex)
		{
			if (ex.HResult == -2147024890)
			{
				throw new ArgumentException(SR.Argument_InvalidHandle, "handle");
			}
			if (ex.HResult == -2147024809)
			{
				throw new ArgumentException(SR.Argument_AlreadyBoundOrSyncHandle, "handle");
			}
			throw;
		}
		return new ThreadPoolBoundHandle(handle);
	}
}
