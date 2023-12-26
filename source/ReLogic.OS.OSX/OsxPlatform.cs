using System;
using ReLogic.Localization.IME;

namespace ReLogic.OS.OSX;

internal class OsxPlatform : Platform
{
	public OsxPlatform()
		: base(PlatformType.OSX)
	{
		RegisterService((IClipboard)new Clipboard());
		RegisterService((IPathService)new PathService());
		RegisterService((IWindowService)new WindowService());
		RegisterService((IImeService)new UnsupportedPlatformIme());
	}

	public override void InitializeClientServices(IntPtr windowHandle)
	{
		RegisterService((IImeService)new FnaIme());
	}
}
