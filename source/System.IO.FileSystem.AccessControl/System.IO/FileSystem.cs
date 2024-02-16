using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

internal static class FileSystem
{
	public static bool DirectoryExists(string fullPath)
	{
		int lastError;
		return DirectoryExists(fullPath, out lastError);
	}

	private static bool DirectoryExists(string path, out int lastError)
	{
		global::Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(global::Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		lastError = FillAttributeInfo(path, ref data, returnErrorOnNotFound: true);
		if (lastError == 0 && data.dwFileAttributes != -1)
		{
			return (data.dwFileAttributes & 0x10) != 0;
		}
		return false;
	}

	public static bool FileExists(string fullPath)
	{
		global::Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(global::Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		if (FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: true) == 0 && data.dwFileAttributes != -1)
		{
			return (data.dwFileAttributes & 0x10) == 0;
		}
		return false;
	}

	internal static int FillAttributeInfo(string path, ref global::Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data, bool returnErrorOnNotFound)
	{
		int num = 0;
		path = System.IO.PathInternal.TrimEndingDirectorySeparator(path);
		using (System.IO.DisableMediaInsertionPrompt.Create())
		{
			if (!global::Interop.Kernel32.GetFileAttributesEx(path, global::Interop.Kernel32.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, ref data))
			{
				num = Marshal.GetLastWin32Error();
				if (!IsPathUnreachableError(num))
				{
					global::Interop.Kernel32.WIN32_FIND_DATA data2 = default(global::Interop.Kernel32.WIN32_FIND_DATA);
					using Microsoft.Win32.SafeHandles.SafeFindHandle safeFindHandle = global::Interop.Kernel32.FindFirstFile(path, ref data2);
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
		if (num >= 2 && System.IO.PathInternal.EndsInDirectorySeparator(fullPath.AsSpan()))
		{
			num--;
		}
		int rootLength = System.IO.PathInternal.GetRootLength(fullPath.AsSpan());
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
				while (num2 > rootLength && !System.IO.PathInternal.IsDirectorySeparator(fullPath[num2]))
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
			global::Interop.Kernel32.SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES = default(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
			sECURITY_ATTRIBUTES.nLength = (uint)sizeof(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
			sECURITY_ATTRIBUTES.lpSecurityDescriptor = (IntPtr)ptr;
			global::Interop.Kernel32.SECURITY_ATTRIBUTES lpSecurityAttributes = sECURITY_ATTRIBUTES;
			while (list.Count > 0)
			{
				string text2 = list[list.Count - 1];
				list.RemoveAt(list.Count - 1);
				flag2 = global::Interop.Kernel32.CreateDirectory(text2, ref lpSecurityAttributes);
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
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(3, pathRoot);
			}
		}
		else if (!flag2 && num3 != 0)
		{
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(num3, path);
		}
	}
}
