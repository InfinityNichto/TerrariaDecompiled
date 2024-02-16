using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class UnionQueryOperator<TInputOutput> : BinaryQueryOperator<TInputOutput, TInputOutput, TInputOutput>
{
	private sealed class UnionQueryOperatorEnumerator<TLeftKey, TRightKey> : QueryOperatorEnumerator<TInputOutput, int>
	{
		private QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource;

		private QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> _rightSource;

		private HashSet<TInputOutput> _hashLookup;

		private readonly CancellationToken _cancellationToken;

		private Shared<int> _outputLoopCount;

		private readonly IEqualityComparer<TInputOutput> _comparer;

		internal UnionQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource, QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightSource, IEqualityComparer<TInputOutput> comparer, CancellationToken cancellationToken)
		{
			_leftSource = leftSource;
			_rightSource = rightSource;
			_comparer = comparer;
			_cancellationToken = cancellationToken;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TInputOutput currentElement, ref int currentKey)
		{
			if (_hashLookup == null)
			{
				_hashLookup = new HashSet<TInputOutput>(_comparer);
				_outputLoopCount = new Shared<int>(0);
			}
			if (_leftSource != null)
			{
				TLeftKey currentKey2 = default(TLeftKey);
				Pair<TInputOutput, NoKeyMemoizationRequired> currentElement2 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
				int num = 0;
				while (_leftSource.MoveNext(ref currentElement2, ref currentKey2))
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					if (_hashLookup.Add(currentElement2.First))
					{
						currentElement = currentElement2.First;
						return true;
					}
				}
				_leftSource.Dispose();
				_leftSource = null;
			}
			if (_rightSource != null)
			{
				TRightKey currentKey3 = default(TRightKey);
				Pair<TInputOutput, NoKeyMemoizationRequired> currentElement3 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
				while (_rightSource.MoveNext(ref currentElement3, ref currentKey3))
				{
					if ((_outputLoopCount.Value++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					if (_hashLookup.Add(currentElement3.First))
					{
						currentElement = currentElement3.First;
						return true;
					}
				}
				_rightSource.Dispose();
				_rightSource = null;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			if (_leftSource != null)
			{
				_leftSource.Dispose();
			}
			if (_rightSource != null)
			{
				_rightSource.Dispose();
			}
		}
	}

	private sealed class OrderedUnionQueryOperatorEnumerator<TLeftKey, TRightKey> : QueryOperatorEnumerator<TInputOutput, ConcatKey<TLeftKey, TRightKey>>
	{
		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource;

		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> _rightSource;

		private readonly IComparer<ConcatKey<TLeftKey, TRightKey>> _keyComparer;

		private IEnumerator<KeyValuePair<Wrapper<TInputOutput>, Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>>> _outputEnumerator;

		private readonly bool _leftOrdered;

		private readonly bool _rightOrdered;

		private readonly IEqualityComparer<TInputOutput> _comparer;

		private readonly CancellationToken _cancellationToken;

		internal OrderedUnionQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource, QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightSource, bool leftOrdered, bool rightOrdered, IEqualityComparer<TInputOutput> comparer, IComparer<ConcatKey<TLeftKey, TRightKey>> keyComparer, CancellationToken cancellationToken)
		{
			_leftSource = leftSource;
			_rightSource = rightSource;
			_keyComparer = keyComparer;
			_leftOrdered = leftOrdered;
			_rightOrdered = rightOrdered;
			_comparer = comparer;
			if (_comparer == null)
			{
				_comparer = EqualityComparer<TInputOutput>.Default;
			}
			_cancellationToken = cancellationToken;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TInputOutput currentElement, ref ConcatKey<TLeftKey, TRightKey> currentKey)
		{
			if (_outputEnumerator == null)
			{
				IEqualityComparer<Wrapper<TInputOutput>> comparer = new WrapperEqualityComparer<TInputOutput>(_comparer);
				Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>> dictionary = new Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>>(comparer);
				Pair<TInputOutput, NoKeyMemoizationRequired> currentElement2 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
				TLeftKey currentKey2 = default(TLeftKey);
				int num = 0;
				while (_leftSource.MoveNext(ref currentElement2, ref currentKey2))
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					ConcatKey<TLeftKey, TRightKey> concatKey = ConcatKey<TLeftKey, TRightKey>.MakeLeft(_leftOrdered ? currentKey2 : default(TLeftKey));
					Wrapper<TInputOutput> key = new Wrapper<TInputOutput>(currentElement2.First);
					if (!dictionary.TryGetValue(key, out var value) || _keyComparer.Compare(concatKey, value.Second) < 0)
					{
						dictionary[key] = new Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>(currentElement2.First, concatKey);
					}
				}
				TRightKey currentKey3 = default(TRightKey);
				while (_rightSource.MoveNext(ref currentElement2, ref currentKey3))
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					ConcatKey<TLeftKey, TRightKey> concatKey2 = ConcatKey<TLeftKey, TRightKey>.MakeRight(_rightOrdered ? currentKey3 : default(TRightKey));
					Wrapper<TInputOutput> key2 = new Wrapper<TInputOutput>(currentElement2.First);
					if (!dictionary.TryGetValue(key2, out var value2) || _keyComparer.Compare(concatKey2, value2.Second) < 0)
					{
						dictionary[key2] = new Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>(currentElement2.First, concatKey2);
					}
				}
				_outputEnumerator = dictionary.GetEnumerator();
			}
			if (_outputEnumerator.MoveNext())
			{
				Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>> value3 = _outputEnumerator.Current.Value;
				currentElement = value3.First;
				currentKey = value3.Second;
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_leftSource.Dispose();
			_rightSource.Dispose();
		}
	}

	private readonly IEqualityComparer<TInputOutput> _comparer;

	internal override bool LimitsParallelism => false;

	internal UnionQueryOperator(ParallelQuery<TInputOutput> left, ParallelQuery<TInputOutput> right, IEqualityComparer<TInputOutput> comparer)
		: base(left, right)
	{
		_comparer = comparer;
		_outputOrdered = base.LeftChild.OutputOrdered || base.RightChild.OutputOrdered;
	}

	internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TInputOutput> leftChildQueryResults = base.LeftChild.Open(settings, preferStriping: false);
		QueryResults<TInputOutput> rightChildQueryResults = base.RightChild.Open(settings, preferStriping: false);
		return new BinaryQueryOperatorResults(leftChildQueryResults, rightChildQueryResults, this, settings, preferStriping: false);
	}

	public override void WrapPartitionedStream<TLeftKey, TRightKey>(PartitionedStream<TInputOutput, TLeftKey> leftStream, PartitionedStream<TInputOutput, TRightKey> rightStream, IPartitionedStreamRecipient<TInputOutput> outputRecipient, bool preferStriping, QuerySettings settings)
	{
		int partitionCount = leftStream.PartitionCount;
		if (base.LeftChild.OutputOrdered)
		{
			PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream = ExchangeUtilities.HashRepartitionOrdered<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(leftStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken);
			WrapPartitionedStreamFixedLeftType(leftHashStream, rightStream, outputRecipient, partitionCount, settings.CancellationState.MergedCancellationToken);
		}
		else
		{
			PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, int> leftHashStream2 = ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(leftStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken);
			WrapPartitionedStreamFixedLeftType(leftHashStream2, rightStream, outputRecipient, partitionCount, settings.CancellationState.MergedCancellationToken);
		}
	}

	private void WrapPartitionedStreamFixedLeftType<TLeftKey, TRightKey>(PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream, PartitionedStream<TInputOutput, TRightKey> rightStream, IPartitionedStreamRecipient<TInputOutput> outputRecipient, int partitionCount, CancellationToken cancellationToken)
	{
		if (base.RightChild.OutputOrdered)
		{
			PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightHashStream = ExchangeUtilities.HashRepartitionOrdered<TInputOutput, NoKeyMemoizationRequired, TRightKey>(rightStream, null, null, _comparer, cancellationToken);
			WrapPartitionedStreamFixedBothTypes(leftHashStream, rightHashStream, outputRecipient, partitionCount, cancellationToken);
		}
		else
		{
			PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightHashStream2 = ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TRightKey>(rightStream, null, null, _comparer, cancellationToken);
			WrapPartitionedStreamFixedBothTypes(leftHashStream, rightHashStream2, outputRecipient, partitionCount, cancellationToken);
		}
	}

	private void WrapPartitionedStreamFixedBothTypes<TLeftKey, TRightKey>(PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream, PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightHashStream, IPartitionedStreamRecipient<TInputOutput> outputRecipient, int partitionCount, CancellationToken cancellationToken)
	{
		if (base.LeftChild.OutputOrdered || base.RightChild.OutputOrdered)
		{
			IComparer<ConcatKey<TLeftKey, TRightKey>> keyComparer = ConcatKey<TLeftKey, TRightKey>.MakeComparer(leftHashStream.KeyComparer, rightHashStream.KeyComparer);
			PartitionedStream<TInputOutput, ConcatKey<TLeftKey, TRightKey>> partitionedStream = new PartitionedStream<TInputOutput, ConcatKey<TLeftKey, TRightKey>>(partitionCount, keyComparer, OrdinalIndexState.Shuffled);
			for (int i = 0; i < partitionCount; i++)
			{
				partitionedStream[i] = new OrderedUnionQueryOperatorEnumerator<TLeftKey, TRightKey>(leftHashStream[i], rightHashStream[i], base.LeftChild.OutputOrdered, base.RightChild.OutputOrdered, _comparer, keyComparer, cancellationToken);
			}
			outputRecipient.Receive(partitionedStream);
		}
		else
		{
			PartitionedStream<TInputOutput, int> partitionedStream2 = new PartitionedStream<TInputOutput, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Shuffled);
			for (int j = 0; j < partitionCount; j++)
			{
				partitionedStream2[j] = new UnionQueryOperatorEnumerator<TLeftKey, TRightKey>(leftHashStream[j], rightHashStream[j], _comparer, cancellationToken);
			}
			outputRecipient.Receive(partitionedStream2);
		}
	}

	internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
	{
		IEnumerable<TInputOutput> first = CancellableEnumerable.Wrap(base.LeftChild.AsSequentialQuery(token), token);
		IEnumerable<TInputOutput> second = CancellableEnumerable.Wrap(base.RightChild.AsSequentialQuery(token), token);
		return first.Union(second, _comparer);
	}
}
