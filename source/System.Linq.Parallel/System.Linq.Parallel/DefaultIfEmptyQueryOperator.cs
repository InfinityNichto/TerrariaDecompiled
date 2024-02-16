using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DefaultIfEmptyQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
{
	private sealed class DefaultIfEmptyQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, TKey>
	{
		private readonly QueryOperatorEnumerator<TSource, TKey> _source;

		private bool _lookedForEmpty;

		private readonly int _partitionIndex;

		private readonly int _partitionCount;

		private readonly TSource _defaultValue;

		private readonly Shared<int> _sharedEmptyCount;

		private readonly CountdownEvent _sharedLatch;

		private readonly CancellationToken _cancelToken;

		internal DefaultIfEmptyQueryOperatorEnumerator(QueryOperatorEnumerator<TSource, TKey> source, TSource defaultValue, int partitionIndex, int partitionCount, Shared<int> sharedEmptyCount, CountdownEvent sharedLatch, CancellationToken cancelToken)
		{
			_source = source;
			_defaultValue = defaultValue;
			_partitionIndex = partitionIndex;
			_partitionCount = partitionCount;
			_sharedEmptyCount = sharedEmptyCount;
			_sharedLatch = sharedLatch;
			_cancelToken = cancelToken;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TSource currentElement, [AllowNull] ref TKey currentKey)
		{
			bool flag = _source.MoveNext(ref currentElement, ref currentKey);
			if (!_lookedForEmpty)
			{
				_lookedForEmpty = true;
				if (!flag)
				{
					if (_partitionIndex == 0)
					{
						_sharedLatch.Wait(_cancelToken);
						_sharedLatch.Dispose();
						if (_sharedEmptyCount.Value == _partitionCount - 1)
						{
							currentElement = _defaultValue;
							currentKey = default(TKey);
							return true;
						}
						return false;
					}
					Interlocked.Increment(ref _sharedEmptyCount.Value);
				}
				if (_partitionIndex != 0)
				{
					_sharedLatch.Signal();
				}
			}
			return flag;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	private readonly TSource _defaultValue;

	internal override bool LimitsParallelism => false;

	internal DefaultIfEmptyQueryOperator(IEnumerable<TSource> child, TSource defaultValue)
		: base(child)
	{
		_defaultValue = defaultValue;
		SetOrdinalIndexState(base.Child.OrdinalIndexState.Worse(OrdinalIndexState.Correct));
	}

	internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TSource> childQueryResults = base.Child.Open(settings, preferStriping);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
	{
		int partitionCount = inputStream.PartitionCount;
		Shared<int> sharedEmptyCount = new Shared<int>(0);
		CountdownEvent sharedLatch = new CountdownEvent(partitionCount - 1);
		PartitionedStream<TSource, TKey> partitionedStream = new PartitionedStream<TSource, TKey>(partitionCount, inputStream.KeyComparer, OrdinalIndexState);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new DefaultIfEmptyQueryOperatorEnumerator<TKey>(inputStream[i], _defaultValue, i, partitionCount, sharedEmptyCount, sharedLatch, settings.CancellationState.MergedCancellationToken);
		}
		recipient.Receive(partitionedStream);
	}

	internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
	{
		return base.Child.AsSequentialQuery(token).DefaultIfEmpty(_defaultValue);
	}
}
