using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class PartitionedDataSource<T> : PartitionedStream<T, int>
{
	internal sealed class ArrayIndexRangeEnumerator : QueryOperatorEnumerator<T, int>
	{
		private sealed class Mutables
		{
			internal int _currentSection;

			internal int _currentChunkSize;

			internal int _currentPositionInChunk;

			internal int _currentChunkOffset;

			internal Mutables()
			{
				_currentSection = -1;
			}
		}

		private readonly T[] _data;

		private readonly int _elementCount;

		private readonly int _partitionCount;

		private readonly int _partitionIndex;

		private readonly int _maxChunkSize;

		private readonly int _sectionCount;

		private Mutables _mutables;

		internal ArrayIndexRangeEnumerator(T[] data, int partitionCount, int partitionIndex, int maxChunkSize)
		{
			_data = data;
			_elementCount = data.Length;
			_partitionCount = partitionCount;
			_partitionIndex = partitionIndex;
			_maxChunkSize = maxChunkSize;
			int num = maxChunkSize * partitionCount;
			_sectionCount = _elementCount / num + ((_elementCount % num != 0) ? 1 : 0);
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref T currentElement, ref int currentKey)
		{
			Mutables mutables = _mutables;
			if (mutables == null)
			{
				mutables = (_mutables = new Mutables());
			}
			if (++mutables._currentPositionInChunk < mutables._currentChunkSize || MoveNextSlowPath())
			{
				currentKey = mutables._currentChunkOffset + mutables._currentPositionInChunk;
				currentElement = _data[currentKey];
				return true;
			}
			return false;
		}

		private bool MoveNextSlowPath()
		{
			Mutables mutables = _mutables;
			int num = ++mutables._currentSection;
			int num2 = _sectionCount - num;
			if (num2 <= 0)
			{
				return false;
			}
			int num3 = num * _partitionCount * _maxChunkSize;
			mutables._currentPositionInChunk = 0;
			if (num2 > 1)
			{
				mutables._currentChunkSize = _maxChunkSize;
				mutables._currentChunkOffset = num3 + _partitionIndex * _maxChunkSize;
			}
			else
			{
				int num4 = _elementCount - num3;
				int num5 = num4 / _partitionCount;
				int num6 = num4 % _partitionCount;
				mutables._currentChunkSize = num5;
				if (_partitionIndex < num6)
				{
					mutables._currentChunkSize++;
				}
				if (mutables._currentChunkSize == 0)
				{
					return false;
				}
				mutables._currentChunkOffset = num3 + _partitionIndex * num5 + ((_partitionIndex < num6) ? _partitionIndex : num6);
			}
			return true;
		}
	}

	internal sealed class ArrayContiguousIndexRangeEnumerator : QueryOperatorEnumerator<T, int>
	{
		private readonly T[] _data;

		private readonly int _startIndex;

		private readonly int _maximumIndex;

		private Shared<int> _currentIndex;

		internal ArrayContiguousIndexRangeEnumerator(T[] data, int partitionCount, int partitionIndex)
		{
			_data = data;
			int num = data.Length / partitionCount;
			int num2 = data.Length % partitionCount;
			int num3 = partitionIndex * num + ((partitionIndex < num2) ? partitionIndex : num2);
			_startIndex = num3 - 1;
			_maximumIndex = num3 + num + ((partitionIndex < num2) ? 1 : 0);
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref T currentElement, ref int currentKey)
		{
			if (_currentIndex == null)
			{
				_currentIndex = new Shared<int>(_startIndex);
			}
			int num = ++_currentIndex.Value;
			if (num < _maximumIndex)
			{
				currentKey = num;
				currentElement = _data[num];
				return true;
			}
			return false;
		}
	}

	internal sealed class ListIndexRangeEnumerator : QueryOperatorEnumerator<T, int>
	{
		private sealed class Mutables
		{
			internal int _currentSection;

			internal int _currentChunkSize;

			internal int _currentPositionInChunk;

			internal int _currentChunkOffset;

			internal Mutables()
			{
				_currentSection = -1;
			}
		}

		private readonly IList<T> _data;

		private readonly int _elementCount;

		private readonly int _partitionCount;

		private readonly int _partitionIndex;

		private readonly int _maxChunkSize;

		private readonly int _sectionCount;

		private Mutables _mutables;

		internal ListIndexRangeEnumerator(IList<T> data, int partitionCount, int partitionIndex, int maxChunkSize)
		{
			_data = data;
			_elementCount = data.Count;
			_partitionCount = partitionCount;
			_partitionIndex = partitionIndex;
			_maxChunkSize = maxChunkSize;
			int num = maxChunkSize * partitionCount;
			_sectionCount = _elementCount / num + ((_elementCount % num != 0) ? 1 : 0);
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref T currentElement, ref int currentKey)
		{
			Mutables mutables = _mutables;
			if (mutables == null)
			{
				mutables = (_mutables = new Mutables());
			}
			if (++mutables._currentPositionInChunk < mutables._currentChunkSize || MoveNextSlowPath())
			{
				currentKey = mutables._currentChunkOffset + mutables._currentPositionInChunk;
				currentElement = _data[currentKey];
				return true;
			}
			return false;
		}

		private bool MoveNextSlowPath()
		{
			Mutables mutables = _mutables;
			int num = ++mutables._currentSection;
			int num2 = _sectionCount - num;
			if (num2 <= 0)
			{
				return false;
			}
			int num3 = num * _partitionCount * _maxChunkSize;
			mutables._currentPositionInChunk = 0;
			if (num2 > 1)
			{
				mutables._currentChunkSize = _maxChunkSize;
				mutables._currentChunkOffset = num3 + _partitionIndex * _maxChunkSize;
			}
			else
			{
				int num4 = _elementCount - num3;
				int num5 = num4 / _partitionCount;
				int num6 = num4 % _partitionCount;
				mutables._currentChunkSize = num5;
				if (_partitionIndex < num6)
				{
					mutables._currentChunkSize++;
				}
				if (mutables._currentChunkSize == 0)
				{
					return false;
				}
				mutables._currentChunkOffset = num3 + _partitionIndex * num5 + ((_partitionIndex < num6) ? _partitionIndex : num6);
			}
			return true;
		}
	}

	internal sealed class ListContiguousIndexRangeEnumerator : QueryOperatorEnumerator<T, int>
	{
		private readonly IList<T> _data;

		private readonly int _startIndex;

		private readonly int _maximumIndex;

		private Shared<int> _currentIndex;

		internal ListContiguousIndexRangeEnumerator(IList<T> data, int partitionCount, int partitionIndex)
		{
			_data = data;
			int num = data.Count / partitionCount;
			int num2 = data.Count % partitionCount;
			int num3 = partitionIndex * num + ((partitionIndex < num2) ? partitionIndex : num2);
			_startIndex = num3 - 1;
			_maximumIndex = num3 + num + ((partitionIndex < num2) ? 1 : 0);
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref T currentElement, ref int currentKey)
		{
			if (_currentIndex == null)
			{
				_currentIndex = new Shared<int>(_startIndex);
			}
			int num = ++_currentIndex.Value;
			if (num < _maximumIndex)
			{
				currentKey = num;
				currentElement = _data[num];
				return true;
			}
			return false;
		}
	}

	private sealed class ContiguousChunkLazyEnumerator : QueryOperatorEnumerator<T, int>
	{
		private sealed class Mutables
		{
			internal readonly T[] _chunkBuffer;

			internal int _nextChunkMaxSize;

			internal int _currentChunkSize;

			internal int _currentChunkIndex;

			internal int _chunkBaseIndex;

			internal int _chunkCounter;

			internal Mutables()
			{
				_nextChunkMaxSize = 1;
				_chunkBuffer = new T[Scheduling.GetDefaultChunkSize<T>()];
				_currentChunkSize = 0;
				_currentChunkIndex = -1;
				_chunkBaseIndex = 0;
				_chunkCounter = 0;
			}
		}

		private readonly IEnumerator<T> _source;

		private readonly object _sourceSyncLock;

		private readonly Shared<int> _currentIndex;

		private readonly Shared<int> _activeEnumeratorsCount;

		private readonly Shared<bool> _exceptionTracker;

		private Mutables _mutables;

		internal ContiguousChunkLazyEnumerator(IEnumerator<T> source, Shared<bool> exceptionTracker, object sourceSyncLock, Shared<int> currentIndex, Shared<int> degreeOfParallelism)
		{
			_source = source;
			_sourceSyncLock = sourceSyncLock;
			_currentIndex = currentIndex;
			_activeEnumeratorsCount = degreeOfParallelism;
			_exceptionTracker = exceptionTracker;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref T currentElement, ref int currentKey)
		{
			Mutables mutables = _mutables;
			if (mutables == null)
			{
				mutables = (_mutables = new Mutables());
			}
			T[] chunkBuffer;
			int num;
			while (true)
			{
				chunkBuffer = mutables._chunkBuffer;
				num = ++mutables._currentChunkIndex;
				if (num < mutables._currentChunkSize)
				{
					break;
				}
				lock (_sourceSyncLock)
				{
					int i = 0;
					if (_exceptionTracker.Value)
					{
						return false;
					}
					try
					{
						for (; i < mutables._nextChunkMaxSize; i++)
						{
							if (!_source.MoveNext())
							{
								break;
							}
							chunkBuffer[i] = _source.Current;
						}
					}
					catch
					{
						_exceptionTracker.Value = true;
						throw;
					}
					mutables._currentChunkSize = i;
					if (i == 0)
					{
						return false;
					}
					mutables._chunkBaseIndex = _currentIndex.Value;
					checked
					{
						_currentIndex.Value += i;
					}
				}
				if (mutables._nextChunkMaxSize < chunkBuffer.Length && (mutables._chunkCounter++ & 7) == 7)
				{
					mutables._nextChunkMaxSize *= 2;
					if (mutables._nextChunkMaxSize > chunkBuffer.Length)
					{
						mutables._nextChunkMaxSize = chunkBuffer.Length;
					}
				}
				mutables._currentChunkIndex = -1;
			}
			currentElement = chunkBuffer[num];
			currentKey = mutables._chunkBaseIndex + num;
			return true;
		}

		protected override void Dispose(bool disposing)
		{
			if (Interlocked.Decrement(ref _activeEnumeratorsCount.Value) == 0)
			{
				_source.Dispose();
			}
		}
	}

	internal PartitionedDataSource(IEnumerable<T> source, int partitionCount, bool useStriping)
		: base(partitionCount, (IComparer<int>)Util.GetDefaultComparer<int>(), (!(source is IList<T>)) ? OrdinalIndexState.Correct : OrdinalIndexState.Indexable)
	{
		InitializePartitions(source, partitionCount, useStriping);
	}

	private void InitializePartitions(IEnumerable<T> source, int partitionCount, bool useStriping)
	{
		if (source is ParallelEnumerableWrapper<T> parallelEnumerableWrapper)
		{
			source = parallelEnumerableWrapper.WrappedEnumerable;
		}
		if (source is IList<T> data)
		{
			QueryOperatorEnumerator<T, int>[] array = new QueryOperatorEnumerator<T, int>[partitionCount];
			T[] array2 = source as T[];
			int num = -1;
			if (useStriping)
			{
				num = Scheduling.GetDefaultChunkSize<T>();
				if (num < 1)
				{
					num = 1;
				}
			}
			for (int i = 0; i < partitionCount; i++)
			{
				if (array2 != null)
				{
					if (useStriping)
					{
						array[i] = new ArrayIndexRangeEnumerator(array2, partitionCount, i, num);
					}
					else
					{
						array[i] = new ArrayContiguousIndexRangeEnumerator(array2, partitionCount, i);
					}
				}
				else if (useStriping)
				{
					array[i] = new ListIndexRangeEnumerator(data, partitionCount, i, num);
				}
				else
				{
					array[i] = new ListContiguousIndexRangeEnumerator(data, partitionCount, i);
				}
			}
			_partitions = array;
		}
		else
		{
			_partitions = MakePartitions(source.GetEnumerator(), partitionCount);
		}
	}

	private static QueryOperatorEnumerator<T, int>[] MakePartitions(IEnumerator<T> source, int partitionCount)
	{
		QueryOperatorEnumerator<T, int>[] array = new QueryOperatorEnumerator<T, int>[partitionCount];
		object sourceSyncLock = new object();
		Shared<int> currentIndex = new Shared<int>(0);
		Shared<int> degreeOfParallelism = new Shared<int>(partitionCount);
		Shared<bool> exceptionTracker = new Shared<bool>(value: false);
		for (int i = 0; i < partitionCount; i++)
		{
			array[i] = new ContiguousChunkLazyEnumerator(source, exceptionTracker, sourceSyncLock, currentIndex, degreeOfParallelism);
		}
		return array;
	}
}
