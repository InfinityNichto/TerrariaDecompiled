using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class RangeEnumerable : ParallelQuery<int>, IParallelPartitionable<int>
{
	private sealed class RangeEnumerator : QueryOperatorEnumerator<int, int>
	{
		private readonly int _from;

		private readonly int _count;

		private readonly int _initialIndex;

		private Shared<int> _currentCount;

		internal RangeEnumerator(int from, int count, int initialIndex)
		{
			_from = from;
			_count = count;
			_initialIndex = initialIndex;
		}

		internal override bool MoveNext(ref int currentElement, ref int currentKey)
		{
			if (_currentCount == null)
			{
				_currentCount = new Shared<int>(-1);
			}
			int num = _currentCount.Value + 1;
			if (num < _count)
			{
				_currentCount.Value = num;
				currentElement = num + _from;
				currentKey = num + _initialIndex;
				return true;
			}
			return false;
		}

		internal override void Reset()
		{
			_currentCount = null;
		}
	}

	private readonly int _from;

	private readonly int _count;

	internal RangeEnumerable(int from, int count)
		: base(QuerySettings.Empty)
	{
		_from = from;
		_count = count;
	}

	public QueryOperatorEnumerator<int, int>[] GetPartitions(int partitionCount)
	{
		int num = _count / partitionCount;
		int num2 = _count % partitionCount;
		int num3 = 0;
		QueryOperatorEnumerator<int, int>[] array = new QueryOperatorEnumerator<int, int>[partitionCount];
		for (int i = 0; i < partitionCount; i++)
		{
			int num4 = ((i < num2) ? (num + 1) : num);
			array[i] = new RangeEnumerator(_from + num3, num4, num3);
			num3 += num4;
		}
		return array;
	}

	public override IEnumerator<int> GetEnumerator()
	{
		return new RangeEnumerator(_from, _count, 0).AsClassicEnumerator();
	}
}
