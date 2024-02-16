using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal enum BOOL
	{
		FALSE,
		TRUE
	}

	internal static class Kernel32
	{
		internal struct FILE_TIME
		{
			internal uint dwLowDateTime;

			internal uint dwHighDateTime;
		}

		internal enum FINDEX_INFO_LEVELS : uint
		{
			FindExInfoStandard,
			FindExInfoBasic,
			FindExInfoMaxInfoLevel
		}

		internal enum FINDEX_SEARCH_OPS : uint
		{
			FindExSearchNameMatch,
			FindExSearchLimitToDirectories,
			FindExSearchLimitToDevices,
			FindExSearchMaxSearchOp
		}

		internal enum GET_FILEEX_INFO_LEVELS : uint
		{
			GetFileExInfoStandard,
			GetFileExMaxInfoLevel
		}

		internal struct SECURITY_ATTRIBUTES
		{
			internal uint nLength;

			internal IntPtr lpSecurityDescriptor;

			internal BOOL bInheritHandle;
		}

		internal struct WIN32_FILE_ATTRIBUTE_DATA
		{
			internal int dwFileAttributes;

			internal FILE_TIME ftCreationTime;

			internal FILE_TIME ftLastAccessTime;

			internal FILE_TIME ftLastWriteTime;

			internal uint nFileSizeHigh;

			internal uint nFileSizeLow;

			internal void PopulateFrom(ref WIN32_FIND_DATA findData)
			{
				dwFileAttributes = (int)findData.dwFileAttributes;
				ftCreationTime = findData.ftCreationTime;
				ftLastAccessTime = findData.ftLastAccessTime;
				ftLastWriteTime = findData.ftLastWriteTime;
				nFileSizeHigh = findData.nFileSizeHigh;
				nFileSizeLow = findData.nFileSizeLow;
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct WIN32_FIND_DATA
		{
			internal uint dwFileAttributes;

			internal FILE_TIME ftCreationTime;

			internal FILE_TIME ftLastAccessTime;

			internal FILE_TIME ftLastWriteTime;

			internal uint nFileSizeHigh;

			internal uint nFileSizeLow;

			internal uint dwReserved0;

			internal uint dwReserved1;

			private unsafe fixed char _cFileName[260];

			private unsafe fixed char _cAlternateFileName[14];
		}

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "CreateFileW", ExactSpelling = true, SetLastError = true)]
		private unsafe static extern SafeFileHandle CreateFilePrivate(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, SECURITY_ATTRIBUTES* lpSecurityAttributes, FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

		internal unsafe static SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, SECURITY_ATTRIBUTES* lpSecurityAttributes, FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile)
		{
			lpFileName = System.IO.PathInternal.EnsureExtendedPrefixIfNeeded(lpFileName);
			return CreateFilePrivate(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool FindClose(IntPtr hFindFile);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindFirstFileExW", ExactSpelling = true, SetLastError = true)]
		private static extern Microsoft.Win32.SafeHandles.SafeFindHandle FindFirstFileExPrivate(string lpFileName, FINDEX_INFO_LEVELS fInfoLevelId, ref WIN32_FIND_DATA lpFindFileData, FINDEX_SEARCH_OPS fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlags);

		internal static Microsoft.Win32.SafeHandles.SafeFindHandle FindFirstFile(string fileName, ref WIN32_FIND_DATA data)
		{
			fileName = System.IO.PathInternal.EnsureExtendedPrefixIfNeeded(fileName);
			return FindFirstFileExPrivate(fileName, FINDEX_INFO_LEVELS.FindExInfoBasic, ref data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, 0);
		}

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

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetFileAttributesExW", ExactSpelling = true, SetLastError = true)]
		private static extern bool GetFileAttributesExPrivate(string name, GET_FILEEX_INFO_LEVELS fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

		internal static bool GetFileAttributesEx(string name, GET_FILEEX_INFO_LEVELS fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation)
		{
			name = System.IO.PathInternal.EnsureExtendedPrefixIfNeeded(name);
			return GetFileAttributesExPrivate(name, fileInfoLevel, ref lpFileInformation);
		}

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[SuppressGCTransition]
		internal static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "CreateDirectoryW", SetLastError = true)]
		private static extern bool CreateDirectoryPrivate(string path, ref SECURITY_ATTRIBUTES lpSecurityAttributes);

		internal static bool CreateDirectory(string path, ref SECURITY_ATTRIBUTES lpSecurityAttributes)
		{
			path = System.IO.PathInternal.EnsureExtendedPrefix(path);
			return CreateDirectoryPrivate(path, ref lpSecurityAttributes);
		}
	}
}
