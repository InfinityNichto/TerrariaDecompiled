using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading;

public static class Monitor
{
	public static long LockContentionCount => GetLockContentionCount();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void Enter(object obj);

	public static void Enter(object obj, ref bool lockTaken)
	{
		if (lockTaken)
		{
			ThrowLockTakenException();
		}
		ReliableEnter(obj, ref lockTaken);
	}

	[DoesNotReturn]
	private static void ThrowLockTakenException()
	{
		throw new ArgumentException(SR.Argument_MustBeFalse, "lockTaken");
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void ReliableEnter(object obj, ref bool lockTaken);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void Exit(object obj);

	public static bool TryEnter(object obj)
	{
		bool lockTaken = false;
		TryEnter(obj, 0, ref lockTaken);
		return lockTaken;
	}

	public static void TryEnter(object obj, ref bool lockTaken)
	{
		if (lockTaken)
		{
			ThrowLockTakenException();
		}
		ReliableEnterTimeout(obj, 0, ref lockTaken);
	}

	public static bool TryEnter(object obj, int millisecondsTimeout)
	{
		bool lockTaken = false;
		TryEnter(obj, millisecondsTimeout, ref lockTaken);
		return lockTaken;
	}

	public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
	{
		if (lockTaken)
		{
			ThrowLockTakenException();
		}
		ReliableEnterTimeout(obj, millisecondsTimeout, ref lockTaken);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void ReliableEnterTimeout(object obj, int timeout, ref bool lockTaken);

	public static bool IsEntered(object obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		return IsEnteredNative(obj);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool IsEnteredNative(object obj);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool ObjWait(int millisecondsTimeout, object obj);

	[UnsupportedOSPlatform("browser")]
	public static bool Wait(object obj, int millisecondsTimeout)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		return ObjWait(millisecondsTimeout, obj);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void ObjPulse(object obj);

	public static void Pulse(object obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		ObjPulse(obj);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void ObjPulseAll(object obj);

	public static void PulseAll(object obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		ObjPulseAll(obj);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern long GetLockContentionCount();

	public static bool TryEnter(object obj, TimeSpan timeout)
	{
		return TryEnter(obj, WaitHandle.ToTimeoutMilliseconds(timeout));
	}

	public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken)
	{
		TryEnter(obj, WaitHandle.ToTimeoutMilliseconds(timeout), ref lockTaken);
	}

	[UnsupportedOSPlatform("browser")]
	public static bool Wait(object obj, TimeSpan timeout)
	{
		return Wait(obj, WaitHandle.ToTimeoutMilliseconds(timeout));
	}

	[UnsupportedOSPlatform("browser")]
	public static bool Wait(object obj)
	{
		return Wait(obj, -1);
	}

	[UnsupportedOSPlatform("browser")]
	public static bool Wait(object obj, int millisecondsTimeout, bool exitContext)
	{
		return Wait(obj, millisecondsTimeout);
	}

	[UnsupportedOSPlatform("browser")]
	public static bool Wait(object obj, TimeSpan timeout, bool exitContext)
	{
		return Wait(obj, WaitHandle.ToTimeoutMilliseconds(timeout));
	}
}
