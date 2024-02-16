namespace System.Net.Http.HPack;

internal static class IntegerEncoder
{
	public static bool Encode(int value, int numBits, Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length == 0)
		{
			bytesWritten = 0;
			return false;
		}
		destination[0] &= MaskHigh(8 - numBits);
		if (value < (1 << numBits) - 1)
		{
			destination[0] |= (byte)value;
			bytesWritten = 1;
			return true;
		}
		destination[0] |= (byte)((1 << numBits) - 1);
		if (1 == destination.Length)
		{
			bytesWritten = 0;
			return false;
		}
		value -= (1 << numBits) - 1;
		int num = 1;
		while (value >= 128)
		{
			destination[num++] = (byte)(value % 128 + 128);
			if (num >= destination.Length)
			{
				bytesWritten = 0;
				return false;
			}
			value /= 128;
		}
		destination[num++] = (byte)value;
		bytesWritten = num;
		return true;
	}

	private static byte MaskHigh(int n)
	{
		return (byte)(-128 >> n - 1);
	}
}
