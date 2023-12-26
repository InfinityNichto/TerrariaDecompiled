using System;
using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Corsair;

internal class CorsairGenericDevice : RgbDevice
{
	private readonly CorsairLedColor[] _ledColors;

	protected CorsairGenericDevice(RgbDeviceType deviceType, Fragment fragment, CorsairLedPosition[] ledPositions, DeviceColorProfile colorProfile)
		: base(RgbDeviceVendor.Corsair, deviceType, fragment, colorProfile)
	{
		_ledColors = new CorsairLedColor[base.LedCount];
		for (int i = 0; i < ledPositions.Length; i++)
		{
			_ledColors[i].LedId = ledPositions[i].LedId;
		}
	}

	public override void Present()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < base.LedCount; i++)
		{
			Vector4 processedLedColor = GetProcessedLedColor(i);
			_ledColors[i].R = (int)(processedLedColor.X * 255f);
			_ledColors[i].G = (int)(processedLedColor.Y * 255f);
			_ledColors[i].B = (int)(processedLedColor.Z * 255f);
		}
		if (_ledColors.Length != 0)
		{
			NativeMethods.CorsairSetLedsColorsAsync(_ledColors.Length, _ledColors, IntPtr.Zero, IntPtr.Zero);
		}
	}
}
