using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

public static class FileSystemAclExtensions
{
	public static DirectorySecurity GetAccessControl(this DirectoryInfo directoryInfo)
	{
		if (directoryInfo == null)
		{
			throw new ArgumentNullException("directoryInfo");
		}
		return new DirectorySecurity(directoryInfo.FullName, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
	}

	public static DirectorySecurity GetAccessControl(this DirectoryInfo directoryInfo, AccessControlSections includeSections)
	{
		if (directoryInfo == null)
		{
			throw new ArgumentNullException("directoryInfo");
		}
		return new DirectorySecurity(directoryInfo.FullName, includeSections);
	}

	public static void SetAccessControl(this DirectoryInfo directoryInfo, DirectorySecurity directorySecurity)
	{
		if (directorySecurity == null)
		{
			throw new ArgumentNullException("directorySecurity");
		}
		string fullPath = Path.GetFullPath(directoryInfo.FullName);
		directorySecurity.Persist(fullPath);
	}

	public static FileSecurity GetAccessControl(this FileInfo fileInfo)
	{
		if (fileInfo == null)
		{
			throw new ArgumentNullException("fileInfo");
		}
		return fileInfo.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
	}

	public static FileSecurity GetAccessControl(this FileInfo fileInfo, AccessControlSections includeSections)
	{
		if (fileInfo == null)
		{
			throw new ArgumentNullException("fileInfo");
		}
		return new FileSecurity(fileInfo.FullName, includeSections);
	}

	public static void SetAccessControl(this FileInfo fileInfo, FileSecurity fileSecurity)
	{
		if (fileInfo == null)
		{
			throw new ArgumentNullException("fileInfo");
		}
		if (fileSecurity == null)
		{
			throw new ArgumentNullException("fileSecurity");
		}
		string fullPath = Path.GetFullPath(fileInfo.FullName);
		fileSecurity.Persist(fullPath);
	}

	public static FileSecurity GetAccessControl(this FileStream fileStream)
	{
		if (fileStream == null)
		{
			throw new ArgumentNullException("fileStream");
		}
		SafeFileHandle safeFileHandle = fileStream.SafeFileHandle;
		if (safeFileHandle.IsClosed)
		{
			throw new ObjectDisposedException(null, System.SR.ObjectDisposed_FileClosed);
		}
		return new FileSecurity(safeFileHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
	}

	public static void SetAccessControl(this FileStream fileStream, FileSecurity fileSecurity)
	{
		if (fileStream == null)
		{
			throw new ArgumentNullException("fileStream");
		}
		if (fileSecurity == null)
		{
			throw new ArgumentNullException("fileSecurity");
		}
		SafeFileHandle safeFileHandle = fileStream.SafeFileHandle;
		if (safeFileHandle.IsClosed)
		{
			throw new ObjectDisposedException(null, System.SR.ObjectDisposed_FileClosed);
		}
		fileSecurity.Persist(safeFileHandle, fileStream.Name);
	}

	public static void Create(this DirectoryInfo directoryInfo, DirectorySecurity directorySecurity)
	{
		if (directoryInfo == null)
		{
			throw new ArgumentNullException("directoryInfo");
		}
		if (directorySecurity == null)
		{
			throw new ArgumentNullException("directorySecurity");
		}
		System.IO.FileSystem.CreateDirectory(directoryInfo.FullName, directorySecurity.GetSecurityDescriptorBinaryForm());
	}

	public static FileStream Create(this FileInfo fileInfo, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity fileSecurity)
	{
		if (fileInfo == null)
		{
			throw new ArgumentNullException("fileInfo");
		}
		if (fileSecurity == null)
		{
			throw new ArgumentNullException("fileSecurity");
		}
		FileShare fileShare = share & ~FileShare.Inheritable;
		if (mode < FileMode.CreateNew || mode > FileMode.Append)
		{
			throw new ArgumentOutOfRangeException("mode", System.SR.ArgumentOutOfRange_Enum);
		}
		if ((fileShare < FileShare.None) || fileShare > (FileShare.ReadWrite | FileShare.Delete))
		{
			throw new ArgumentOutOfRangeException("share", System.SR.ArgumentOutOfRange_Enum);
		}
		if (bufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		if ((rights & FileSystemRights.Write) == 0 && (mode == FileMode.Truncate || mode == FileMode.CreateNew || mode == FileMode.Create || mode == FileMode.Append))
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidFileModeAndFileSystemRightsCombo, mode, rights));
		}
		SafeFileHandle safeFileHandle = CreateFileHandle(fileInfo.FullName, mode, rights, share, options, fileSecurity);
		try
		{
			return new FileStream(safeFileHandle, GetFileStreamFileAccess(rights), bufferSize, (options & FileOptions.Asynchronous) != 0);
		}
		catch
		{
			safeFileHandle.Dispose();
			throw;
		}
	}

	public static DirectoryInfo CreateDirectory(this DirectorySecurity directorySecurity, string path)
	{
		if (directorySecurity == null)
		{
			throw new ArgumentNullException("directorySecurity");
		}
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(System.SR.Arg_PathEmpty);
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		directoryInfo.Create(directorySecurity);
		return directoryInfo;
	}

	private static FileAccess GetFileStreamFileAccess(FileSystemRights rights)
	{
		FileAccess fileAccess = (FileAccess)0;
		if ((rights & FileSystemRights.ReadData) != 0 || ((uint)rights & 0x80000000u) != 0)
		{
			fileAccess = FileAccess.Read;
		}
		if ((rights & FileSystemRights.WriteData) != 0 || (rights & (FileSystemRights)1073741824) != 0)
		{
			fileAccess = ((fileAccess == FileAccess.Read) ? FileAccess.ReadWrite : FileAccess.Write);
		}
		return fileAccess;
	}

	private unsafe static SafeFileHandle CreateFileHandle(string fullPath, FileMode mode, FileSystemRights rights, FileShare share, FileOptions options, FileSecurity security)
	{
		if (mode == FileMode.Append)
		{
			mode = FileMode.OpenOrCreate;
		}
		int dwFlagsAndAttributes = (int)(options | (FileOptions)1048576 | FileOptions.None);
		SafeFileHandle safeFileHandle;
		fixed (byte* ptr = security.GetSecurityDescriptorBinaryForm())
		{
			global::Interop.Kernel32.SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES = default(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
			sECURITY_ATTRIBUTES.nLength = (uint)sizeof(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
			sECURITY_ATTRIBUTES.bInheritHandle = (((share & FileShare.Inheritable) != 0) ? global::Interop.BOOL.TRUE : global::Interop.BOOL.FALSE);
			sECURITY_ATTRIBUTES.lpSecurityDescriptor = (IntPtr)ptr;
			global::Interop.Kernel32.SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES2 = sECURITY_ATTRIBUTES;
			using (System.IO.DisableMediaInsertionPrompt.Create())
			{
				safeFileHandle = global::Interop.Kernel32.CreateFile(fullPath, (int)rights, share, &sECURITY_ATTRIBUTES2, mode, dwFlagsAndAttributes, IntPtr.Zero);
				ValidateFileHandle(safeFileHandle, fullPath);
			}
		}
		return safeFileHandle;
	}

	private static void ValidateFileHandle(SafeFileHandle handle, string fullPath)
	{
		if (handle.IsInvalid)
		{
			int num = Marshal.GetLastWin32Error();
			if (num == 3 && fullPath.Length == Path.GetPathRoot(fullPath).Length)
			{
				num = 5;
			}
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(num, fullPath);
		}
	}
}
