using System.Collections.Generic;

namespace ReLogic.Peripherals.RGB;

public abstract class RgbKeyboard : RgbDevice
{
	protected RgbKeyboard(RgbDeviceVendor vendor, Fragment fragment, DeviceColorProfile colorProfile)
		: base(vendor, RgbDeviceType.Keyboard, fragment, colorProfile)
	{
	}

	public abstract void Render(IEnumerable<RgbKey> keys);
}
