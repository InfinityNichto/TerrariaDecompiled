using System;
using Microsoft.Xna.Framework;
using SDL2;

namespace ReLogic.OS.Windows;

internal class WindowService : IWindowService
{
	public float GetScaling()
	{
		try
		{
			IntPtr dC = NativeMethods.GetDC(IntPtr.Zero);
			int deviceCaps = NativeMethods.GetDeviceCaps(dC, NativeMethods.DeviceCap.VertRes);
			return (float)NativeMethods.GetDeviceCaps(dC, NativeMethods.DeviceCap.DesktopVertRes) / (float)deviceCaps;
		}
		catch (Exception)
		{
			return 1f;
		}
	}

	public void SetQuickEditEnabled(bool enabled)
	{
		IntPtr stdHandle = NativeMethods.GetStdHandle(NativeMethods.StdHandleType.Input);
		if (NativeMethods.GetConsoleMode(stdHandle, out var lpMode))
		{
			lpMode = ((!enabled) ? (lpMode & ~NativeMethods.ConsoleMode.QuickEditMode) : (lpMode | NativeMethods.ConsoleMode.QuickEditMode));
			NativeMethods.SetConsoleMode(stdHandle, lpMode);
		}
	}

	public void SetUnicodeTitle(GameWindow window, string title)
	{
		SDL.SDL_SetWindowTitle(window.Handle, title);
	}

	public void StartFlashingIcon(GameWindow window)
	{
		NativeMethods.FlashInfo flashInfo = NativeMethods.FlashInfo.CreateStart(window.Handle);
		NativeMethods.FlashWindowEx(ref flashInfo);
	}

	public void StopFlashingIcon(GameWindow window)
	{
		NativeMethods.FlashInfo flashInfo = NativeMethods.FlashInfo.CreateStop(window.Handle);
		NativeMethods.FlashWindowEx(ref flashInfo);
	}

	public void HideConsole()
	{
		NativeMethods.HideConsole();
	}
}
