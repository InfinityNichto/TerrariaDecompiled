using System;
using System.Runtime.InteropServices;

internal static class Interop
{
	internal static class Kernel32
	{
		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "GetDriveTypeW", SetLastError = true)]
		internal static extern int GetDriveType(string drive);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "GetVolumeInformationW", SetLastError = true)]
		internal unsafe static extern bool GetVolumeInformation(string drive, char* volumeName, int volumeNameBufLen, int* volSerialNumber, int* maxFileNameLen, out int fileSystemFlags, char* fileSystemName, int fileSystemNameBufLen);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern int GetLogicalDrives();

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "GetDiskFreeSpaceExW", SetLastError = true)]
		internal static extern bool GetDiskFreeSpaceEx(string drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "SetVolumeLabelW", SetLastError = true)]
		internal static extern bool SetVolumeLabel(string driveLetter, string volumeName);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[SuppressGCTransition]
		internal static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

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
	}
}
