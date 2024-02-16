using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class LastQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
{
	private sealed class LastQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, int>
	{
		private readonly QueryOperatorEnumerator<TSource, TKey> _source;

		private readonly Func<TSource, bool> _predicate;

		private bool _alreadySearched;

		private readonly int _partitionId;

		private readonly LastQueryOperatorState<TKey> _operatorState;

		private readonly CountdownEvent _sharedBarrier;

		private readonly CancellationToken _cancellationToken;

		private readonly IComparer<TKey> _keyComparer;

		internal LastQueryOperatorEnumerator(QueryOperatorEnumerator<TSource, TKey> source, Func<TSource, bool> predicate, LastQueryOperatorState<TKey> operatorState, CountdownEvent sharedBarrier, CancellationToken cancelToken, IComparer<TKey> keyComparer, int partitionId)
		{
			_source = source;
			_predicate = predicate;
			_operatorState = operatorState;
			_sharedBarrier = sharedBarrier;
			_cancellationToken = cancelToken;
			_keyComparer = keyComparer;
			_partitionId = partitionId;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TSource currentElement, ref int currentKey)
		{
			if (_alreadySearched)
			{
				return false;
			}
			TSource val = default(TSource);
			TKey val2 = default(TKey);
			bool flag = false;
			try
			{
				int num = 0;
				TSource currentElement2 = default(TSource);
				TKey currentKey2 = default(TKey);
				while (_source.MoveNext(ref currentElement2, ref currentKey2))
				{
					if ((num & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					if (_predicate == null || _predicate(currentElement2))
					{
						val = currentElement2;
						val2 = currentKey2;
						flag = true;
					}
					num++;
				}
				if (flag)
				{
					lock (_operatorState)
					{
						if (_operatorState._partitionId == -1 || _keyComparer.Compare(val2, _operatorState._key) > 0)
						{
							_operatorState._partitionId = _partitionId;
							_operatorState._key = val2;
						}
					}
				}
			}
			finally
			{
				_sharedBarrier.Signal();
			}
			_alreadySearched = true;
			if (_partitionId == _operatorState._partitionId)
			{
				_sharedBarrier.Wait(_cancellationToken);
				if (_operatorState._partitionId == _partitionId)
				{
					currentElement = val;
					currentKey = 0;
					return true;
				}
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	private sealed class LastQueryOperatorState<TKey>
	{
		internal TKey _key;

		internal int _partitionId = -1;
	}

	private readonly Func<TSource, bool> _predicate;

	private readonly bool _prematureMergeNeeded;

	internal override bool LimitsParallelism => false;

	internal LastQueryOperator(IEnumerable<TSource> child, Func<TSource, bool> predicate)
		: base(child)
	{
		_predicate = predicate;
		_prematureMergeNeeded = base.Child.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
	}

	internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TSource> childQueryResults = base.Child.Open(settings, preferStriping: false);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
	{
		if (_prematureMergeNeeded)
		{
			PartitionedStream<TSource, int> partitionedStream = QueryOperator<TSource>.ExecuteAndCollectResults(inputStream, inputStream.PartitionCount, base.Child.OutputOrdered, preferStriping, settings).GetPartitionedStream();
			WrapHelper(partitionedStream, recipient, settings);
		}
		else
		{
			WrapHelper(inputStream, recipient, settings);
		}
	}

	private void WrapHelper<TKey>(PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, QuerySettings settings)
	{
		int partitionCount = inputStream.PartitionCount;
		LastQueryOperatorState<TKey> operatorState = new LastQueryOperatorState<TKey>();
		CountdownEvent sharedBarrier = new CountdownEvent(partitionCount);
		PartitionedStream<TSource, int> partitionedStream = new PartitionedStream<TSource, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Shuffled);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new LastQueryOperatorEnumerator<TKey>(inputStream[i], _predicate, operatorState, sharedBarrier, settings.CancellationState.MergedCancellationToken, inputStream.KeyComparer, i);
		}
		recipient.Receive(partitionedStream);
	}

	[ExcludeFromCodeCoverage(Justification = "This method should never be called as fallback to sequential is handled in ParallelEnumerable.First()")]
	internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
	{
		throw new NotSupportedException();
	}
}
