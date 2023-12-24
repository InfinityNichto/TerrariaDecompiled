using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Corsair;

internal class CorsairMouse : CorsairGenericDevice
{
	private CorsairMouse(Fragment fragment, CorsairLedPosition[] leds, DeviceColorProfile colorProfile)
		: base(RgbDeviceType.Mouse, fragment, leds, colorProfile)
	{
		base.PreferredLevelOfDetail = EffectDetailLevel.Low;
	}

	public static CorsairMouse Create(int deviceIndex, CorsairDeviceInfo deviceInfo, DeviceColorProfile colorProfile)
	{
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		CorsairLedPosition[] array = deviceInfo.PhysicalLayout switch
		{
			CorsairPhysicalLayout.CPL_Zones1 => new CorsairLedPosition[1]
			{
				new CorsairLedPosition
				{
					LedId = CorsairLedId.CLM_1
				}
			}, 
			CorsairPhysicalLayout.CPL_Zones2 => new CorsairLedPosition[2]
			{
				new CorsairLedPosition
				{
					LedId = CorsairLedId.CLM_1
				},
				new CorsairLedPosition
				{
					LedId = CorsairLedId.CLM_2
				}
			}, 
			CorsairPhysicalLayout.CPL_Zones3 => new CorsairLedPosition[3]
			{
				new CorsairLedPosition
				{
					LedId = CorsairLedId.CLM_1
				},
				new CorsairLedPosition
				{
					LedId = CorsairLedId.CLM_2
				},
				new CorsairLedPosition
				{
					LedId = CorsairLedId.CLM_3
				}
			}, 
			CorsairPhysicalLayout.CPL_Zones4 => new CorsairLedPosition[4]
			{
				new CorsairLedPosition
				{
					LedId = CorsairLedId.CLM_1
				},
				new CorsairLedPosition
				{
					LedId = CorsairLedId.CLM_2
				},
				new CorsairLedPosition
				{
					LedId = CorsairLedId.CLM_3
				},
				new CorsairLedPosition
				{
					LedId = CorsairLedId.CLM_4
				}
			}, 
			_ => new CorsairLedPosition[0], 
		};
		return new CorsairMouse(Fragment.FromGrid(new Rectangle(27, 0, 1, array.Length)), array, colorProfile);
	}
}
