namespace System.Threading.Tasks;

internal sealed class ParallelLoopStateFlags64 : ParallelLoopStateFlags
{
	internal long _lowestBreakIteration = long.MaxValue;

	internal long LowestBreakIteration
	{
		get
		{
			if (IntPtr.Size >= 8)
			{
				return _lowestBreakIteration;
			}
			return Interlocked.Read(ref _lowestBreakIteration);
		}
	}

	internal long? NullableLowestBreakIteration
	{
		get
		{
			if (_lowestBreakIteration == long.MaxValue)
			{
				return null;
			}
			if (IntPtr.Size >= 8)
			{
				return _lowestBreakIteration;
			}
			return Interlocked.Read(ref _lowestBreakIteration);
		}
	}

	internal bool ShouldExitLoop(long CallerIteration)
	{
		int loopStateFlags = base.LoopStateFlags;
		if (loopStateFlags != 0)
		{
			if ((loopStateFlags & 0xD) == 0)
			{
				if (((uint)loopStateFlags & 2u) != 0)
				{
					return CallerIteration > LowestBreakIteration;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	internal bool ShouldExitLoop()
	{
		int loopStateFlags = base.LoopStateFlags;
		if (loopStateFlags != 0)
		{
			return (loopStateFlags & 9) != 0;
		}
		return false;
	}
}
