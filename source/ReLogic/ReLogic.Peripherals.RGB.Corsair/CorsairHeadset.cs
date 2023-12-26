using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Corsair;

internal class CorsairHeadset : CorsairGenericDevice
{
	private CorsairHeadset(Fragment fragment, CorsairLedPosition[] ledPositions, DeviceColorProfile colorProfile)
		: base(RgbDeviceType.Headset, fragment, ledPositions, colorProfile)
	{
		base.PreferredLevelOfDetail = EffectDetailLevel.Low;
	}

	public static CorsairHeadset Create(int deviceIndex, DeviceColorProfile colorProfile)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		CorsairLedPosition[] ledPositions = new CorsairLedPosition[2]
		{
			new CorsairLedPosition
			{
				Width = 1.0,
				Height = 1.0,
				LedId = CorsairLedId.CLH_LeftLogo
			},
			new CorsairLedPosition
			{
				Width = 1.0,
				Height = 1.0,
				LedId = CorsairLedId.CLH_RightLogo
			}
		};
		return new CorsairHeadset(Fragment.FromGrid(new Rectangle(-2, 0, 2, 1)), ledPositions, colorProfile);
	}
}
