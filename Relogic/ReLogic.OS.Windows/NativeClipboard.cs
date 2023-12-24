using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ReLogic.OS.Windows;

internal static class NativeClipboard
{
	private const uint CF_UNICODETEXT = 13u;

	[DllImport("User32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsClipboardFormatAvailable(uint format);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool OpenClipboard(IntPtr hWndNewOwner);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool CloseClipboard();

	[DllImport("user32.dll")]
	private static extern bool SetClipboardData(uint uFormat, IntPtr data);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr GetClipboardData(uint uFormat);

	[DllImport("Kernel32.dll", SetLastError = true)]
	private static extern IntPtr GlobalLock(IntPtr hMem);

	[DllImport("Kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GlobalUnlock(IntPtr hMem);

	[DllImport("Kernel32.dll", SetLastError = true)]
	private static extern int GlobalSize(IntPtr hMem);

	public static void SetText(string text)
	{
		OpenClipboard(IntPtr.Zero);
		IntPtr ptr = Marshal.StringToHGlobalUni(text);
		SetClipboardData(13u, ptr);
		CloseClipboard();
	}

	public static bool TryGetText(out string text)
	{
		text = null;
		if (!IsClipboardFormatAvailable(13u))
		{
			return false;
		}
		try
		{
			if (!OpenClipboard(IntPtr.Zero))
			{
				return false;
			}
			IntPtr handle = GetClipboardData(13u);
			if (handle == IntPtr.Zero)
			{
				return false;
			}
			IntPtr pointer = IntPtr.Zero;
			try
			{
				pointer = GlobalLock(handle);
				if (pointer == IntPtr.Zero)
				{
					return false;
				}
				int size = GlobalSize(handle);
				byte[] buff = new byte[size];
				Marshal.Copy(pointer, buff, 0, size);
				text = Encoding.Unicode.GetString(buff).TrimEnd('\0');
				return !string.IsNullOrEmpty(text);
			}
			finally
			{
				if (pointer != IntPtr.Zero)
				{
					GlobalUnlock(handle);
				}
			}
		}
		finally
		{
			CloseClipboard();
		}
	}
}
