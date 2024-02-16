using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class TakeOrSkipWhileQueryOperator<TResult> : UnaryQueryOperator<TResult, TResult>
{
	private sealed class TakeOrSkipWhileQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TResult, TKey>
	{
		private readonly QueryOperatorEnumerator<TResult, TKey> _source;

		private readonly Func<TResult, bool> _predicate;

		private readonly Func<TResult, TKey, bool> _indexedPredicate;

		private readonly bool _take;

		private readonly IComparer<TKey> _keyComparer;

		private readonly OperatorState<TKey> _operatorState;

		private readonly CountdownEvent _sharedBarrier;

		private readonly CancellationToken _cancellationToken;

		private List<Pair<TResult, TKey>> _buffer;

		private Shared<int> _bufferIndex;

		private int _updatesSeen;

		private TKey _currentLowKey;

		internal TakeOrSkipWhileQueryOperatorEnumerator(QueryOperatorEnumerator<TResult, TKey> source, Func<TResult, bool> predicate, Func<TResult, TKey, bool> indexedPredicate, bool take, OperatorState<TKey> operatorState, CountdownEvent sharedBarrier, CancellationToken cancelToken, IComparer<TKey> keyComparer)
		{
			_source = source;
			_predicate = predicate;
			_indexedPredicate = indexedPredicate;
			_take = take;
			_operatorState = operatorState;
			_sharedBarrier = sharedBarrier;
			_cancellationToken = cancelToken;
			_keyComparer = keyComparer;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TResult currentElement, [AllowNull] ref TKey currentKey)
		{
			if (_buffer == null)
			{
				List<Pair<TResult, TKey>> list = new List<Pair<TResult, TKey>>();
				try
				{
					TResult currentElement2 = default(TResult);
					TKey currentKey2 = default(TKey);
					int num = 0;
					while (_source.MoveNext(ref currentElement2, ref currentKey2))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						list.Add(new Pair<TResult, TKey>(currentElement2, currentKey2));
						if (_updatesSeen != _operatorState._updatesDone)
						{
							lock (_operatorState)
							{
								_currentLowKey = _operatorState._currentLowKey;
								_updatesSeen = _operatorState._updatesDone;
							}
						}
						if (_updatesSeen > 0 && _keyComparer.Compare(currentKey2, _currentLowKey) > 0)
						{
							break;
						}
						if ((_predicate == null) ? _indexedPredicate(currentElement2, currentKey2) : _predicate(currentElement2))
						{
							continue;
						}
						lock (_operatorState)
						{
							if (_operatorState._updatesDone == 0 || _keyComparer.Compare(_operatorState._currentLowKey, currentKey2) > 0)
							{
								_currentLowKey = (_operatorState._currentLowKey = currentKey2);
								_updatesSeen = ++_operatorState._updatesDone;
							}
						}
						break;
					}
				}
				finally
				{
					_sharedBarrier.Signal();
				}
				_sharedBarrier.Wait(_cancellationToken);
				_buffer = list;
				_bufferIndex = new Shared<int>(-1);
			}
			if (_take)
			{
				if (_bufferIndex.Value >= _buffer.Count - 1)
				{
					return false;
				}
				_bufferIndex.Value++;
				currentElement = _buffer[_bufferIndex.Value].First;
				currentKey = _buffer[_bufferIndex.Value].Second;
				if (_operatorState._updatesDone != 0)
				{
					return _keyComparer.Compare(_operatorState._currentLowKey, currentKey) > 0;
				}
				return true;
			}
			if (_operatorState._updatesDone == 0)
			{
				return false;
			}
			if (_bufferIndex.Value < _buffer.Count - 1)
			{
				_bufferIndex.Value++;
				while (_bufferIndex.Value < _buffer.Count)
				{
					if (_keyComparer.Compare(_buffer[_bufferIndex.Value].Second, _operatorState._currentLowKey) >= 0)
					{
						currentElement = _buffer[_bufferIndex.Value].First;
						currentKey = _buffer[_bufferIndex.Value].Second;
						return true;
					}
					_bufferIndex.Value++;
				}
			}
			if (_source.MoveNext(ref currentElement, ref currentKey))
			{
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	private sealed class OperatorState<TKey>
	{
		internal volatile int _updatesDone;

		internal TKey _currentLowKey;
	}

	private readonly Func<TResult, bool> _predicate;

	private readonly Func<TResult, int, bool> _indexedPredicate;

	private readonly bool _take;

	private bool _prematureMerge;

	private bool _limitsParallelism;

	internal override bool LimitsParallelism => _limitsParallelism;

	internal TakeOrSkipWhileQueryOperator(IEnumerable<TResult> child, Func<TResult, bool> predicate, Func<TResult, int, bool> indexedPredicate, bool take)
		: base(child)
	{
		_predicate = predicate;
		_indexedPredicate = indexedPredicate;
		_take = take;
		InitOrderIndexState();
	}

	private void InitOrderIndexState()
	{
		OrdinalIndexState state = OrdinalIndexState.Increasing;
		OrdinalIndexState ordinalIndexState = base.Child.OrdinalIndexState;
		if (_indexedPredicate != null)
		{
			state = OrdinalIndexState.Correct;
			_limitsParallelism = ordinalIndexState == OrdinalIndexState.Increasing;
		}
		OrdinalIndexState ordinalIndexState2 = ordinalIndexState.Worse(OrdinalIndexState.Correct);
		if (ordinalIndexState2.IsWorseThan(state))
		{
			_prematureMerge = true;
		}
		if (!_take)
		{
			ordinalIndexState2 = ordinalIndexState2.Worse(OrdinalIndexState.Increasing);
		}
		SetOrdinalIndexState(ordinalIndexState2);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TResult, TKey> inputStream, IPartitionedStreamRecipient<TResult> recipient, bool preferStriping, QuerySettings settings)
	{
		if (_prematureMerge)
		{
			ListQueryResults<TResult> listQueryResults = QueryOperator<TResult>.ExecuteAndCollectResults(inputStream, inputStream.PartitionCount, base.Child.OutputOrdered, preferStriping, settings);
			PartitionedStream<TResult, int> partitionedStream = listQueryResults.GetPartitionedStream();
			WrapHelper(partitionedStream, recipient, settings);
		}
		else
		{
			WrapHelper(inputStream, recipient, settings);
		}
	}

	private void WrapHelper<TKey>(PartitionedStream<TResult, TKey> inputStream, IPartitionedStreamRecipient<TResult> recipient, QuerySettings settings)
	{
		int partitionCount = inputStream.PartitionCount;
		OperatorState<TKey> operatorState = new OperatorState<TKey>();
		CountdownEvent sharedBarrier = new CountdownEvent(partitionCount);
		Func<TResult, TKey, bool> indexedPredicate = (Func<TResult, TKey, bool>)(object)_indexedPredicate;
		PartitionedStream<TResult, TKey> partitionedStream = new PartitionedStream<TResult, TKey>(partitionCount, inputStream.KeyComparer, OrdinalIndexState);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new TakeOrSkipWhileQueryOperatorEnumerator<TKey>(inputStream[i], _predicate, indexedPredicate, _take, operatorState, sharedBarrier, settings.CancellationState.MergedCancellationToken, inputStream.KeyComparer);
		}
		recipient.Receive(partitionedStream);
	}

	internal override QueryResults<TResult> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TResult> childQueryResults = base.Child.Open(settings, preferStriping: true);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
	}

	internal override IEnumerable<TResult> AsSequentialQuery(CancellationToken token)
	{
		if (_take)
		{
			if (_indexedPredicate != null)
			{
				return base.Child.AsSequentialQuery(token).TakeWhile(_indexedPredicate);
			}
			return base.Child.AsSequentialQuery(token).TakeWhile(_predicate);
		}
		if (_indexedPredicate != null)
		{
			IEnumerable<TResult> source = CancellableEnumerable.Wrap(base.Child.AsSequentialQuery(token), token);
			return source.SkipWhile(_indexedPredicate);
		}
		IEnumerable<TResult> source2 = CancellableEnumerable.Wrap(base.Child.AsSequentialQuery(token), token);
		return source2.SkipWhile(_predicate);
	}
}
