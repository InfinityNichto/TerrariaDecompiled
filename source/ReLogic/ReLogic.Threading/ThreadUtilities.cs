using System.Diagnostics;

namespace ReLogic.Threading;

public static class ThreadUtilities
{
	public static void HighPrecisionSleep(double timeInMs)
	{
		double num = (double)Stopwatch.Frequency / 1000.0;
		long num2 = Stopwatch.GetTimestamp() + (long)(timeInMs * num);
		while (Stopwatch.GetTimestamp() < num2)
		{
		}
	}
}
