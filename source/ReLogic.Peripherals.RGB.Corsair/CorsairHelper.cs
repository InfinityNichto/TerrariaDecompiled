using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Corsair;

internal static class CorsairHelper
{
	public static Fragment CreateFragment(CorsairLedPosition[] leds, Vector2 offset)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		Point[] array = (Point[])(object)new Point[leds.Length];
		Vector2[] array2 = (Vector2[])(object)new Vector2[leds.Length];
		double num = 0.0;
		double num2 = 0.0;
		if (leds.Length != 0)
		{
			num = leds[0].Top + (double)offset.Y;
			num2 = leds[0].Top + (double)offset.Y;
		}
		Point point = default(Point);
		for (int i = 0; i < leds.Length; i++)
		{
			leds[i].Left += offset.X;
			leds[i].Top += offset.Y;
			((Point)(ref point))._002Ector((int)Math.Floor((leds[i].Left + leds[i].Width * 0.5) / 20.0), (int)Math.Floor(leds[i].Top / 20.0));
			array[i] = point;
			num = Math.Min(leds[i].Top, num);
			num2 = Math.Max(leds[i].Top, num2);
		}
		double num3 = 1.0;
		if (num != num2)
		{
			num3 = 1.0 / (num2 - num);
		}
		for (int j = 0; j < leds.Length; j++)
		{
			array2[j] = new Vector2((float)((leds[j].Left + leds[j].Width * 0.5) * num3), (float)((leds[j].Top - num) * num3));
		}
		return Fragment.FromCustom(array, array2);
	}

	public static CorsairLedPosition[] GetLedPositionsForMouseMatOrKeyboard(int deviceIndex)
	{
		IntPtr intPtr = NativeMethods.CorsairGetLedPositionsByDeviceIndex(deviceIndex);
		if (intPtr == IntPtr.Zero)
		{
			return new CorsairLedPosition[0];
		}
		CorsairLedPositions corsairLedPositions = (CorsairLedPositions)Marshal.PtrToStructure(intPtr, typeof(CorsairLedPositions));
		int numberOfLed = corsairLedPositions.NumberOfLed;
		CorsairLedPosition[] array = new CorsairLedPosition[numberOfLed];
		int num = Marshal.SizeOf(typeof(CorsairLedPosition));
		for (int i = 0; i < numberOfLed; i++)
		{
			array[i] = (CorsairLedPosition)Marshal.PtrToStructure(corsairLedPositions.LedPositionPtr, typeof(CorsairLedPosition));
			corsairLedPositions.LedPositionPtr += num;
		}
		return array;
	}
}
