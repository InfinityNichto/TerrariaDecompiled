using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

public abstract class WaitHandle : MarshalByRefObject, IDisposable
{
	internal const int MaxWaitHandles = 64;

	protected static readonly IntPtr InvalidHandle = new IntPtr(-1);

	private SafeWaitHandle _waitHandle;

	[ThreadStatic]
	private static SafeWaitHandle[] t_safeWaitHandlesForRent;

	internal const int WaitSuccess = 0;

	internal const int WaitAbandoned = 128;

	public const int WaitTimeout = 258;

	[Obsolete("WaitHandleHandle has been deprecated. Use the SafeWaitHandle property instead.")]
	public virtual IntPtr Handle
	{
		get
		{
			if (_waitHandle != null)
			{
				return _waitHandle.DangerousGetHandle();
			}
			return InvalidHandle;
		}
		set
		{
			if (value == InvalidHandle)
			{
				if (_waitHandle != null)
				{
					_waitHandle.SetHandleAsInvalid();
					_waitHandle = null;
				}
			}
			else
			{
				_waitHandle = new SafeWaitHandle(value, ownsHandle: true);
			}
		}
	}

	public SafeWaitHandle SafeWaitHandle
	{
		get
		{
			return _waitHandle ?? (_waitHandle = new SafeWaitHandle(InvalidHandle, ownsHandle: false));
		}
		[param: AllowNull]
		set
		{
			_waitHandle = value;
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int WaitOneCore(IntPtr waitHandle, int millisecondsTimeout);

	internal unsafe static int WaitMultipleIgnoringSyncContext(Span<IntPtr> waitHandles, bool waitAll, int millisecondsTimeout)
	{
		fixed (IntPtr* waitHandles2 = &MemoryMarshal.GetReference(waitHandles))
		{
			return WaitMultipleIgnoringSyncContext(waitHandles2, waitHandles.Length, waitAll, millisecondsTimeout);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern int WaitMultipleIgnoringSyncContext(IntPtr* waitHandles, int numHandles, bool waitAll, int millisecondsTimeout);

	private static int SignalAndWaitCore(IntPtr waitHandleToSignal, IntPtr waitHandleToWaitOn, int millisecondsTimeout)
	{
		int num = SignalAndWaitNative(waitHandleToSignal, waitHandleToWaitOn, millisecondsTimeout);
		if (num == 298)
		{
			throw new InvalidOperationException(SR.Threading_WaitHandleTooManyPosts);
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int SignalAndWaitNative(IntPtr waitHandleToSignal, IntPtr waitHandleToWaitOn, int millisecondsTimeout);

	internal static int ToTimeoutMilliseconds(TimeSpan timeout)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1)
		{
			throw new ArgumentOutOfRangeException("timeout", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("timeout", SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
		}
		return (int)num;
	}

	public virtual void Close()
	{
		Dispose();
	}

	protected virtual void Dispose(bool explicitDisposing)
	{
		_waitHandle?.Close();
	}

	public void Dispose()
	{
		Dispose(explicitDisposing: true);
		GC.SuppressFinalize(this);
	}

	public virtual bool WaitOne(int millisecondsTimeout)
	{
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		return WaitOneNoCheck(millisecondsTimeout);
	}

	private bool WaitOneNoCheck(int millisecondsTimeout)
	{
		SafeWaitHandle safeWaitHandle = _waitHandle ?? throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);
		bool success = false;
		try
		{
			safeWaitHandle.DangerousAddRef(ref success);
			SynchronizationContext current = SynchronizationContext.Current;
			int num = ((current == null || !current.IsWaitNotificationRequired()) ? WaitOneCore(safeWaitHandle.DangerousGetHandle(), millisecondsTimeout) : current.Wait(new IntPtr[1] { safeWaitHandle.DangerousGetHandle() }, waitAll: false, millisecondsTimeout));
			if (num == 128)
			{
				throw new AbandonedMutexException();
			}
			return num != 258;
		}
		finally
		{
			if (success)
			{
				safeWaitHandle.DangerousRelease();
			}
		}
	}

	private static SafeWaitHandle[] RentSafeWaitHandleArray(int capacity)
	{
		SafeWaitHandle[] array = t_safeWaitHandlesForRent;
		t_safeWaitHandlesForRent = null;
		int num = ((array != null) ? array.Length : 0);
		if (num < capacity)
		{
			array = new SafeWaitHandle[Math.Max(capacity, Math.Min(64, 2 * num))];
		}
		return array;
	}

	private static void ReturnSafeWaitHandleArray(SafeWaitHandle[] safeWaitHandles)
	{
		t_safeWaitHandlesForRent = safeWaitHandles;
	}

	private static void ObtainSafeWaitHandles(ReadOnlySpan<WaitHandle> waitHandles, Span<SafeWaitHandle> safeWaitHandles, Span<IntPtr> unsafeWaitHandles)
	{
		bool success = true;
		SafeWaitHandle safeWaitHandle = null;
		try
		{
			for (int i = 0; i < waitHandles.Length; i++)
			{
				WaitHandle waitHandle = waitHandles[i];
				if (waitHandle == null)
				{
					throw new ArgumentNullException("waitHandles[" + i + "]", SR.ArgumentNull_ArrayElement);
				}
				SafeWaitHandle safeWaitHandle2 = waitHandle._waitHandle ?? throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);
				safeWaitHandle = safeWaitHandle2;
				success = false;
				safeWaitHandle2.DangerousAddRef(ref success);
				safeWaitHandles[i] = safeWaitHandle2;
				unsafeWaitHandles[i] = safeWaitHandle2.DangerousGetHandle();
			}
		}
		catch
		{
			for (int j = 0; j < waitHandles.Length; j++)
			{
				SafeWaitHandle safeWaitHandle3 = safeWaitHandles[j];
				if (safeWaitHandle3 == null)
				{
					break;
				}
				safeWaitHandle3.DangerousRelease();
				safeWaitHandles[j] = null;
				if (safeWaitHandle3 == safeWaitHandle)
				{
					safeWaitHandle = null;
					success = true;
				}
			}
			if (!success)
			{
				safeWaitHandle.DangerousRelease();
			}
			throw;
		}
	}

	private static int WaitMultiple(WaitHandle[] waitHandles, bool waitAll, int millisecondsTimeout)
	{
		if (waitHandles == null)
		{
			throw new ArgumentNullException("waitHandles", SR.ArgumentNull_Waithandles);
		}
		return WaitMultiple(new ReadOnlySpan<WaitHandle>(waitHandles), waitAll, millisecondsTimeout);
	}

	private static int WaitMultiple(ReadOnlySpan<WaitHandle> waitHandles, bool waitAll, int millisecondsTimeout)
	{
		if (waitHandles.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyWaithandleArray, "waitHandles");
		}
		if (waitHandles.Length > 64)
		{
			throw new NotSupportedException(SR.NotSupported_MaxWaitHandles);
		}
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		SynchronizationContext current = SynchronizationContext.Current;
		bool flag = current?.IsWaitNotificationRequired() ?? false;
		SafeWaitHandle[] array = RentSafeWaitHandleArray(waitHandles.Length);
		try
		{
			int num;
			if (flag)
			{
				IntPtr[] array2 = new IntPtr[waitHandles.Length];
				ObtainSafeWaitHandles(waitHandles, array, array2);
				num = current.Wait(array2, waitAll, millisecondsTimeout);
			}
			else
			{
				Span<IntPtr> span = stackalloc IntPtr[waitHandles.Length];
				ObtainSafeWaitHandles(waitHandles, array, span);
				num = WaitMultipleIgnoringSyncContext(span, waitAll, millisecondsTimeout);
			}
			if (num >= 128 && num < 128 + waitHandles.Length)
			{
				if (waitAll)
				{
					throw new AbandonedMutexException();
				}
				num -= 128;
				throw new AbandonedMutexException(num, waitHandles[num]);
			}
			return num;
		}
		finally
		{
			for (int i = 0; i < waitHandles.Length; i++)
			{
				SafeWaitHandle safeWaitHandle = array[i];
				if (safeWaitHandle != null)
				{
					safeWaitHandle.DangerousRelease();
					array[i] = null;
				}
			}
			ReturnSafeWaitHandleArray(array);
		}
	}

	private static int WaitAnyMultiple(ReadOnlySpan<SafeWaitHandle> safeWaitHandles, int millisecondsTimeout)
	{
		SynchronizationContext current = SynchronizationContext.Current;
		if (current != null && current.IsWaitNotificationRequired())
		{
			IntPtr[] array = new IntPtr[safeWaitHandles.Length];
			for (int i = 0; i < safeWaitHandles.Length; i++)
			{
				array[i] = safeWaitHandles[i].DangerousGetHandle();
			}
			return current.Wait(array, waitAll: false, millisecondsTimeout);
		}
		Span<IntPtr> waitHandles = stackalloc IntPtr[safeWaitHandles.Length];
		for (int j = 0; j < safeWaitHandles.Length; j++)
		{
			waitHandles[j] = safeWaitHandles[j].DangerousGetHandle();
		}
		return WaitMultipleIgnoringSyncContext(waitHandles, waitAll: false, millisecondsTimeout);
	}

	private static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, int millisecondsTimeout)
	{
		if (toSignal == null)
		{
			throw new ArgumentNullException("toSignal");
		}
		if (toWaitOn == null)
		{
			throw new ArgumentNullException("toWaitOn");
		}
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		SafeWaitHandle waitHandle = toSignal._waitHandle;
		SafeWaitHandle waitHandle2 = toWaitOn._waitHandle;
		if (waitHandle == null || waitHandle2 == null)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);
		}
		bool success = false;
		bool success2 = false;
		try
		{
			waitHandle.DangerousAddRef(ref success);
			waitHandle2.DangerousAddRef(ref success2);
			int num = SignalAndWaitCore(waitHandle.DangerousGetHandle(), waitHandle2.DangerousGetHandle(), millisecondsTimeout);
			if (num == 128)
			{
				throw new AbandonedMutexException();
			}
			return num != 258;
		}
		finally
		{
			if (success2)
			{
				waitHandle2.DangerousRelease();
			}
			if (success)
			{
				waitHandle.DangerousRelease();
			}
		}
	}

