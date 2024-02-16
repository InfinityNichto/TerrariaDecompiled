namespace System.Threading.Tasks;

internal sealed class RangeManager
{
	internal readonly IndexRange[] _indexRanges;

	internal readonly bool _use32BitCurrentIndex;

	internal int _nCurrentIndexRangeToAssign;

	internal long _nStep;

	internal RangeManager(long nFromInclusive, long nToExclusive, long nStep, int nNumExpectedWorkers)
	{
		_nCurrentIndexRangeToAssign = 0;
		_nStep = nStep;
		if (nNumExpectedWorkers == 1)
		{
			nNumExpectedWorkers = 2;
		}
		ulong num = (ulong)(nToExclusive - nFromInclusive);
		ulong num2 = num / (ulong)nNumExpectedWorkers;
		num2 -= num2 % (ulong)nStep;
		if (num2 == 0L)
		{
			num2 = (ulong)nStep;
		}
		int num3 = (int)(num / num2);
		if (num % num2 != 0L)
		{
			num3++;
		}
		long num4 = (long)num2;
		_use32BitCurrentIndex = IntPtr.Size == 4 && num4 <= int.MaxValue;
		_indexRanges = new IndexRange[num3];
		long num5 = nFromInclusive;
		for (int i = 0; i < num3; i++)
		{
			_indexRanges[i]._nFromInclusive = num5;
			_indexRanges[i]._nSharedCurrentIndexOffset = null;
			_indexRanges[i]._bRangeFinished = 0;
			num5 += num4;
			if (num5 < num5 - num4 || num5 > nToExclusive)
			{
				num5 = nToExclusive;
			}
			_indexRanges[i]._nToExclusive = num5;
		}
	}

	internal RangeWorker RegisterNewWorker()
	{
		int nInitialRange = (Interlocked.Increment(ref _nCurrentIndexRangeToAssign) - 1) % _indexRanges.Length;
		return new RangeWorker(_indexRanges, nInitialRange, _nStep, _use32BitCurrentIndex);
	}
}
