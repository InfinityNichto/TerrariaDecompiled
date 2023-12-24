using System;
using ReLogic.Localization.IME;
using SDL2;

namespace ReLogic.OS.Windows;

internal class WindowsPlatform : Platform
{
	private WindowsMessageHook _wndProcHook;

	private bool _disposedValue;

	public WindowsPlatform()
		: base(PlatformType.Windows)
	{
		RegisterService((IClipboard)new Clipboard());
		RegisterService((IPathService)new PathService());
		RegisterService((IWindowService)new WindowService());
		RegisterService((IImeService)new UnsupportedPlatformIme());
	}

	public override void InitializeClientServices(IntPtr windowHandle)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		SDL_SysWMinfo info = default(SDL_SysWMinfo);
		SDL.SDL_VERSION(ref info.version);
		SDL.SDL_GetWindowWMInfo(windowHandle, ref info);
		windowHandle = info.info.win.window;
		if (_wndProcHook == null)
		{
			_wndProcHook = new WindowsMessageHook(windowHandle);
		}
		RegisterService((IImeService)new WinImm32Ime(_wndProcHook, windowHandle));
	}

	protected override void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing && _wndProcHook != null)
			{
				_wndProcHook.Dispose();
				_wndProcHook = null;
			}
			_disposedValue = true;
			base.Dispose(disposing);
		}
	}
}
