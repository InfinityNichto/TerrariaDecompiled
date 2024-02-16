using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ExceptQueryOperator<TInputOutput> : BinaryQueryOperator<TInputOutput, TInputOutput, TInputOutput>
{
	private sealed class ExceptQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TInputOutput, int>
	{
		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource;

		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> _rightSource;

		private readonly IEqualityComparer<TInputOutput> _comparer;

		private HashSet<TInputOutput> _hashLookup;

		private readonly CancellationToken _cancellationToken;

		private Shared<int> _outputLoopCount;

		internal ExceptQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource, QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource, IEqualityComparer<TInputOutput> comparer, CancellationToken cancellationToken)
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
				_outputLoopCount = new Shared<int>(0);
				_hashLookup = new HashSet<TInputOutput>(_comparer);
				Pair<TInputOutput, NoKeyMemoizationRequired> currentElement2 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
				int currentKey2 = 0;
				int num = 0;
				while (_rightSource.MoveNext(ref currentElement2, ref currentKey2))
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					_hashLookup.Add(currentElement2.First);
				}
			}
			Pair<TInputOutput, NoKeyMemoizationRequired> currentElement3 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
			TLeftKey currentKey3 = default(TLeftKey);
			while (_leftSource.MoveNext(ref currentElement3, ref currentKey3))
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
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_leftSource.Dispose();
			_rightSource.Dispose();
		}
	}

	private sealed class OrderedExceptQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TInputOutput, TLeftKey>
	{
		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource;

		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> _rightSource;

		private readonly IEqualityComparer<TInputOutput> _comparer;

		private readonly IComparer<TLeftKey> _leftKeyComparer;

		private IEnumerator<KeyValuePair<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>>> _outputEnumerator;

		private readonly CancellationToken _cancellationToken;

		internal OrderedExceptQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource, QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource, IEqualityComparer<TInputOutput> comparer, IComparer<TLeftKey> leftKeyComparer, CancellationToken cancellationToken)
		{
			_leftSource = leftSource;
			_rightSource = rightSource;
			_comparer = comparer;
			_leftKeyComparer = leftKeyComparer;
			_cancellationToken = cancellationToken;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TInputOutput currentElement, [AllowNull] ref TLeftKey currentKey)
		{
			if (_outputEnumerator == null)
			{
				HashSet<TInputOutput> hashSet = new HashSet<TInputOutput>(_comparer);
				Pair<TInputOutput, NoKeyMemoizationRequired> currentElement2 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
				int currentKey2 = 0;
				int num = 0;
				while (_rightSource.MoveNext(ref currentElement2, ref currentKey2))
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					hashSet.Add(currentElement2.First);
				}
				Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>> dictionary = new Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>>(new WrapperEqualityComparer<TInputOutput>(_comparer));
				Pair<TInputOutput, NoKeyMemoizationRequired> currentElement3 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
				TLeftKey currentKey3 = default(TLeftKey);
				while (_leftSource.MoveNext(ref currentElement3, ref currentKey3))
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					if (!hashSet.Contains(currentElement3.First))
					{
						Wrapper<TInputOutput> key = new Wrapper<TInputOutput>(currentElement3.First);
						if (!dictionary.TryGetValue(key, out var value) || _leftKeyComparer.Compare(currentKey3, value.Second) < 0)
						{
							dictionary[key] = new Pair<TInputOutput, TLeftKey>(currentElement3.First, currentKey3);
						}
					}
				}
				_outputEnumerator = dictionary.GetEnumerator();
			}
			if (_outputEnumerator.MoveNext())
			{
				Pair<TInputOutput, TLeftKey> value2 = _outputEnumerator.Current.Value;
				currentElement = value2.First;
				currentKey = value2.Second;
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

	internal ExceptQueryOperator(ParallelQuery<TInputOutput> left, ParallelQuery<TInputOutput> right, IEqualityComparer<TInputOutput> comparer)
		: base(left, right)
	{
		_comparer = comparer;
		_outputOrdered = base.LeftChild.OutputOrdered;
		SetOrdinalIndex(OrdinalIndexState.Shuffled);
	}

	internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TInputOutput> leftChildQueryResults = base.LeftChild.Open(settings, preferStriping: false);
		QueryResults<TInputOutput> rightChildQueryResults = base.RightChild.Open(settings, preferStriping: false);
		return new BinaryQueryOperatorResults(leftChildQueryResults, rightChildQueryResults, this, settings, preferStriping: false);
	}

	public override void WrapPartitionedStream<TLeftKey, TRightKey>(PartitionedStream<TInputOutput, TLeftKey> leftStream, PartitionedStream<TInputOutput, TRightKey> rightStream, IPartitionedStreamRecipient<TInputOutput> outputRecipient, bool preferStriping, QuerySettings settings)
	{
		if (base.OutputOrdered)
		{
			WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartitionOrdered<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(leftStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
		}
		else
		{
			WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(leftStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
		}
	}

	private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream, PartitionedStream<TInputOutput, TRightKey> rightPartitionedStream, IPartitionedStreamRecipient<TInputOutput> outputRecipient, CancellationToken cancellationToken)
	{
		int partitionCount = leftHashStream.PartitionCount;
		PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, int> partitionedStream = ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TRightKey>(rightPartitionedStream, null, null, _comparer, cancellationToken);
		PartitionedStream<TInputOutput, TLeftKey> partitionedStream2 = new PartitionedStream<TInputOutput, TLeftKey>(partitionCount, leftHashStream.KeyComparer, OrdinalIndexState.Shuffled);
		for (int i = 0; i < partitionCount; i++)
		{
			if (base.OutputOrdered)
			{
				partitionedStream2[i] = new OrderedExceptQueryOperatorEnumerator<TLeftKey>(leftHashStream[i], partitionedStream[i], _comparer, leftHashStream.KeyComparer, cancellationToken);
			}
			else
			{
				partitionedStream2[i] = (QueryOperatorEnumerator<TInputOutput, TLeftKey>)(object)new ExceptQueryOperatorEnumerator<TLeftKey>(leftHashStream[i], partitionedStream[i], _comparer, cancellationToken);
			}
		}
		outputRecipient.Receive(partitionedStream2);
	}

	internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
	{
		IEnumerable<TInputOutput> first = CancellableEnumerable.Wrap(base.LeftChild.AsSequentialQuery(token), token);
		IEnumerable<TInputOutput> second = CancellableEnumerable.Wrap(base.RightChild.AsSequentialQuery(token), token);
		return first.Except(second, _comparer);
	}
}
