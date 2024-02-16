namespace System.Globalization;

internal static class InternalGlobalizationHelper
{
	internal static long TimeToTicks(int hour, int minute, int second)
	{
		long num = (long)hour * 3600L + (long)minute * 60L + second;
		if (num > 922337203685L || num < -922337203685L)
		{
			throw new ArgumentOutOfRangeException(null, SR.Overflow_TimeSpanTooLong);
		}
		return num * 10000000;
	}
}
