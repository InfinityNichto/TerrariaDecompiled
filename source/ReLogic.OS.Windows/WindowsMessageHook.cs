using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ReLogic.OS.Windows;

internal class WindowsMessageHook : IDisposable
{
	private delegate IntPtr WndProcCallback(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

	private const int GWL_WNDPROC = -4;

	private IntPtr _windowHandle = IntPtr.Zero;

	private IntPtr _previousWndProc = IntPtr.Zero;

	private WndProcCallback _wndProc;

	private List<IMessageFilter> _filters = new List<IMessageFilter>();

	private bool disposedValue;

	public WindowsMessageHook(IntPtr windowHandle)
	{
		_windowHandle = windowHandle;
		_wndProc = WndProc;
		_previousWndProc = NativeMethods.SetWindowLongPtr(_windowHandle, -4, Marshal.GetFunctionPointerForDelegate((Delegate)_wndProc));
	}

	public void AddMessageFilter(IMessageFilter filter)
	{
		_filters.Add(filter);
	}

	public void RemoveMessageFilter(IMessageFilter filter)
	{
		_filters.Remove(filter);
	}

	private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
	{
		Message message = Message.Create(hWnd, msg, wParam, lParam);
		if (InternalWndProc(ref message))
		{
			return message.Result;
		}
		return NativeMethods.CallWindowProc(_previousWndProc, message.HWnd, message.Msg, message.WParam, message.LParam);
	}

	private bool InternalWndProc(ref Message message)
	{
		foreach (IMessageFilter filter in _filters)
		{
			if (filter.PreFilterMessage(ref message))
			{
				return true;
			}
		}
		return false;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			NativeMethods.SetWindowLongPtr(_windowHandle, -4, _previousWndProc);
			disposedValue = true;
		}
	}

	~WindowsMessageHook()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
