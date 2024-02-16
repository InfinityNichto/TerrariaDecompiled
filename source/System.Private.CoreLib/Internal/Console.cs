using System;
using System.Text;

namespace Internal;

public static class Console
{
	private static readonly IntPtr s_outputHandle = Interop.Kernel32.GetStdHandle(-11);

	public static void WriteLine(string? s)
	{
		Write(s + "\r\n");
	}

	public static void WriteLine()
	{
		Write("\r\n");
	}

	public unsafe static void Write(string s)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		fixed (byte* bytes2 = bytes)
		{
			Interop.Kernel32.WriteFile(s_outputHandle, bytes2, bytes.Length, out var _, IntPtr.Zero);
		}
	}
}
