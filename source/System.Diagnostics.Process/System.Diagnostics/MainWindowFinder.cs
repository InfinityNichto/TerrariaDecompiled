using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics;

internal struct MainWindowFinder
{
	private IntPtr _bestHandle;

	private int _processId;

	public unsafe static IntPtr FindMainWindow(int processId)
	{
		Unsafe.SkipInit(out MainWindowFinder mainWindowFinder);
		mainWindowFinder._bestHandle = IntPtr.Zero;
		mainWindowFinder._processId = processId;
		global::Interop.User32.EnumWindows((delegate* unmanaged<IntPtr, IntPtr, global::Interop.BOOL>)(delegate*<IntPtr, IntPtr, global::Interop.BOOL>)(&EnumWindowsCallback), (IntPtr)(&mainWindowFinder));
		return mainWindowFinder._bestHandle;
	}

	private static bool IsMainWindow(IntPtr handle)
	{
		if (global::Interop.User32.GetWindow(handle, 4) == IntPtr.Zero)
		{
			return global::Interop.User32.IsWindowVisible(handle) != global::Interop.BOOL.FALSE;
		}
		return false;
	}

	[UnmanagedCallersOnly]
	private unsafe static global::Interop.BOOL EnumWindowsCallback(IntPtr handle, IntPtr extraParameter)
	{
		MainWindowFinder* ptr = (MainWindowFinder*)(void*)extraParameter;
		int num = 0;
		global::Interop.User32.GetWindowThreadProcessId(handle, &num);
		if (num == ptr->_processId && IsMainWindow(handle))
		{
			ptr->_bestHandle = handle;
			return global::Interop.BOOL.FALSE;
		}
		return global::Interop.BOOL.TRUE;
	}
}
