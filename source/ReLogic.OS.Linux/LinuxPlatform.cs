using System;
using ReLogic.Localization.IME;

namespace ReLogic.OS.Linux;

internal class LinuxPlatform : Platform
{
	public LinuxPlatform()
		: base(PlatformType.Linux)
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
