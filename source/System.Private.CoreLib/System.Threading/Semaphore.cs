using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

public sealed class Semaphore : WaitHandle
{
	public Semaphore(int initialCount, int maximumCount)
		: this(initialCount, maximumCount, null)
	{
	}

	public Semaphore(int initialCount, int maximumCount, string? name)
		: this(initialCount, maximumCount, name, out var _)
	{
	}

	public Semaphore(int initialCount, int maximumCount, string? name, out bool createdNew)
	{
		if (initialCount < 0)
		{
			throw new ArgumentOutOfRangeException("initialCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (maximumCount < 1)
		{
			throw new ArgumentOutOfRangeException("maximumCount", SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (initialCount > maximumCount)
		{
			throw new ArgumentException(SR.Argument_SemaphoreInitialMaximum);
		}
		CreateSemaphoreCore(initialCount, maximumCount, name, out createdNew);
	}

	[SupportedOSPlatform("windows")]
	public static Semaphore OpenExisting(string name)
	{
		Semaphore result;
		return OpenExistingWorker(name, out result) switch
		{
			OpenExistingResult.NameNotFound => throw new WaitHandleCannotBeOpenedException(), 
			OpenExistingResult.NameInvalid => throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name)), 
			OpenExistingResult.PathNotFound => throw new IOException(SR.Format(SR.IO_PathNotFound_Path, name)), 
			_ => result, 
		};
	}

	[SupportedOSPlatform("windows")]
	public static bool TryOpenExisting(string name, [NotNullWhen(true)] out Semaphore? result)
	{
		return OpenExistingWorker(name, out result) == OpenExistingResult.Success;
	}

	public int Release()
	{
		return ReleaseCore(1);
	}

	public int Release(int releaseCount)
	{
		if (releaseCount < 1)
		{
			throw new ArgumentOutOfRangeException("releaseCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return ReleaseCore(releaseCount);
	}

	private Semaphore(SafeWaitHandle handle)
	{
		base.SafeWaitHandle = handle;
	}

	private void CreateSemaphoreCore(int initialCount, int maximumCount, string name, out bool createdNew)
	{
		SafeWaitHandle safeWaitHandle = Interop.Kernel32.CreateSemaphoreEx(IntPtr.Zero, initialCount, maximumCount, name, 0u, 34603010u);
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		if (safeWaitHandle.IsInvalid)
		{
			if (!string.IsNullOrEmpty(name) && lastPInvokeError == 6)
			{
				throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
			}
			throw Win32Marshal.GetExceptionForLastWin32Error();
		}
		createdNew = lastPInvokeError != 183;
		base.SafeWaitHandle = safeWaitHandle;
	}

	private static OpenExistingResult OpenExistingWorker(string name, out Semaphore result)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "name");
		}
		SafeWaitHandle safeWaitHandle = Interop.Kernel32.OpenSemaphore(34603010u, inheritHandle: false, name);
		if (safeWaitHandle.IsInvalid)
		{
			result = null;
			switch (Marshal.GetLastPInvokeError())
			{
			case 2:
			case 123:
				return OpenExistingResult.NameNotFound;
			case 3:
				return OpenExistingResult.PathNotFound;
			case 6:
				return OpenExistingResult.NameInvalid;
			default:
				throw Win32Marshal.GetExceptionForLastWin32Error();
			}
		}
		result = new Semaphore(safeWaitHandle);
		return OpenExistingResult.Success;
	}

	private int ReleaseCore(int releaseCount)
	{
		if (!Interop.Kernel32.ReleaseSemaphore(base.SafeWaitHandle, releaseCount, out var previousCount))
		{
			throw new SemaphoreFullException();
		}
		return previousCount;
	}
}
