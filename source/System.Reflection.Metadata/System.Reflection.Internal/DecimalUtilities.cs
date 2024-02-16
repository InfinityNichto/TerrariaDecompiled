namespace System.Reflection.Internal;

internal static class DecimalUtilities
{
	public static int GetScale(this decimal value)
	{
		Span<int> destination = stackalloc int[4];
		decimal.GetBits(value, destination);
		return (byte)(destination[3] >> 16);
	}

	public static void GetBits(this decimal value, out bool isNegative, out byte scale, out uint low, out uint mid, out uint high)
	{
		Span<int> destination = stackalloc int[4];
		decimal.GetBits(value, destination);
		low = (uint)destination[0];
		mid = (uint)destination[1];
		high = (uint)destination[2];
		scale = (byte)(destination[3] >> 16);
		isNegative = (destination[3] & 0x80000000u) != 0;
	}
}
