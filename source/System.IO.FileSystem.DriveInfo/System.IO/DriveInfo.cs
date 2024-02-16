using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.IO;

public sealed class DriveInfo : ISerializable
{
	private readonly string _name;

	public string Name => _name;

	public bool IsReady => Directory.Exists(Name);

	public DirectoryInfo RootDirectory => new DirectoryInfo(Name);

	public DriveType DriveType => (DriveType)global::Interop.Kernel32.GetDriveType(Name);

	public unsafe string DriveFormat
	{
		get
		{
			char* ptr = stackalloc char[261];
			using (System.IO.DisableMediaInsertionPrompt.Create())
			{
				if (!global::Interop.Kernel32.GetVolumeInformation(Name, null, 0, null, null, out var _, ptr, 261))
				{
					throw Error.GetExceptionForLastWin32DriveError(Name);
				}
			}
			return new string(ptr);
		}
	}

	public long AvailableFreeSpace
	{
		get
		{
			uint lpOldMode;
			bool flag = global::Interop.Kernel32.SetThreadErrorMode(1u, out lpOldMode);
			try
			{
				if (!global::Interop.Kernel32.GetDiskFreeSpaceEx(Name, out var freeBytesForUser, out var _, out var _))
				{
					throw Error.GetExceptionForLastWin32DriveError(Name);
				}
				return freeBytesForUser;
			}
			finally
			{
				if (flag)
				{
					global::Interop.Kernel32.SetThreadErrorMode(lpOldMode, out var _);
				}
			}
		}
	}

	public long TotalFreeSpace
	{
		get
		{
			uint lpOldMode;
			bool flag = global::Interop.Kernel32.SetThreadErrorMode(1u, out lpOldMode);
			try
			{
				if (!global::Interop.Kernel32.GetDiskFreeSpaceEx(Name, out var _, out var _, out var freeBytes))
				{
					throw Error.GetExceptionForLastWin32DriveError(Name);
				}
				return freeBytes;
			}
			finally
			{
				if (flag)
				{
					global::Interop.Kernel32.SetThreadErrorMode(lpOldMode, out var _);
				}
			}
		}
	}

	public long TotalSize
	{
		get
		{
			global::Interop.Kernel32.SetThreadErrorMode(1u, out var lpOldMode);
			try
			{
				if (!global::Interop.Kernel32.GetDiskFreeSpaceEx(Name, out var _, out var totalBytes, out var _))
				{
					throw Error.GetExceptionForLastWin32DriveError(Name);
				}
				return totalBytes;
			}
			finally
			{
				global::Interop.Kernel32.SetThreadErrorMode(lpOldMode, out var _);
			}
		}
	}

	public unsafe string VolumeLabel
	{
		get
		{
			char* ptr = stackalloc char[261];
			using (System.IO.DisableMediaInsertionPrompt.Create())
			{
				if (!global::Interop.Kernel32.GetVolumeInformation(Name, ptr, 261, null, null, out var _, null, 0))
				{
					throw Error.GetExceptionForLastWin32DriveError(Name);
				}
			}
			return new string(ptr);
		}
		[SupportedOSPlatform("windows")]
		[param: AllowNull]
		set
		{
			uint lpOldMode;
			bool flag = global::Interop.Kernel32.SetThreadErrorMode(1u, out lpOldMode);
			try
			{
				if (!global::Interop.Kernel32.SetVolumeLabel(Name, value))
				{
					int lastPInvokeError = Marshal.GetLastPInvokeError();
					if (lastPInvokeError == 5)
					{
						throw new UnauthorizedAccessException(System.SR.InvalidOperation_SetVolumeLabelFailed);
					}
					throw Error.GetExceptionForWin32DriveError(lastPInvokeError, Name);
				}
			}
			finally
			{
				if (flag)
				{
					global::Interop.Kernel32.SetThreadErrorMode(lpOldMode, out var _);
				}
			}
		}
	}

	public DriveInfo(string driveName)
	{
		if (driveName == null)
		{
			throw new ArgumentNullException("driveName");
		}
		_name = NormalizeDriveName(driveName);
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public override string ToString()
	{
		return Name;
	}

	private static string NormalizeDriveName(string driveName)
	{
		return System.IO.DriveInfoInternal.NormalizeDriveName(driveName);
	}

	public static DriveInfo[] GetDrives()
	{
		string[] logicalDrives = System.IO.DriveInfoInternal.GetLogicalDrives();
		DriveInfo[] array = new DriveInfo[logicalDrives.Length];
		for (int i = 0; i < logicalDrives.Length; i++)
		{
			array[i] = new DriveInfo(logicalDrives[i]);
		}
		return array;
	}
}
