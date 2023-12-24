using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Razer;

internal static class RazerHelper
{
	public static uint Vector4ToDeviceColor(Vector4 color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		int num4 = (int)(color.X * 255f);
		int num2 = (int)(color.Y * 255f);
		int num3 = (int)(color.Z * 255f);
		num3 <<= 16;
		num2 <<= 8;
		return (uint)(num4 | num2 | num3);
	}

	public static uint XnaColorToDeviceColor(Color color)
	{
		byte r = ((Color)(ref color)).R;
		int g = ((Color)(ref color)).G;
		int b = ((Color)(ref color)).B;
		b <<= 16;
		g <<= 8;
		return (uint)(r | g | b);
	}
}
