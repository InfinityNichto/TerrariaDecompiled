using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class TakeOrSkipQueryOperator<TResult> : UnaryQueryOperator<TResult, TResult>
{
	private sealed class TakeOrSkipQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TResult, TKey>
	{
		private readonly QueryOperatorEnumerator<TResult, TKey> _source;

		private readonly int _count;

		private readonly bool _take;

		private readonly IComparer<TKey> _keyComparer;

		private readonly FixedMaxHeap<TKey> _sharedIndices;

		private readonly CountdownEvent _sharedBarrier;

		private readonly CancellationToken _cancellationToken;

		private List<Pair<TResult, TKey>> _buffer;

		private Shared<int> _bufferIndex;

		internal TakeOrSkipQueryOperatorEnumerator(QueryOperatorEnumerator<TResult, TKey> source, bool take, FixedMaxHeap<TKey> sharedIndices, CountdownEvent sharedBarrier, CancellationToken cancellationToken, IComparer<TKey> keyComparer)
		{
			_source = source;
			_count = sharedIndices.Size;
			_take = take;
			_sharedIndices = sharedIndices;
			_sharedBarrier = sharedBarrier;
			_cancellationToken = cancellationToken;
			_keyComparer = keyComparer;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TResult currentElement, [AllowNull] ref TKey currentKey)
		{
			if (_buffer == null && _count > 0)
			{
				List<Pair<TResult, TKey>> list = new List<Pair<TResult, TKey>>();
				TResult currentElement2 = default(TResult);
				TKey currentKey2 = default(TKey);
				int num = 0;
				while (list.Count < _count && _source.MoveNext(ref currentElement2, ref currentKey2))
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					list.Add(new Pair<TResult, TKey>(currentElement2, currentKey2));
					lock (_sharedIndices)
					{
						if (!_sharedIndices.Insert(currentKey2))
						{
							break;
						}
					}
				}
				_sharedBarrier.Signal();
				_sharedBarrier.Wait(_cancellationToken);
				_buffer = list;
				_bufferIndex = new Shared<int>(-1);
			}
			if (_take)
			{
				if (_count == 0 || _bufferIndex.Value >= _buffer.Count - 1)
				{
					return false;
				}
				_bufferIndex.Value++;
				currentElement = _buffer[_bufferIndex.Value].First;
				currentKey = _buffer[_bufferIndex.Value].Second;
				if (_sharedIndices.Count != 0)
				{
					return _keyComparer.Compare(_buffer[_bufferIndex.Value].Second, _sharedIndices.MaxValue) <= 0;
				}
				return true;
			}
			TKey val = default(TKey);
			if (_count > 0)
			{
				if (_sharedIndices.Count < _count)
				{
					return false;
				}
				val = _sharedIndices.MaxValue;
				if (_bufferIndex.Value < _buffer.Count - 1)
				{
					_bufferIndex.Value++;
					while (_bufferIndex.Value < _buffer.Count)
					{
						if (_keyComparer.Compare(_buffer[_bufferIndex.Value].Second, val) > 0)
						{
							currentElement = _buffer[_bufferIndex.Value].First;
							currentKey = _buffer[_bufferIndex.Value].Second;
							return true;
						}
						_bufferIndex.Value++;
					}
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

	private sealed class TakeOrSkipQueryOperatorResults : UnaryQueryOperatorResults
	{
		private readonly TakeOrSkipQueryOperator<TResult> _takeOrSkipOp;

		private readonly int _childCount;

		internal override bool IsIndexible => _childCount >= 0;

		internal override int ElementsCount
		{
			get
			{
				if (_takeOrSkipOp._take)
				{
					return Math.Min(_childCount, _takeOrSkipOp._count);
				}
				return Math.Max(_childCount - _takeOrSkipOp._count, 0);
			}
		}

		public static QueryResults<TResult> NewResults(QueryResults<TResult> childQueryResults, TakeOrSkipQueryOperator<TResult> op, QuerySettings settings, bool preferStriping)
		{
			if (childQueryResults.IsIndexible)
			{
				return new TakeOrSkipQueryOperatorResults(childQueryResults, op, settings, preferStriping);
			}
			return new UnaryQueryOperatorResults(childQueryResults, op, settings, preferStriping);
		}

		private TakeOrSkipQueryOperatorResults(QueryResults<TResult> childQueryResults, TakeOrSkipQueryOperator<TResult> takeOrSkipOp, QuerySettings settings, bool preferStriping)
			: base(childQueryResults, (UnaryQueryOperator<TResult, TResult>)takeOrSkipOp, settings, preferStriping)
		{
			_takeOrSkipOp = takeOrSkipOp;
			_childCount = _childQueryResults.ElementsCount;
		}

		internal override TResult GetElement(int index)
		{
			if (_takeOrSkipOp._take)
			{
				return _childQueryResults.GetElement(index);
			}
			return _childQueryResults.GetElement(_takeOrSkipOp._count + index);
		}
	}

	private readonly int _count;

	private readonly bool _take;

	private bool _prematureMerge;

	internal override bool LimitsParallelism => false;

	internal TakeOrSkipQueryOperator(IEnumerable<TResult> child, int count, bool take)
		: base(child)
	{
		_count = count;
		_take = take;
		SetOrdinalIndexState(OutputOrdinalIndexState());
	}

	private OrdinalIndexState OutputOrdinalIndexState()
	{
		OrdinalIndexState ordinalIndexState = base.Child.OrdinalIndexState;
		if (ordinalIndexState == OrdinalIndexState.Indexable)
		{
			return OrdinalIndexState.Indexable;
		}
		if (ordinalIndexState.IsWorseThan(OrdinalIndexState.Increasing))
		{
			_prematureMerge = true;
			ordinalIndexState = OrdinalIndexState.Correct;
		}
		if (!_take && ordinalIndexState == OrdinalIndexState.Correct)
		{
			ordinalIndexState = OrdinalIndexState.Increasing;
		}
		return ordinalIndexState;
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
		FixedMaxHeap<TKey> sharedIndices = new FixedMaxHeap<TKey>(_count, inputStream.KeyComparer);
		CountdownEvent sharedBarrier = new CountdownEvent(partitionCount);
		PartitionedStream<TResult, TKey> partitionedStream = new PartitionedStream<TResult, TKey>(partitionCount, inputStream.KeyComparer, OrdinalIndexState);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new TakeOrSkipQueryOperatorEnumerator<TKey>(inputStream[i], _take, sharedIndices, sharedBarrier, settings.CancellationState.MergedCancellationToken, inputStream.KeyComparer);
		}
		recipient.Receive(partitionedStream);
	}

	internal override QueryResults<TResult> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TResult> childQueryResults = base.Child.Open(settings, preferStriping: true);
		return TakeOrSkipQueryOperatorResults.NewResults(childQueryResults, this, settings, preferStriping);
	}

	internal override IEnumerable<TResult> AsSequentialQuery(CancellationToken token)
	{
		if (_take)
		{
			return base.Child.AsSequentialQuery(token).Take(_count);
		}
		IEnumerable<TResult> source = CancellableEnumerable.Wrap(base.Child.AsSequentialQuery(token), token);
		return source.Skip(_count);
	}
}
