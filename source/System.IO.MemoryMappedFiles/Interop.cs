using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal static class Kernel32
	{
		internal struct SECURITY_ATTRIBUTES
		{
			internal uint nLength;

			internal IntPtr lpSecurityDescriptor;

			internal BOOL bInheritHandle;
		}

		internal struct SYSTEM_INFO
		{
			internal ushort wProcessorArchitecture;

			internal ushort wReserved;

			internal int dwPageSize;

			internal IntPtr lpMinimumApplicationAddress;

			internal IntPtr lpMaximumApplicationAddress;

			internal IntPtr dwActiveProcessorMask;

			internal int dwNumberOfProcessors;

			internal int dwProcessorType;

			internal int dwAllocationGranularity;

			internal short wProcessorLevel;

			internal short wProcessorRevision;
		}

		internal struct MEMORY_BASIC_INFORMATION
		{
			internal unsafe void* BaseAddress;

			internal unsafe void* AllocationBase;

			internal uint AllocationProtect;

			internal UIntPtr RegionSize;

			internal uint State;

			internal uint Protect;

			internal uint Type;
		}

		internal struct MEMORYSTATUSEX
		{
			internal uint dwLength;

			internal uint dwMemoryLoad;

			internal ulong ullTotalPhys;

			internal ulong ullAvailPhys;

			internal ulong ullTotalPageFile;

			internal ulong ullAvailPageFile;

			internal ulong ullTotalVirtual;

			internal ulong ullAvailVirtual;

			internal ulong ullAvailExtendedVirtual;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateFileMappingW", SetLastError = true)]
		internal static extern SafeMemoryMappedFileHandle CreateFileMapping(SafeFileHandle hFile, ref SECURITY_ATTRIBUTES lpFileMappingAttributes, int flProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateFileMappingW", SetLastError = true)]
		internal static extern SafeMemoryMappedFileHandle CreateFileMapping(IntPtr hFile, ref SECURITY_ATTRIBUTES lpFileMappingAttributes, int flProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern SafeMemoryMappedViewHandle MapViewOfFile(SafeMemoryMappedFileHandle hFileMappingObject, int dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenFileMappingW", SetLastError = true)]
		internal static extern SafeMemoryMappedFileHandle OpenFileMapping(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpName);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern IntPtr VirtualAlloc(SafeHandle lpAddress, UIntPtr dwSize, int flAllocationType, int flProtect);

		[DllImport("kernel32.dll")]
		internal unsafe static extern BOOL GlobalMemoryStatusEx(MEMORYSTATUSEX* lpBuffer);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CloseHandle(IntPtr handle);

		[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Unicode, EntryPoint = "FormatMessageW", ExactSpelling = true, SetLastError = true)]
		private unsafe static extern int FormatMessage(int dwFlags, IntPtr lpSource, uint dwMessageId, int dwLanguageId, void* lpBuffer, int nSize, IntPtr arguments);

		internal static string GetMessage(int errorCode)
		{
			return GetMessage(errorCode, IntPtr.Zero);
		}

		internal unsafe static string GetMessage(int errorCode, IntPtr moduleHandle)
		{
			int num = 12800;
			if (moduleHandle != IntPtr.Zero)
			{
				num |= 0x800;
			}
			Span<char> span = stackalloc char[256];
			fixed (char* lpBuffer = span)
			{
				int num2 = FormatMessage(num, moduleHandle, (uint)errorCode, 0, lpBuffer, span.Length, IntPtr.Zero);
				if (num2 > 0)
				{
					return GetAndTrimString(span.Slice(0, num2));
				}
			}
			if (Marshal.GetLastWin32Error() == 122)
			{
				IntPtr intPtr = default(IntPtr);
				try
				{
					int num3 = FormatMessage(num | 0x100, moduleHandle, (uint)errorCode, 0, &intPtr, 0, IntPtr.Zero);
					if (num3 > 0)
					{
						return GetAndTrimString(new Span<char>((void*)intPtr, num3));
					}
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
			}
			return $"Unknown error (0x{errorCode:x})";
		}

		private static string GetAndTrimString(Span<char> buffer)
		{
			int num = buffer.Length;
			while (num > 0 && buffer[num - 1] <= ' ')
			{
				num--;
			}
			return buffer.Slice(0, num).ToString();
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool FlushViewOfFile(IntPtr lpBaseAddress, UIntPtr dwNumberOfBytesToFlush);

		[DllImport("kernel32.dll")]
		internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern UIntPtr VirtualQuery(SafeHandle lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, UIntPtr dwLength);
	}

	internal enum BOOL
	{
		FALSE,
		TRUE
	}

	public unsafe static void CheckForAvailableVirtualMemory(ulong nativeSize)
	{
		Kernel32.MEMORYSTATUSEX mEMORYSTATUSEX = default(Kernel32.MEMORYSTATUSEX);
		mEMORYSTATUSEX.dwLength = (uint)sizeof(Kernel32.MEMORYSTATUSEX);
		if (Kernel32.GlobalMemoryStatusEx(&mEMORYSTATUSEX) != 0)
		{
			ulong ullTotalVirtual = mEMORYSTATUSEX.ullTotalVirtual;
			if (nativeSize >= ullTotalVirtual)
			{
				throw new IOException(System.SR.IO_NotEnoughMemory);
			}
		}
	}

	public static SafeMemoryMappedFileHandle CreateFileMapping(SafeFileHandle hFile, ref Kernel32.SECURITY_ATTRIBUTES securityAttributes, int pageProtection, long maximumSize, string name)
	{
		SplitLong(maximumSize, out var high, out var low);
		return Kernel32.CreateFileMapping(hFile, ref securityAttributes, pageProtection, high, low, name);
	}

	public static SafeMemoryMappedFileHandle CreateFileMapping(IntPtr hFile, ref Kernel32.SECURITY_ATTRIBUTES securityAttributes, int pageProtection, long maximumSize, string name)
	{
		SplitLong(maximumSize, out var high, out var low);
		return Kernel32.CreateFileMapping(hFile, ref securityAttributes, pageProtection, high, low, name);
	}

	public static SafeMemoryMappedViewHandle MapViewOfFile(SafeMemoryMappedFileHandle hFileMappingObject, int desiredAccess, long fileOffset, UIntPtr numberOfBytesToMap)
	{
		SplitLong(fileOffset, out var high, out var low);
		return Kernel32.MapViewOfFile(hFileMappingObject, desiredAccess, high, low, numberOfBytesToMap);
	}

	public static SafeMemoryMappedFileHandle OpenFileMapping(int desiredAccess, bool inheritHandle, string name)
	{
		return Kernel32.OpenFileMapping(desiredAccess, inheritHandle, name);
	}

	public static IntPtr VirtualAlloc(SafeHandle baseAddress, UIntPtr size, int allocationType, int protection)
	{
		return Kernel32.VirtualAlloc(baseAddress, size, allocationType, protection);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SplitLong(long number, out int high, out int low)
	{
		high = (int)(number >> 32);
		low = (int)(number & 0xFFFFFFFFu);
	}
}
