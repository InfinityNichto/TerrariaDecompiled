using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

internal static class FileSystem
{
	internal static void VerifyValidPath(string path, string argName)
	{
		if (path == null)
		{
			throw new ArgumentNullException(argName);
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Arg_PathEmpty, argName);
		}
		if (path.Contains('\0'))
		{
			throw new ArgumentException(SR.Argument_InvalidPathChars, argName);
		}
	}

	public static bool DirectoryExists(string fullPath)
	{
		int lastError;
		return DirectoryExists(fullPath, out lastError);
	}

	private static bool DirectoryExists(string path, out int lastError)
	{
		Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		lastError = FillAttributeInfo(path, ref data, returnErrorOnNotFound: true);
		if (lastError == 0 && data.dwFileAttributes != -1)
		{
			return (data.dwFileAttributes & 0x10) != 0;
		}
		return false;
	}

	public static bool FileExists(string fullPath)
	{
		Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		if (FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: true) == 0 && data.dwFileAttributes != -1)
		{
			return (data.dwFileAttributes & 0x10) == 0;
		}
		return false;
	}

	internal static int FillAttributeInfo(string path, ref Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data, bool returnErrorOnNotFound)
	{
		int num = 0;
		path = PathInternal.TrimEndingDirectorySeparator(path);
		using (DisableMediaInsertionPrompt.Create())
		{
			if (!Interop.Kernel32.GetFileAttributesEx(path, Interop.Kernel32.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, ref data))
			{
				num = Marshal.GetLastWin32Error();
				if (!IsPathUnreachableError(num))
				{
					Interop.Kernel32.WIN32_FIND_DATA data2 = default(Interop.Kernel32.WIN32_FIND_DATA);
					using SafeFindHandle safeFindHandle = Interop.Kernel32.FindFirstFile(path, ref data2);
					if (safeFindHandle.IsInvalid)
					{
						num = Marshal.GetLastWin32Error();
					}
					else
					{
						num = 0;
						data.PopulateFrom(ref data2);
					}
				}
			}
		}
		if (num != 0 && !returnErrorOnNotFound && ((uint)(num - 2) <= 1u || num == 21))
		{
			data.dwFileAttributes = -1;
			return 0;
		}
		return num;
	}

	internal static bool IsPathUnreachableError(int errorCode)
	{
		switch (errorCode)
		{
		case 2:
		case 3:
		case 6:
		case 21:
		case 53:
		case 65:
		case 67:
		case 87:
		case 123:
		case 161:
		case 206:
		case 1231:
			return true;
		default:
			return false;
		}
	}

	public unsafe static void CreateDirectory(string fullPath, byte[] securityDescriptor = null)
	{
		if (DirectoryExists(fullPath))
		{
			return;
		}
		List<string> list = new List<string>();
		bool flag = false;
		int num = fullPath.Length;
		if (num >= 2 && PathInternal.EndsInDirectorySeparator(fullPath.AsSpan()))
		{
			num--;
		}
		int rootLength = PathInternal.GetRootLength(fullPath.AsSpan());
		if (num > rootLength)
		{
			int num2 = num - 1;
			while (num2 >= rootLength && !flag)
			{
				string text = fullPath.Substring(0, num2 + 1);
				if (!DirectoryExists(text))
				{
					list.Add(text);
				}
				else
				{
					flag = true;
				}
				while (num2 > rootLength && !PathInternal.IsDirectorySeparator(fullPath[num2]))
				{
					num2--;
				}
				num2--;
			}
		}
		int count = list.Count;
		bool flag2 = true;
		int num3 = 0;
		string path = fullPath;
		fixed (byte* ptr = securityDescriptor)
		{
			Interop.Kernel32.SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES = default(Interop.Kernel32.SECURITY_ATTRIBUTES);
			sECURITY_ATTRIBUTES.nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES);
			sECURITY_ATTRIBUTES.lpSecurityDescriptor = (IntPtr)ptr;
			Interop.Kernel32.SECURITY_ATTRIBUTES lpSecurityAttributes = sECURITY_ATTRIBUTES;
			while (list.Count > 0)
			{
				string text2 = list[list.Count - 1];
				list.RemoveAt(list.Count - 1);
				flag2 = Interop.Kernel32.CreateDirectory(text2, ref lpSecurityAttributes);
				if (!flag2 && num3 == 0)
				{
					int lastError = Marshal.GetLastWin32Error();
					if (lastError != 183)
					{
						num3 = lastError;
					}
					else if (FileExists(text2) || (!DirectoryExists(text2, out lastError) && lastError == 5))
					{
						num3 = lastError;
						path = text2;
					}
				}
			}
		}
		if (count == 0 && !flag)
		{
			string pathRoot = Path.GetPathRoot(fullPath);
			if (!DirectoryExists(pathRoot))
			{
				throw Win32Marshal.GetExceptionForWin32Error(3, pathRoot);
			}
		}
		else if (!flag2 && num3 != 0)
		{
			throw Win32Marshal.GetExceptionForWin32Error(num3, path);
		}
	}

	public static void Encrypt(string path)
	{
		string fullPath = Path.GetFullPath(path);
		if (!Interop.Advapi32.EncryptFile(fullPath))
		{
			ThrowExceptionEncryptDecryptFail(fullPath);
		}
	}

	public static void Decrypt(string path)
	{
		string fullPath = Path.GetFullPath(path);
		if (!Interop.Advapi32.DecryptFile(fullPath))
		{
			ThrowExceptionEncryptDecryptFail(fullPath);
		}
	}

	private unsafe static void ThrowExceptionEncryptDecryptFail(string fullPath)
	{
		int lastWin32Error = Marshal.GetLastWin32Error();
		if (lastWin32Error == 5)
		{
			string text = DriveInfoInternal.NormalizeDriveName(Path.GetPathRoot(fullPath));
			using (DisableMediaInsertionPrompt.Create())
			{
				if (!Interop.Kernel32.GetVolumeInformation(text, null, 0, null, null, out var fileSystemFlags, null, 0))
				{
					lastWin32Error = Marshal.GetLastWin32Error();
					throw Win32Marshal.GetExceptionForWin32Error(lastWin32Error, text);
				}
				if (((ulong)fileSystemFlags & 0x20000uL) == 0L)
				{
					throw new NotSupportedException(SR.PlatformNotSupported_FileEncryption);
				}
			}
		}
		throw Win32Marshal.GetExceptionForWin32Error(lastWin32Error, fullPath);
	}

	public static void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
	{
		int num = Interop.Kernel32.CopyFile(sourceFullPath, destFullPath, !overwrite);
		if (num == 0)
		{
			return;
		}
		string path = destFullPath;
		if (num != 80)
		{
			using (SafeFileHandle safeFileHandle = Interop.Kernel32.CreateFile(sourceFullPath, int.MinValue, FileShare.Read, FileMode.Open, 0))
			{
				if (safeFileHandle.IsInvalid)
				{
					path = sourceFullPath;
				}
			}
			if (num == 5 && DirectoryExists(destFullPath))
			{
				throw new IOException(SR.Format(SR.Arg_FileIsDirectory_Name, destFullPath), 5);
			}
		}
		throw Win32Marshal.GetExceptionForWin32Error(num, path);
	}

	public static void ReplaceFile(string sourceFullPath, string destFullPath, string destBackupFullPath, bool ignoreMetadataErrors)
	{
		int dwReplaceFlags = (ignoreMetadataErrors ? 2 : 0);
		if (!Interop.Kernel32.ReplaceFile(destFullPath, sourceFullPath, destBackupFullPath, dwReplaceFlags, IntPtr.Zero, IntPtr.Zero))
		{
			throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
		}
	}

	public static void DeleteFile(string fullPath)
	{
		if (!Interop.Kernel32.DeleteFile(fullPath))
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (lastWin32Error != 2)
			{
				throw Win32Marshal.GetExceptionForWin32Error(lastWin32Error, fullPath);
			}
		}
	}

	public static FileAttributes GetAttributes(string fullPath)
	{
		Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		int num = FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: true);
		if (num != 0)
		{
			throw Win32Marshal.GetExceptionForWin32Error(num, fullPath);
		}
		return (FileAttributes)data.dwFileAttributes;
	}

	public static DateTimeOffset GetCreationTime(string fullPath)
	{
		Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		int num = FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: false);
		if (num != 0)
		{
			throw Win32Marshal.GetExceptionForWin32Error(num, fullPath);
		}
		return data.ftCreationTime.ToDateTimeOffset();
	}

	public static DateTimeOffset GetLastAccessTime(string fullPath)
	{
		Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		int num = FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: false);
		if (num != 0)
		{
			throw Win32Marshal.GetExceptionForWin32Error(num, fullPath);
		}
		return data.ftLastAccessTime.ToDateTimeOffset();
	}

	public static DateTimeOffset GetLastWriteTime(string fullPath)
	{
		Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		int num = FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: false);
		if (num != 0)
		{
			throw Win32Marshal.GetExceptionForWin32Error(num, fullPath);
		}
		return data.ftLastWriteTime.ToDateTimeOffset();
	}

	public static void MoveDirectory(string sourceFullPath, string destFullPath)
	{
		if (!Interop.Kernel32.MoveFile(sourceFullPath, destFullPath, overwrite: false))
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			switch (lastWin32Error)
			{
			case 2:
				throw Win32Marshal.GetExceptionForWin32Error(3, sourceFullPath);
			case 183:
				throw Win32Marshal.GetExceptionForWin32Error(183, destFullPath);
			case 5:
				throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, sourceFullPath), Win32Marshal.MakeHRFromErrorCode(lastWin32Error));
			default:
				throw Win32Marshal.GetExceptionForWin32Error(lastWin32Error);
			}
		}
	}

	public static void MoveFile(string sourceFullPath, string destFullPath, bool overwrite)
	{
		if (!Interop.Kernel32.MoveFile(sourceFullPath, destFullPath, overwrite))
		{
			throw Win32Marshal.GetExceptionForLastWin32Error();
		}
	}

	private static SafeFileHandle OpenHandle(string fullPath, bool asDirectory)
	{
		string text = fullPath.Substring(0, PathInternal.GetRootLength(fullPath.AsSpan()));
		if (text == fullPath && text[1] == Path.VolumeSeparatorChar)
		{
			throw new ArgumentException(SR.Arg_PathIsVolume, "path");
		}
		SafeFileHandle safeFileHandle = Interop.Kernel32.CreateFile(fullPath, 1073741824, FileShare.ReadWrite | FileShare.Delete, FileMode.Open, asDirectory ? 33554432 : 0);
		if (safeFileHandle.IsInvalid)
		{
			int num = Marshal.GetLastWin32Error();
			if (!asDirectory && num == 3 && fullPath.Equals(Directory.GetDirectoryRoot(fullPath)))
			{
				num = 5;
			}
			throw Win32Marshal.GetExceptionForWin32Error(num, fullPath);
		}
		return safeFileHandle;
	}

	public static void RemoveDirectory(string fullPath, bool recursive)
	{
		if (!recursive)
		{
			RemoveDirectoryInternal(fullPath, topLevel: true);
			return;
		}
		Interop.Kernel32.WIN32_FIND_DATA findData = default(Interop.Kernel32.WIN32_FIND_DATA);
		GetFindData(fullPath, isDirectory: true, ignoreAccessDenied: true, ref findData);
		if (IsNameSurrogateReparsePoint(ref findData))
		{
			RemoveDirectoryInternal(fullPath, topLevel: true);
			return;
		}
		fullPath = PathInternal.EnsureExtendedPrefix(fullPath);
		RemoveDirectoryRecursive(fullPath, ref findData, topLevel: true);
	}

	private static void GetFindData(string fullPath, bool isDirectory, bool ignoreAccessDenied, ref Interop.Kernel32.WIN32_FIND_DATA findData)
	{
		using SafeFindHandle safeFindHandle = Interop.Kernel32.FindFirstFile(Path.TrimEndingDirectorySeparator(fullPath), ref findData);
		if (safeFindHandle.IsInvalid)
		{
			int num = Marshal.GetLastWin32Error();
			if (isDirectory && num == 2)
			{
				num = 3;
			}
			if (!(isDirectory && num == 5 && ignoreAccessDenied))
			{
				throw Win32Marshal.GetExceptionForWin32Error(num, fullPath);
			}
		}
	}

	private static bool IsNameSurrogateReparsePoint(ref Interop.Kernel32.WIN32_FIND_DATA data)
	{
		if ((data.dwFileAttributes & 0x400u) != 0)
		{
			return (data.dwReserved0 & 0x20000000) != 0;
		}
		return false;
	}

	private static void RemoveDirectoryRecursive(string fullPath, ref Interop.Kernel32.WIN32_FIND_DATA findData, bool topLevel)
	{
		Exception ex = null;
		using (SafeFindHandle safeFindHandle = Interop.Kernel32.FindFirstFile(Path.Join(fullPath, "*"), ref findData))
		{
			if (safeFindHandle.IsInvalid)
			{
				throw Win32Marshal.GetExceptionForLastWin32Error(fullPath);
			}
			int lastWin32Error;
			do
			{
				if ((findData.dwFileAttributes & 0x10) == 0)
				{
					string stringFromFixedBuffer = findData.cFileName.GetStringFromFixedBuffer();
					if (!Interop.Kernel32.DeleteFile(Path.Combine(fullPath, stringFromFixedBuffer)) && ex == null)
					{
						lastWin32Error = Marshal.GetLastWin32Error();
						if (lastWin32Error != 2)
						{
							ex = Win32Marshal.GetExceptionForWin32Error(lastWin32Error, stringFromFixedBuffer);
						}
					}
				}
				else
				{
					if (findData.cFileName.FixedBufferEqualsString(".") || findData.cFileName.FixedBufferEqualsString(".."))
					{
						continue;
					}
					string stringFromFixedBuffer2 = findData.cFileName.GetStringFromFixedBuffer();
					if (!IsNameSurrogateReparsePoint(ref findData))
					{
						try
						{
							RemoveDirectoryRecursive(Path.Combine(fullPath, stringFromFixedBuffer2), ref findData, topLevel: false);
						}
						catch (Exception ex2)
						{
							if (ex == null)
							{
								ex = ex2;
							}
						}
						continue;
					}
					if (findData.dwReserved0 == 2684354563u)
					{
						string mountPoint = Path.Join(fullPath, stringFromFixedBuffer2, "\\");
						if (!Interop.Kernel32.DeleteVolumeMountPoint(mountPoint) && ex == null)
						{
							lastWin32Error = Marshal.GetLastWin32Error();
							if (lastWin32Error != 0 && lastWin32Error != 3)
							{
								ex = Win32Marshal.GetExceptionForWin32Error(lastWin32Error, stringFromFixedBuffer2);
							}
						}
					}
					if (!Interop.Kernel32.RemoveDirectory(Path.Combine(fullPath, stringFromFixedBuffer2)) && ex == null)
					{
						lastWin32Error = Marshal.GetLastWin32Error();
						if (lastWin32Error != 3)
						{
							ex = Win32Marshal.GetExceptionForWin32Error(lastWin32Error, stringFromFixedBuffer2);
						}
					}
				}
			}
			while (Interop.Kernel32.FindNextFile(safeFindHandle, ref findData));
			if (ex != null)
			{
				throw ex;
			}
			lastWin32Error = Marshal.GetLastWin32Error();
			if (lastWin32Error != 0 && lastWin32Error != 18)
			{
				throw Win32Marshal.GetExceptionForWin32Error(lastWin32Error, fullPath);
			}
		}
		RemoveDirectoryInternal(fullPath, topLevel, allowDirectoryNotEmpty: true);
	}

	private static void RemoveDirectoryInternal(string fullPath, bool topLevel, bool allowDirectoryNotEmpty = false)
	{
		if (Interop.Kernel32.RemoveDirectory(fullPath))
		{
			return;
		}
		int num = Marshal.GetLastWin32Error();
		switch (num)
		{
		case 2:
			num = 3;
			goto case 3;
		case 3:
			if (!topLevel)
			{
				return;
			}
			break;
		case 145:
			if (allowDirectoryNotEmpty)
			{
				return;
			}
			break;
		case 5:
			throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, fullPath));
		}
		throw Win32Marshal.GetExceptionForWin32Error(num, fullPath);
	}

	public static void SetAttributes(string fullPath, FileAttributes attributes)
	{
		if (!Interop.Kernel32.SetFileAttributes(fullPath, (int)attributes))
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (lastWin32Error == 87)
			{
				throw new ArgumentException(SR.Arg_InvalidFileAttrs, "attributes");
			}
			throw Win32Marshal.GetExceptionForWin32Error(lastWin32Error, fullPath);
		}
	}

	private unsafe static void SetFileTime(string fullPath, bool asDirectory, long creationTime = -1L, long lastAccessTime = -1L, long lastWriteTime = -1L, long changeTime = -1L, uint fileAttributes = 0u)
	{
		using SafeFileHandle hFile = OpenHandle(fullPath, asDirectory);
		Interop.Kernel32.FILE_BASIC_INFO fILE_BASIC_INFO = default(Interop.Kernel32.FILE_BASIC_INFO);
		fILE_BASIC_INFO.CreationTime = creationTime;
		fILE_BASIC_INFO.LastAccessTime = lastAccessTime;
		fILE_BASIC_INFO.LastWriteTime = lastWriteTime;
		fILE_BASIC_INFO.ChangeTime = changeTime;
		fILE_BASIC_INFO.FileAttributes = fileAttributes;
		Interop.Kernel32.FILE_BASIC_INFO fILE_BASIC_INFO2 = fILE_BASIC_INFO;
		if (!Interop.Kernel32.SetFileInformationByHandle(hFile, 0, &fILE_BASIC_INFO2, (uint)sizeof(Interop.Kernel32.FILE_BASIC_INFO)))
		{
			throw Win32Marshal.GetExceptionForLastWin32Error(fullPath);
		}
	}

	public static void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory)
	{
		SetFileTime(fullPath, asDirectory, time.ToFileTime(), -1L, -1L, -1L);
	}

	public static void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory)
	{
		SetFileTime(fullPath, asDirectory, -1L, time.ToFileTime(), -1L, -1L);
	}

	public static void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory)
	{
		SetFileTime(fullPath, asDirectory, -1L, -1L, time.ToFileTime(), -1L);
	}

	public static string[] GetLogicalDrives()
	{
		return DriveInfoInternal.GetLogicalDrives();
	}

	internal static void CreateSymbolicLink(string path, string pathToTarget, bool isDirectory)
	{
		string linkTargetFullPath = PathInternal.GetLinkTargetFullPath(path, pathToTarget);
		Interop.Kernel32.CreateSymbolicLink(path, pathToTarget, isDirectory);
	}

	internal static FileSystemInfo ResolveLinkTarget(string linkPath, bool returnFinalTarget, bool isDirectory)
	{
		string text = (returnFinalTarget ? GetFinalLinkTarget(linkPath, isDirectory) : GetImmediateLinkTarget(linkPath, isDirectory, throwOnError: true, returnFullPath: true));
		if (text != null)
		{
			if (!isDirectory)
			{
				return new FileInfo(text);
			}
			return new DirectoryInfo(text);
		}
		return null;
	}

	internal static string GetLinkTarget(string linkPath, bool isDirectory)
	{
		return GetImmediateLinkTarget(linkPath, isDirectory, throwOnError: false, returnFullPath: false);
	}

	internal unsafe static string GetImmediateLinkTarget(string linkPath, bool isDirectory, bool throwOnError, bool returnFullPath)
	{
		using (SafeFileHandle safeFileHandle = OpenSafeFileHandle(linkPath, 35651584))
		{
			if (safeFileHandle.IsInvalid)
			{
				if (!throwOnError)
				{
					return null;
				}
				int num = Marshal.GetLastWin32Error();
				if (isDirectory && num == 2)
				{
					num = 3;
				}
				throw Win32Marshal.GetExceptionForWin32Error(num, linkPath);
			}
			byte[] array = ArrayPool<byte>.Shared.Rent(16384);
			try
			{
				if (!Interop.Kernel32.DeviceIoControl(safeFileHandle, 589992u, IntPtr.Zero, 0u, array, 16384u, out var _, IntPtr.Zero))
				{
					if (!throwOnError)
					{
						return null;
					}
					int lastWin32Error = Marshal.GetLastWin32Error();
					if (lastWin32Error == 4390)
					{
						return null;
					}
					throw Win32Marshal.GetExceptionForWin32Error(lastWin32Error, linkPath);
				}
				Span<byte> span = new Span<byte>(array);
				bool flag = MemoryMarshal.TryRead<Interop.Kernel32.SymbolicLinkReparseBuffer>(span, out var value);
				if (value.ReparseTag == 2684354572u)
				{
					int start = sizeof(Interop.Kernel32.SymbolicLinkReparseBuffer) + value.SubstituteNameOffset;
					int substituteNameLength = value.SubstituteNameLength;
					Span<char> span2 = MemoryMarshal.Cast<byte, char>(span.Slice(start, substituteNameLength));
					if ((value.Flags & 1) == 0)
					{
						if (span2.StartsWith("\\??\\UNC\\".AsSpan()))
						{
							return Path.Join("\\\\".AsSpan(), span2.Slice("\\??\\UNC\\".Length));
						}
						return GetTargetPathWithoutNTPrefix(span2);
					}
					if (returnFullPath)
					{
						return Path.Join(Path.GetDirectoryName(linkPath.AsSpan()), span2);
					}
					return span2.ToString();
				}
				if (value.ReparseTag == 2684354563u)
				{
					flag = MemoryMarshal.TryRead<Interop.Kernel32.MountPointReparseBuffer>(span, out var value2);
					int start2 = sizeof(Interop.Kernel32.MountPointReparseBuffer) + value2.SubstituteNameOffset;
					int substituteNameLength2 = value2.SubstituteNameLength;
					Span<char> span3 = MemoryMarshal.Cast<byte, char>(span.Slice(start2, substituteNameLength2));
					return GetTargetPathWithoutNTPrefix(span3);
				}
				return null;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
		static string GetTargetPathWithoutNTPrefix(ReadOnlySpan<char> targetPath)
		{
			return targetPath.Slice("\\??\\".Length).ToString();
		}
	}

	private static string GetFinalLinkTarget(string linkPath, bool isDirectory)
	{
		Interop.Kernel32.WIN32_FIND_DATA findData = default(Interop.Kernel32.WIN32_FIND_DATA);
		GetFindData(linkPath, isDirectory, ignoreAccessDenied: false, ref findData);
		if ((findData.dwFileAttributes & 0x400) == 0 || (findData.dwReserved0 != 2684354572u && findData.dwReserved0 != 2684354563u))
		{
			return null;
		}
		using (SafeFileHandle safeFileHandle = OpenSafeFileHandle(linkPath, 33554435))
		{
			if (safeFileHandle.IsInvalid)
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (IsPathUnreachableError(lastWin32Error))
				{
					return GetFinalLinkTargetSlow(linkPath);
				}
				throw Win32Marshal.GetExceptionForWin32Error(lastWin32Error, linkPath);
			}
			char[] array = ArrayPool<char>.Shared.Rent(4096);
			try
			{
				uint num = GetFinalPathNameByHandle(safeFileHandle, array);
				if (num > array.Length)
				{
					char[] array2 = array;
					array = ArrayPool<char>.Shared.Rent((int)num);
					ArrayPool<char>.Shared.Return(array2);
					num = GetFinalPathNameByHandle(safeFileHandle, array);
				}
				if (num == 0)
				{
					throw Win32Marshal.GetExceptionForLastWin32Error(linkPath);
				}
				int num2 = ((!PathInternal.IsExtended(linkPath.AsSpan())) ? 4 : 0);
				return new string(array, num2, (int)num - num2);
			}
			finally
			{
				ArrayPool<char>.Shared.Return(array);
			}
		}
		string GetFinalLinkTargetSlow(string linkPath)
		{
			string immediateLinkTarget = GetImmediateLinkTarget(linkPath, isDirectory, throwOnError: false, returnFullPath: true);
			string result = null;
			while (immediateLinkTarget != null)
			{
				result = immediateLinkTarget;
				immediateLinkTarget = GetImmediateLinkTarget(immediateLinkTarget, isDirectory, throwOnError: false, returnFullPath: true);
			}
			return result;
		}
		unsafe static uint GetFinalPathNameByHandle(SafeFileHandle handle, char[] buffer)
		{
			fixed (char* lpszFilePath = buffer)
			{
				return Interop.Kernel32.GetFinalPathNameByHandle(handle, lpszFilePath, (uint)buffer.Length, 0u);
			}
		}
	}

	private unsafe static SafeFileHandle OpenSafeFileHandle(string path, int flags)
	{
		return Interop.Kernel32.CreateFile(path, 0, FileShare.ReadWrite | FileShare.Delete, (Interop.Kernel32.SECURITY_ATTRIBUTES*)(void*)IntPtr.Zero, FileMode.Open, flags, IntPtr.Zero);
	}
}
