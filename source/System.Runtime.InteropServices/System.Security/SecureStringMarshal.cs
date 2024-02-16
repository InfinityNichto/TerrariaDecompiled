using System.Runtime.InteropServices;

namespace System.Security;

public static class SecureStringMarshal
{
	public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s)
	{
		return Marshal.SecureStringToCoTaskMemAnsi(s);
	}

	public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s)
	{
		return Marshal.SecureStringToGlobalAllocAnsi(s);
	}

	public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s)
	{
		return Marshal.SecureStringToCoTaskMemUnicode(s);
	}

	public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s)
	{
		return Marshal.SecureStringToGlobalAllocUnicode(s);
	}
}
