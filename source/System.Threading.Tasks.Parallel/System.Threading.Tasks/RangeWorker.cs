using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading.Tasks;

[StructLayout(LayoutKind.Auto)]
internal struct RangeWorker
{
	internal readonly IndexRange[] _indexRanges;

	internal int _nCurrentIndexRange;

	internal long _nStep;

	internal long _nIncrementValue;

	internal readonly long _nMaxIncrementValue;

	internal readonly bool _use32BitCurrentIndex;

	internal bool IsInitialized => _indexRanges != null;

	internal RangeWorker(IndexRange[] ranges, int nInitialRange, long nStep, bool use32BitCurrentIndex)
	{
		_indexRanges = ranges;
		_use32BitCurrentIndex = use32BitCurrentIndex;
		_nCurrentIndexRange = nInitialRange;
		_nStep = nStep;
		_nIncrementValue = nStep;
		_nMaxIncrementValue = 16 * nStep;
	}

	internal unsafe bool FindNewWork(out long nFromInclusiveLocal, out long nToExclusiveLocal)
	{
		int num = _indexRanges.Length;
		do
		{
			IndexRange indexRange = _indexRanges[_nCurrentIndexRange];
			if (indexRange._bRangeFinished == 0)
			{
				StrongBox<long> nSharedCurrentIndexOffset = _indexRanges[_nCurrentIndexRange]._nSharedCurrentIndexOffset;
				if (nSharedCurrentIndexOffset == null)
				{
					Interlocked.CompareExchange(ref _indexRanges[_nCurrentIndexRange]._nSharedCurrentIndexOffset, new StrongBox<long>(0L), null);
					nSharedCurrentIndexOffset = _indexRanges[_nCurrentIndexRange]._nSharedCurrentIndexOffset;
				}
				long num2;
				if (IntPtr.Size == 4 && _use32BitCurrentIndex)
				{
					fixed (long* ptr = &nSharedCurrentIndexOffset.Value)
					{
						num2 = Interlocked.Add(ref *(int*)ptr, (int)_nIncrementValue) - _nIncrementValue;
					}
				}
				else
				{
					num2 = Interlocked.Add(ref nSharedCurrentIndexOffset.Value, _nIncrementValue) - _nIncrementValue;
				}
				if (indexRange._nToExclusive - indexRange._nFromInclusive > num2)
				{
					nFromInclusiveLocal = indexRange._nFromInclusive + num2;
					nToExclusiveLocal = nFromInclusiveLocal + _nIncrementValue;
					if (nToExclusiveLocal > indexRange._nToExclusive || nToExclusiveLocal < indexRange._nFromInclusive)
					{
						nToExclusiveLocal = indexRange._nToExclusive;
					}
					if (_nIncrementValue < _nMaxIncrementValue)
					{
						_nIncrementValue *= 2L;
						if (_nIncrementValue > _nMaxIncrementValue)
						{
							_nIncrementValue = _nMaxIncrementValue;
						}
					}
					return true;
				}
				Interlocked.Exchange(ref _indexRanges[_nCurrentIndexRange]._bRangeFinished, 1);
			}
			_nCurrentIndexRange = (_nCurrentIndexRange + 1) % _indexRanges.Length;
			num--;
		}
		while (num > 0);
		nFromInclusiveLocal = 0L;
		nToExclusiveLocal = 0L;
		return false;
	}

	internal bool FindNewWork32(out int nFromInclusiveLocal32, out int nToExclusiveLocal32)
	{
		long nFromInclusiveLocal33;
		long nToExclusiveLocal33;
		bool result = FindNewWork(out nFromInclusiveLocal33, out nToExclusiveLocal33);
		nFromInclusiveLocal32 = (int)nFromInclusiveLocal33;
		nToExclusiveLocal32 = (int)nToExclusiveLocal33;
		return result;
	}
}
