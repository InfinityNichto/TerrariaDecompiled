using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Logitech;

internal class LogitechSingleLightDevice : RgbDevice
{
	public LogitechSingleLightDevice(DeviceColorProfile colorProfile)
		: base(RgbDeviceVendor.Logitech, RgbDeviceType.Generic, Fragment.FromGrid(new Rectangle(30, 0, 1, 1)), colorProfile)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		base.PreferredLevelOfDetail = EffectDetailLevel.Low;
	}

	public override void Present()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (NativeMethods.LogiLedSetTargetDevice(2))
		{
			Vector4 processedLedColor = GetProcessedLedColor(0);
			NativeMethods.LogiLedSetLighting((int)(processedLedColor.X * 100f), (int)(processedLedColor.Y * 100f), (int)(processedLedColor.Z * 100f));
		}
	}
}
