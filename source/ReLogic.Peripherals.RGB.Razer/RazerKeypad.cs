using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Razer;

internal class RazerKeypad : RgbDevice
{
	private NativeMethods.CustomKeypadEffect _effect = NativeMethods.CustomKeypadEffect.Create();

	private readonly EffectHandle _handle = new EffectHandle();

	public RazerKeypad(DeviceColorProfile colorProfile)
		: base(RgbDeviceVendor.Razer, RgbDeviceType.Keypad, Fragment.FromGrid(new Rectangle(-10, 0, 5, 4)), colorProfile)
	{
	}//IL_001e: Unknown result type (might be due to invalid IL or missing references)


	public override void Present()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < base.LedCount; i++)
		{
			_effect.Color[i] = RazerHelper.Vector4ToDeviceColor(GetProcessedLedColor(i));
		}
		_handle.SetAsKeypadEffect(ref _effect);
		_handle.Apply();
	}
}
