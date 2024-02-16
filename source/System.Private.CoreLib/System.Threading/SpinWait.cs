namespace System.Threading;

public struct SpinWait
{
	internal static readonly int SpinCountforSpinBeforeWait = (Environment.IsSingleProcessor ? 1 : 35);

	private int _count;

	public int Count
	{
		get
		{
			return _count;
		}
		internal set
		{
			_count = value;
		}
	}

	public bool NextSpinWillYield
	{
		get
		{
			if (_count < 10)
			{
				return Environment.IsSingleProcessor;
			}
			return true;
		}
	}

	public void SpinOnce()
	{
		SpinOnceCore(20);
	}

	public void SpinOnce(int sleep1Threshold)
	{
		if (sleep1Threshold < -1)
		{
			throw new ArgumentOutOfRangeException("sleep1Threshold", sleep1Threshold, SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (sleep1Threshold >= 0 && sleep1Threshold < 10)
		{
			sleep1Threshold = 10;
		}
		SpinOnceCore(sleep1Threshold);
	}

	private void SpinOnceCore(int sleep1Threshold)
	{
		if ((_count >= 10 && ((_count >= sleep1Threshold && sleep1Threshold >= 0) || (_count - 10) % 2 == 0)) || Environment.IsSingleProcessor)
		{
			if (_count >= sleep1Threshold && sleep1Threshold >= 0)
			{
				Thread.Sleep(1);
			}
			else
			{
				int num = ((_count >= 10) ? ((_count - 10) / 2) : _count);
				if (num % 5 == 4)
				{
					Thread.Sleep(0);
				}
				else
				{
					Thread.Yield();
				}
			}
		}
		else
		{
			int num2 = Thread.OptimalMaxSpinWaitsPerSpinIteration;
			if (_count <= 30 && 1 << _count < num2)
			{
				num2 = 1 << _count;
			}
			Thread.SpinWait(num2);
		}
		_count = ((_count == int.MaxValue) ? 10 : (_count + 1));
	}

	public void Reset()
	{
		_count = 0;
	}

	public static void SpinUntil(Func<bool> condition)
	{
		SpinUntil(condition, -1);
	}

	public static bool SpinUntil(Func<bool> condition, TimeSpan timeout)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1 || num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("timeout", timeout, SR.SpinWait_SpinUntil_TimeoutWrong);
		}
		return SpinUntil(condition, (int)num);
	}

	public static bool SpinUntil(Func<bool> condition, int millisecondsTimeout)
	{
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", millisecondsTimeout, SR.SpinWait_SpinUntil_TimeoutWrong);
		}
		if (condition == null)
		{
			throw new ArgumentNullException("condition", SR.SpinWait_SpinUntil_ArgumentNull);
		}
		uint num = 0u;
		if (millisecondsTimeout != 0 && millisecondsTimeout != -1)
		{
			num = TimeoutHelper.GetTime();
		}
		SpinWait spinWait = default(SpinWait);
		while (!condition())
		{
			if (millisecondsTimeout == 0)
			{
				return false;
			}
			spinWait.SpinOnce();
			if (millisecondsTimeout != -1 && spinWait.NextSpinWillYield && millisecondsTimeout <= TimeoutHelper.GetTime() - num)
			{
				return false;
			}
		}
		return true;
	}
}