	public virtual bool WaitOne(TimeSpan timeout)
	{
		return WaitOneNoCheck(ToTimeoutMilliseconds(timeout));
	}

	public virtual bool WaitOne()
	{
		return WaitOneNoCheck(-1);
	}

	public virtual bool WaitOne(int millisecondsTimeout, bool exitContext)
	{
		return WaitOne(millisecondsTimeout);
	}

	public virtual bool WaitOne(TimeSpan timeout, bool exitContext)
	{
		return WaitOneNoCheck(ToTimeoutMilliseconds(timeout));
	}

	public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout)
	{
		return WaitMultiple(waitHandles, waitAll: true, millisecondsTimeout) != 258;
	}

	public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout)
	{
		return WaitMultiple(waitHandles, waitAll: true, ToTimeoutMilliseconds(timeout)) != 258;
	}

	public static bool WaitAll(WaitHandle[] waitHandles)
	{
		return WaitMultiple(waitHandles, waitAll: true, -1) != 258;
	}

	public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
	{
		return WaitMultiple(waitHandles, waitAll: true, millisecondsTimeout) != 258;
	}

	public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout, bool exitContext)
	{
		return WaitMultiple(waitHandles, waitAll: true, ToTimeoutMilliseconds(timeout)) != 258;
	}

	public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout)
	{
		return WaitMultiple(waitHandles, waitAll: false, millisecondsTimeout);
	}

	internal static int WaitAny(ReadOnlySpan<SafeWaitHandle> safeWaitHandles, int millisecondsTimeout)
	{
		return WaitAnyMultiple(safeWaitHandles, millisecondsTimeout);
	}

	public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout)
	{
		return WaitMultiple(waitHandles, waitAll: false, ToTimeoutMilliseconds(timeout));
	}

	public static int WaitAny(WaitHandle[] waitHandles)
	{
		return WaitMultiple(waitHandles, waitAll: false, -1);
	}

	public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
	{
		return WaitMultiple(waitHandles, waitAll: false, millisecondsTimeout);
	}

	public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout, bool exitContext)
	{
		return WaitMultiple(waitHandles, waitAll: false, ToTimeoutMilliseconds(timeout));
	}

	public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn)
	{
		return SignalAndWait(toSignal, toWaitOn, -1);
	}

	public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, TimeSpan timeout, bool exitContext)
	{
		return SignalAndWait(toSignal, toWaitOn, ToTimeoutMilliseconds(timeout));
	}

	public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, int millisecondsTimeout, bool exitContext)
	{
		return SignalAndWait(toSignal, toWaitOn, millisecondsTimeout);
	}
}
