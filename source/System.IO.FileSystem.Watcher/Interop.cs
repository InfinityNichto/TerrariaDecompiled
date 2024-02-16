using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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
		internal struct SECURITY_ATTRIBUTES
		{
			internal uint nLength;

			internal IntPtr lpSecurityDescriptor;

			internal BOOL bInheritHandle;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal readonly struct FILE_NOTIFY_INFORMATION
		{
			internal readonly uint NextEntryOffset;

			internal readonly FileAction Action;

			internal readonly uint FileNameLength;
		}

		internal enum FileAction : uint
		{
			FILE_ACTION_ADDED = 1u,
			FILE_ACTION_REMOVED,
			FILE_ACTION_MODIFIED,
			FILE_ACTION_RENAMED_OLD_NAME,
			FILE_ACTION_RENAMED_NEW_NAME
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal unsafe static extern bool ReadDirectoryChangesW(SafeFileHandle hDirectory, byte[] lpBuffer, uint nBufferLength, bool bWatchSubtree, uint dwNotifyFilter, uint* lpBytesReturned, NativeOverlapped* lpOverlapped, void* lpCompletionRoutine);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "CreateFileW", ExactSpelling = true, SetLastError = true)]
		private unsafe static extern SafeFileHandle CreateFilePrivate(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, SECURITY_ATTRIBUTES* lpSecurityAttributes, FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

		internal unsafe static SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, FileMode dwCreationDisposition, int dwFlagsAndAttributes)
		{
			lpFileName = System.IO.PathInternal.EnsureExtendedPrefixIfNeeded(lpFileName);
			return CreateFilePrivate(lpFileName, dwDesiredAccess, dwShareMode, null, dwCreationDisposition, dwFlagsAndAttributes, IntPtr.Zero);
		}
	}
}
