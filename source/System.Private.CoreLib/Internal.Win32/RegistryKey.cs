using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using Internal.Win32.SafeHandles;

namespace Internal.Win32;

internal sealed class RegistryKey : IDisposable
{
	private readonly SafeRegistryHandle _hkey;

	private RegistryKey(SafeRegistryHandle hkey)
	{
		_hkey = hkey;
	}

	void IDisposable.Dispose()
	{
		if (_hkey != null)
		{
			_hkey.Dispose();
		}
	}

	public void DeleteValue(string name, bool throwOnMissingValue)
	{
		int num = Interop.Advapi32.RegDeleteValue(_hkey, name);
		if (num == 2 || num == 206)
		{
			if (throwOnMissingValue)
			{
				throw new ArgumentException(SR.Arg_RegSubKeyValueAbsent);
			}
			num = 0;
		}
	}

	internal static RegistryKey OpenBaseKey(IntPtr hKey)
	{
		return new RegistryKey(new SafeRegistryHandle(hKey, ownsHandle: false));
	}

	public RegistryKey OpenSubKey(string name)
	{
		return OpenSubKey(name, writable: false);
	}

	public RegistryKey OpenSubKey(string name, bool writable)
	{
		SafeRegistryHandle hkResult;
		int num = Interop.Advapi32.RegOpenKeyEx(_hkey, name, 0, writable ? 131103 : 131097, out hkResult);
		if (num == 0 && !hkResult.IsInvalid)
		{
			return new RegistryKey(hkResult);
		}
		if (num == 5 || num == 1346)
		{
			throw new SecurityException(SR.Security_RegistryPermission);
		}
		return null;
	}

	public string[] GetSubKeyNames()
	{
		List<string> list = new List<string>();
		char[] array = ArrayPool<char>.Shared.Rent(256);
		try
		{
			int lpcbName = array.Length;
			int num;
			while ((num = Interop.Advapi32.RegEnumKeyEx(_hkey, list.Count, array, ref lpcbName, null, null, null, null)) != 259)
			{
				if (num == 0)
				{
					list.Add(new string(array, 0, lpcbName));
					lpcbName = array.Length;
				}
				else
				{
					Win32Error(num, null);
				}
			}
		}
		finally
		{
			ArrayPool<char>.Shared.Return(array);
		}
		return list.ToArray();
	}

	public string[] GetValueNames()
	{
		List<string> list = new List<string>();
		char[] array = ArrayPool<char>.Shared.Rent(100);
		try
		{
			int lpcbValueName = array.Length;
			int num;
			while ((num = Interop.Advapi32.RegEnumValue(_hkey, list.Count, array, ref lpcbValueName, IntPtr.Zero, null, null, null)) != 259)
			{
				switch (num)
				{
				case 0:
					list.Add(new string(array, 0, lpcbValueName));
					break;
				case 234:
				{
					char[] array2 = array;
					int num2 = array2.Length;
					array = null;
					ArrayPool<char>.Shared.Return(array2);
					array = ArrayPool<char>.Shared.Rent(checked(num2 * 2));
					break;
				}
				default:
					Win32Error(num, null);
					break;
				}
				lpcbValueName = array.Length;
			}
		}
		finally
		{
			if (array != null)
			{
				ArrayPool<char>.Shared.Return(array);
			}
		}
		return list.ToArray();
	}

	public object GetValue(string name)
	{
		return GetValue(name, null);
	}

