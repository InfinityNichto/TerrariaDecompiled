using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Razer;

internal class RazerLink : RgbDevice
{
	private NativeMethods.CustomChromaLinkEffect _effect = NativeMethods.CustomChromaLinkEffect.Create();

	private readonly EffectHandle _handle = new EffectHandle();

	public RazerLink(DeviceColorProfile colorProfile)
		: base(RgbDeviceVendor.Razer, RgbDeviceType.Generic, Fragment.FromGrid(new Rectangle(0, -1, 5, 1)), colorProfile)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		base.PreferredLevelOfDetail = EffectDetailLevel.Low;
	}

	public override void Present()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < _effect.Color.Length; i++)
		{
			_effect.Color[i] = RazerHelper.Vector4ToDeviceColor(GetProcessedLedColor(i));
		}
		_handle.SetAsChromaLinkEffect(ref _effect);
		_handle.Apply();
	}
}
