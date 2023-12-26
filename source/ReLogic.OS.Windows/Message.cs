using System;

namespace ReLogic.OS.Windows;

public struct Message
{
	public IntPtr HWnd;

	public int Msg;

	public IntPtr WParam;

	public IntPtr LParam;

	public IntPtr Result;

	public static Message Create(IntPtr hWnd, int msg, IntPtr wparam, IntPtr lparam)
	{
		Message result = default(Message);
		result.HWnd = hWnd;
		result.Msg = msg;
		result.WParam = wparam;
		result.LParam = lparam;
		result.Result = IntPtr.Zero;
		return result;
	}
}
