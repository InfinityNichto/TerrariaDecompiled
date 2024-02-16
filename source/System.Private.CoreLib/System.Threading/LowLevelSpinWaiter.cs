namespace System.Threading;

internal struct LowLevelSpinWaiter
{
	private int _spinningThreadCount;

	public bool SpinWaitForCondition(Func<bool> condition, int spinCount, int sleep0Threshold)
	{
		int processorCount = Environment.ProcessorCount;
		int num = Interlocked.Increment(ref _spinningThreadCount);
		try
		{
			if (num <= processorCount)
			{
				for (int i = ((processorCount <= 1) ? sleep0Threshold : 0); i < spinCount; i++)
				{
					Wait(i, sleep0Threshold, processorCount);
					if (condition())
					{
						return true;
					}
				}
			}
		}
		finally
		{
			Interlocked.Decrement(ref _spinningThreadCount);
		}
		return false;
	}

	public static void Wait(int spinIndex, int sleep0Threshold, int processorCount)
	{
		if (processorCount > 1 && (spinIndex < sleep0Threshold || (spinIndex - sleep0Threshold) % 2 != 0))
		{
			int num = Thread.OptimalMaxSpinWaitsPerSpinIteration;
			if (spinIndex <= 30 && 1 << spinIndex < num)
			{
				num = 1 << spinIndex;
			}
			Thread.SpinWait(num);
		}
		else
		{
			Thread.UninterruptibleSleep0();
		}
	}
}
