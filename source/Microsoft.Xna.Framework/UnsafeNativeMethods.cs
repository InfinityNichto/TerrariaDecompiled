using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework;

internal static class UnsafeNativeMethods
{
	public struct SecurityAttributes
	{
		private int Length;

		private IntPtr SecurityDescriptor;

		private int InheritHandle;

		public unsafe SecurityAttributes(bool inheritHandle)
		{
			Length = sizeof(SecurityAttributes);
			SecurityDescriptor = IntPtr.Zero;
			InheritHandle = (inheritHandle ? 1 : 0);
		}
	}

	public const int INFINITE = -1;

	public const uint STATUS_DLL_NOT_FOUND = 3221225781u;

	public const int PAGE_READONLY = 2;

	public const int PAGE_READWRITE = 4;

	public const int PAGE_WRITECOPY = 8;

	public const int FILE_MAP_WRITE = 2;

	public const int FILE_MAP_READ = 4;

	public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

	[DllImport("Kernel32.dll")]
	public static extern int CloseHandle(IntPtr hObject);

	[DllImport("Kernel32.dll")]
	public static extern IntPtr CreateEvent(ref SecurityAttributes lpEventAttributes, [MarshalAs(UnmanagedType.Bool)] bool bManualReset, [MarshalAs(UnmanagedType.Bool)] bool bInitialState, IntPtr lpName);

	[DllImport("Kernel32.dll")]
	public static extern int SetEvent(IntPtr hEvent);

	[DllImport("Kernel32.dll")]
	public unsafe static extern int WaitForMultipleObjects(int nCount, IntPtr* handles, [MarshalAs(UnmanagedType.Bool)] bool bWaitAll, int dwMilliseconds);

	[DllImport("Kernel32.dll")]
	public unsafe static extern IntPtr CreateFileMapping(IntPtr hFile, SecurityAttributes* lpFileMappingAttributes, int flProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);

	[DllImport("Kernel32.dll")]
	public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, int dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, int dwNumberOfBytesToMap);

	[DllImport("Kernel32.dll")]
	public static extern int UnmapViewOfFile(IntPtr lpBaseAddress);
}
