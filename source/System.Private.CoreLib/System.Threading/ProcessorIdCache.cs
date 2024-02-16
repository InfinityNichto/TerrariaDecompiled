using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading;

internal static class ProcessorIdCache
{
	[ThreadStatic]
	private static int t_currentProcessorIdCache;

	private static int s_processorIdRefreshRate;

	private static int RefreshCurrentProcessorId()
	{
		int num = Thread.GetCurrentProcessorNumber();
		if (num < 0)
		{
			num = Environment.CurrentManagedThreadId;
		}
		t_currentProcessorIdCache = ((num << 16) & 0x7FFFFFFF) | s_processorIdRefreshRate;
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int GetCurrentProcessorId()
	{
		int num = t_currentProcessorIdCache--;
		if ((num & 0xFFFF) == 0)
		{
			return RefreshCurrentProcessorId();
		}
		return num >> 16;
	}

	internal static bool ProcessorNumberSpeedCheck()
	{
		double num = double.MaxValue;
		double num2 = double.MaxValue;
		UninlinedThreadStatic();
		if (Thread.GetCurrentProcessorNumber() < 0)
		{
			s_processorIdRefreshRate = 65535;
			return false;
		}
		long num3 = Stopwatch.Frequency / 1000000 + 1;
		for (int i = 0; i < 10; i++)
		{
			int num4 = 8;
			long timestamp;
			do
			{
				num4 *= 2;
				timestamp = Stopwatch.GetTimestamp();
				for (int j = 0; j < num4; j++)
				{
					Thread.GetCurrentProcessorNumber();
				}
				timestamp = Stopwatch.GetTimestamp() - timestamp;
			}
			while (timestamp < num3);
			num = Math.Min(num, (double)timestamp / (double)num4);
			num4 /= 4;
			do
			{
				num4 *= 2;
				timestamp = Stopwatch.GetTimestamp();
				for (int k = 0; k < num4; k++)
				{
					UninlinedThreadStatic();
				}
				timestamp = Stopwatch.GetTimestamp() - timestamp;
			}
			while (timestamp < num3);
			num2 = Math.Min(num2, (double)timestamp / (double)num4);
		}
		s_processorIdRefreshRate = Math.Min((int)(num * 5.0 / num2), 5000);
		return s_processorIdRefreshRate <= 5;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int UninlinedThreadStatic()
	{
		return t_currentProcessorIdCache;
	}
}
