namespace System.Threading.Tasks;

internal sealed class ParallelLoopStateFlags32 : ParallelLoopStateFlags
{
	internal volatile int _lowestBreakIteration = int.MaxValue;

	internal int LowestBreakIteration => _lowestBreakIteration;

	internal long? NullableLowestBreakIteration
	{
		get
		{
			if (_lowestBreakIteration == int.MaxValue)
			{
				return null;
			}
			long location = _lowestBreakIteration;
			if (IntPtr.Size >= 8)
			{
				return location;
			}
			return Interlocked.Read(ref location);
		}
	}

	internal bool ShouldExitLoop(int CallerIteration)
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
