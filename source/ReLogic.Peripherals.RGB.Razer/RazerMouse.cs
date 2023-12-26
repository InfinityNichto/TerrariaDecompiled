using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Razer;

internal class RazerMouse : RgbDevice
{
	private NativeMethods.CustomMouseEffect _effect = NativeMethods.CustomMouseEffect.Create();

	private readonly EffectHandle _handle = new EffectHandle();

	public RazerMouse(DeviceColorProfile colorProfile)
		: base(RgbDeviceVendor.Razer, RgbDeviceType.Mouse, Fragment.FromGrid(new Rectangle(27, 0, 7, 9)), colorProfile)
	{
	}//IL_001f: Unknown result type (might be due to invalid IL or missing references)


	public override void Present()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < base.LedCount; i++)
		{
			_effect.Color[i] = RazerHelper.Vector4ToDeviceColor(GetProcessedLedColor(i));
		}
		_handle.SetAsMouseEffect(ref _effect);
		_handle.Apply();
	}
}
