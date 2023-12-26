using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Razer;

internal class RazerHeadset : RgbDevice
{
	private NativeMethods.CustomHeadsetEffect _effect = NativeMethods.CustomHeadsetEffect.Create();

	private readonly EffectHandle _handle = new EffectHandle();

	public RazerHeadset(DeviceColorProfile colorProfile)
		: base(RgbDeviceVendor.Razer, RgbDeviceType.Headset, Fragment.FromGrid(new Rectangle(-5, 0, 5, 1)), colorProfile)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		base.PreferredLevelOfDetail = EffectDetailLevel.Low;
	}

	public override void Present()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < _effect.Color.Length; i++)
		{
			_effect.Color[i] = RazerHelper.Vector4ToDeviceColor(GetProcessedLedColor(i));
		}
		_handle.SetAsHeadsetEffect(ref _effect);
		_handle.Apply();
	}
}
