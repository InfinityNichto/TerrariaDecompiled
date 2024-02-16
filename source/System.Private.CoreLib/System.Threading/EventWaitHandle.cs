using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

public class EventWaitHandle : WaitHandle
{
	public EventWaitHandle(bool initialState, EventResetMode mode)
		: this(initialState, mode, null, out var _)
	{
	}

	public EventWaitHandle(bool initialState, EventResetMode mode, string? name)
		: this(initialState, mode, name, out var _)
	{
	}

	public EventWaitHandle(bool initialState, EventResetMode mode, string? name, out bool createdNew)
	{
		if (mode != 0 && mode != EventResetMode.ManualReset)
		{
			throw new ArgumentException(SR.Argument_InvalidFlag, "mode");
		}
		CreateEventCore(initialState, mode, name, out createdNew);
	}

	[SupportedOSPlatform("windows")]
	public static EventWaitHandle OpenExisting(string name)
	{
		EventWaitHandle result;
		return OpenExistingWorker(name, out result) switch
		{
			OpenExistingResult.NameNotFound => throw new WaitHandleCannotBeOpenedException(), 
			OpenExistingResult.NameInvalid => throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name)), 
			OpenExistingResult.PathNotFound => throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, name)), 
			_ => result, 
		};
	}

	[SupportedOSPlatform("windows")]
	public static bool TryOpenExisting(string name, [NotNullWhen(true)] out EventWaitHandle? result)
	{
		return OpenExistingWorker(name, out result) == OpenExistingResult.Success;
	}

	private EventWaitHandle(SafeWaitHandle handle)
	{
		base.SafeWaitHandle = handle;
	}

	private void CreateEventCore(bool initialState, EventResetMode mode, string name, out bool createdNew)
	{
		uint num = (initialState ? 2u : 0u);
		if (mode == EventResetMode.ManualReset)
		{
			num |= 1u;
		}
		SafeWaitHandle safeWaitHandle = Interop.Kernel32.CreateEventEx(IntPtr.Zero, name, num, 34603010u);
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		if (safeWaitHandle.IsInvalid)
		{
			safeWaitHandle.SetHandleAsInvalid();
			if (!string.IsNullOrEmpty(name) && lastPInvokeError == 6)
			{
				throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
			}
			throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, name);
		}
		createdNew = lastPInvokeError != 183;
		base.SafeWaitHandle = safeWaitHandle;
	}

	private static OpenExistingResult OpenExistingWorker(string name, out EventWaitHandle result)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "name");
		}
		result = null;
		SafeWaitHandle safeWaitHandle = Interop.Kernel32.OpenEvent(34603010u, inheritHandle: false, name);
		if (safeWaitHandle.IsInvalid)
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			switch (lastPInvokeError)
			{
			case 2:
			case 123:
				return OpenExistingResult.NameNotFound;
			case 3:
				return OpenExistingResult.PathNotFound;
			case 6:
				return OpenExistingResult.NameInvalid;
			default:
				throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, name);
			}
		}
		result = new EventWaitHandle(safeWaitHandle);
		return OpenExistingResult.Success;
	}

	public bool Reset()
	{
		bool flag = Interop.Kernel32.ResetEvent(base.SafeWaitHandle);
		if (!flag)
		{
			throw Win32Marshal.GetExceptionForLastWin32Error();
		}
		return flag;
	}

	public bool Set()
	{
		bool flag = Interop.Kernel32.SetEvent(base.SafeWaitHandle);
		if (!flag)
		{
			throw Win32Marshal.GetExceptionForLastWin32Error();
		}
		return flag;
	}

	internal static bool Set(SafeWaitHandle waitHandle)
	{
		return Interop.Kernel32.SetEvent(waitHandle);
	}
}
