namespace System.Xml;

internal static class Bits
{
	public static int Count(uint num)
	{
		num = (num & 0x55555555) + ((num >> 1) & 0x55555555);
		num = (num & 0x33333333) + ((num >> 2) & 0x33333333);
		num = (num & 0xF0F0F0F) + ((num >> 4) & 0xF0F0F0F);
		num = (num & 0xFF00FF) + ((num >> 8) & 0xFF00FF);
		num = (num & 0xFFFF) + (num >> 16);
		return (int)num;
	}

	public static bool ExactlyOne(uint num)
	{
		if (num != 0)
		{
			return (num & (num - 1)) == 0;
		}
		return false;
	}

	public static uint ClearLeast(uint num)
	{
		return num & (num - 1);
	}

	public static int LeastPosition(uint num)
	{
		if (num == 0)
		{
			return 0;
		}
		return Count(num ^ (num - 1));
	}
}
