using System.Runtime.InteropServices;

namespace System.IO;

internal static class Error
{
	internal static Exception GetExceptionForLastWin32DriveError(string driveName)
	{
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		return GetExceptionForWin32DriveError(lastPInvokeError, driveName);
	}

	internal static Exception GetExceptionForWin32DriveError(int errorCode, string driveName)
	{
		if (errorCode == 3 || errorCode == 15)
		{
			return new DriveNotFoundException(System.SR.Format(System.SR.IO_DriveNotFound_Drive, driveName));
		}
		return System.IO.Win32Marshal.GetExceptionForWin32Error(errorCode, driveName);
	}
}
