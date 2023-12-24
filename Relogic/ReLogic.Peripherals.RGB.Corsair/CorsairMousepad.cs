using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Corsair;

internal class CorsairMousepad : CorsairGenericDevice
{
	private CorsairMousepad(Fragment fragment, CorsairLedPosition[] leds, DeviceColorProfile colorProfile)
		: base(RgbDeviceType.Mousepad, fragment, leds, colorProfile)
	{
	}

	public static CorsairMousepad Create(int deviceIndex, DeviceColorProfile colorProfile)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		CorsairLedPosition[] ledPositionsForMouseMatOrKeyboard = CorsairHelper.GetLedPositionsForMouseMatOrKeyboard(deviceIndex);
		return new CorsairMousepad(CorsairHelper.CreateFragment(ledPositionsForMouseMatOrKeyboard, new Vector2(1040f, 0f)), ledPositionsForMouseMatOrKeyboard, colorProfile);
	}
}