	[return: NotNullIfNotNull("defaultValue")]
	public object GetValue(string name, object defaultValue)
	{
		object result = defaultValue;
		int lpType = 0;
		int lpcbData = 0;
		int num = Interop.Advapi32.RegQueryValueEx(_hkey, name, (int[])null, ref lpType, (byte[])null, ref lpcbData);
		if (num != 0 && num != 234)
		{
			return result;
		}
		if (lpcbData < 0)
		{
			lpcbData = 0;
		}
		switch (lpType)
		{
		case 0:
		case 3:
		case 5:
		{
			byte[] array3 = new byte[lpcbData];
			num = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref lpType, array3, ref lpcbData);
			result = array3;
			break;
		}
		case 11:
			if (lpcbData <= 8)
			{
				long lpData = 0L;
				num = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref lpType, ref lpData, ref lpcbData);
				result = lpData;
				break;
			}
			goto case 0;
		case 4:
			if (lpcbData <= 4)
			{
				int lpData2 = 0;
				num = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref lpType, ref lpData2, ref lpcbData);
				result = lpData2;
				break;
			}
			goto case 11;
		case 1:
		{
			if (lpcbData % 2 == 1)
			{
				try
				{
					lpcbData = checked(lpcbData + 1);
				}
				catch (OverflowException innerException2)
				{
					throw new IOException(SR.Arg_RegGetOverflowBug, innerException2);
				}
			}
			char[] array4 = new char[lpcbData / 2];
			num = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref lpType, array4, ref lpcbData);
			result = ((array4.Length == 0 || array4[^1] != 0) ? new string(array4) : new string(array4, 0, array4.Length - 1));
			break;
		}
		case 2:
		{
			if (lpcbData % 2 == 1)
			{
				try
				{
					lpcbData = checked(lpcbData + 1);
				}
				catch (OverflowException innerException3)
				{
					throw new IOException(SR.Arg_RegGetOverflowBug, innerException3);
				}
			}
			char[] array5 = new char[lpcbData / 2];
			num = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref lpType, array5, ref lpcbData);
			result = ((array5.Length == 0 || array5[^1] != 0) ? new string(array5) : new string(array5, 0, array5.Length - 1));
			result = Environment.ExpandEnvironmentVariables((string)result);
			break;
		}
		case 7:
		{
			if (lpcbData % 2 == 1)
			{
				try
				{
					lpcbData = checked(lpcbData + 1);
				}
				catch (OverflowException innerException)
				{
					throw new IOException(SR.Arg_RegGetOverflowBug, innerException);
				}
			}
			char[] array = new char[lpcbData / 2];
			num = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref lpType, array, ref lpcbData);
			if (array.Length != 0 && array[^1] != 0)
			{
				Array.Resize(ref array, array.Length + 1);
			}
			string[] array2 = Array.Empty<string>();
			int num2 = 0;
			int num3 = 0;
			int num4 = array.Length;
			while (num == 0 && num3 < num4)
			{
				int i;
				for (i = num3; i < num4 && array[i] != 0; i++)
				{
				}
				string text = null;
				if (i < num4)
				{
					if (i - num3 > 0)
					{
						text = new string(array, num3, i - num3);
					}
					else if (i != num4 - 1)
					{
						text = string.Empty;
					}
				}
				else
				{
					text = new string(array, num3, num4 - num3);
				}
				num3 = i + 1;
				if (text != null)
				{
					if (array2.Length == num2)
					{
						Array.Resize(ref array2, (num2 > 0) ? (num2 * 2) : 4);
					}
					array2[num2++] = text;
				}
			}
			Array.Resize(ref array2, num2);
			result = array2;
			break;
		}
		}
		return result;
	}

	internal void SetValue(string name, string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (name != null && name.Length > 16383)
		{
			throw new ArgumentException(SR.Arg_RegValStrLenBug, "name");
		}
		int num = Interop.Advapi32.RegSetValueEx(_hkey, name, 0, 1, value, checked(value.Length * 2 + 2));
		if (num != 0)
		{
			Win32Error(num, null);
		}
	}

	internal static void Win32Error(int errorCode, string str)
	{
		switch (errorCode)
		{
		case 5:
			if (str != null)
			{
				throw new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_RegistryKeyGeneric_Key, str));
			}
			throw new UnauthorizedAccessException();
		case 2:
			throw new IOException(SR.Arg_RegKeyNotFound, errorCode);
		default:
			throw new IOException(Interop.Kernel32.GetMessage(errorCode), errorCode);
		}
	}
}
