using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel;

internal sealed class RepeatEnumerable<TResult> : ParallelQuery<TResult>, IParallelPartitionable<TResult>
{
	private sealed class RepeatEnumerator : QueryOperatorEnumerator<TResult, int>
	{
		private readonly TResult _element;

		private readonly int _count;

		private readonly int _indexOffset;

		private Shared<int> _currentIndex;

		internal RepeatEnumerator(TResult element, int count, int indexOffset)
		{
			_element = element;
			_count = count;
			_indexOffset = indexOffset;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TResult currentElement, ref int currentKey)
		{
			if (_currentIndex == null)
			{
				_currentIndex = new Shared<int>(-1);
			}
			if (_currentIndex.Value < _count - 1)
			{
				_currentIndex.Value++;
				currentElement = _element;
				currentKey = _currentIndex.Value + _indexOffset;
				return true;
			}
			return false;
		}

		internal override void Reset()
		{
			_currentIndex = null;
		}
	}

	private readonly TResult _element;

	private readonly int _count;

	internal RepeatEnumerable(TResult element, int count)
		: base(QuerySettings.Empty)
	{
		_element = element;
		_count = count;
	}

	public QueryOperatorEnumerator<TResult, int>[] GetPartitions(int partitionCount)
	{
		int num = (_count + partitionCount - 1) / partitionCount;
		QueryOperatorEnumerator<TResult, int>[] array = new QueryOperatorEnumerator<TResult, int>[partitionCount];
		int num2 = 0;
		int num3 = 0;
		while (num2 < partitionCount)
		{
			if (num3 + num > _count)
			{
				array[num2] = new RepeatEnumerator(_element, (num3 < _count) ? (_count - num3) : 0, num3);
			}
			else
			{
				array[num2] = new RepeatEnumerator(_element, num, num3);
			}
			num2++;
			num3 += num;
		}
		return array;
	}

	public override IEnumerator<TResult> GetEnumerator()
	{
		return new RepeatEnumerator(_element, _count, 0).AsClassicEnumerator();
	}
}
