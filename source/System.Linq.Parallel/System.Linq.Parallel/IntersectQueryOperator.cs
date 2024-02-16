using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class IntersectQueryOperator<TInputOutput> : BinaryQueryOperator<TInputOutput, TInputOutput, TInputOutput>
{
	private sealed class IntersectQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TInputOutput, int>
	{
		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource;

		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> _rightSource;

		private readonly IEqualityComparer<TInputOutput> _comparer;

		private HashSet<TInputOutput> _hashLookup;

		private readonly CancellationToken _cancellationToken;

		private Shared<int> _outputLoopCount;

		internal IntersectQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource, QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource, IEqualityComparer<TInputOutput> comparer, CancellationToken cancellationToken)
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
				if (_hashLookup.Remove(currentElement3.First))
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

	private sealed class OrderedIntersectQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TInputOutput, TLeftKey>
	{
		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource;

		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> _rightSource;

		private readonly IEqualityComparer<Wrapper<TInputOutput>> _comparer;

		private readonly IComparer<TLeftKey> _leftKeyComparer;

		private Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>> _hashLookup;

		private readonly CancellationToken _cancellationToken;

		internal OrderedIntersectQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource, QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource, IEqualityComparer<TInputOutput> comparer, IComparer<TLeftKey> leftKeyComparer, CancellationToken cancellationToken)
		{
			_leftSource = leftSource;
			_rightSource = rightSource;
			_comparer = new WrapperEqualityComparer<TInputOutput>(comparer);
			_leftKeyComparer = leftKeyComparer;
			_cancellationToken = cancellationToken;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TInputOutput currentElement, [AllowNull] ref TLeftKey currentKey)
		{
			int num = 0;
			if (_hashLookup == null)
			{
				_hashLookup = new Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>>(_comparer);
				Pair<TInputOutput, NoKeyMemoizationRequired> currentElement2 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
				TLeftKey currentKey2 = default(TLeftKey);
				while (_leftSource.MoveNext(ref currentElement2, ref currentKey2))
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					Wrapper<TInputOutput> key = new Wrapper<TInputOutput>(currentElement2.First);
					if (!_hashLookup.TryGetValue(key, out var value) || _leftKeyComparer.Compare(currentKey2, value.Second) < 0)
					{
						_hashLookup[key] = new Pair<TInputOutput, TLeftKey>(currentElement2.First, currentKey2);
					}
				}
			}
			Pair<TInputOutput, NoKeyMemoizationRequired> currentElement3 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
			int currentKey3 = 0;
			while (_rightSource.MoveNext(ref currentElement3, ref currentKey3))
			{
				if ((num++ & 0x3F) == 0)
				{
					_cancellationToken.ThrowIfCancellationRequested();
				}
				Wrapper<TInputOutput> key2 = new Wrapper<TInputOutput>(currentElement3.First);
				if (_hashLookup.TryGetValue(key2, out var value2))
				{
					currentElement = value2.First;
					currentKey = value2.Second;
					_hashLookup.Remove(new Wrapper<TInputOutput>(value2.First));
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

	private readonly IEqualityComparer<TInputOutput> _comparer;

	internal override bool LimitsParallelism => false;

	internal IntersectQueryOperator(ParallelQuery<TInputOutput> left, ParallelQuery<TInputOutput> right, IEqualityComparer<TInputOutput> comparer)
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

	public override void WrapPartitionedStream<TLeftKey, TRightKey>(PartitionedStream<TInputOutput, TLeftKey> leftPartitionedStream, PartitionedStream<TInputOutput, TRightKey> rightPartitionedStream, IPartitionedStreamRecipient<TInputOutput> outputRecipient, bool preferStriping, QuerySettings settings)
	{
		if (base.OutputOrdered)
		{
			WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartitionOrdered<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(leftPartitionedStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken), rightPartitionedStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
		}
		else
		{
			WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(leftPartitionedStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken), rightPartitionedStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
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
				partitionedStream2[i] = new OrderedIntersectQueryOperatorEnumerator<TLeftKey>(leftHashStream[i], partitionedStream[i], _comparer, leftHashStream.KeyComparer, cancellationToken);
			}
			else
			{
				partitionedStream2[i] = (QueryOperatorEnumerator<TInputOutput, TLeftKey>)(object)new IntersectQueryOperatorEnumerator<TLeftKey>(leftHashStream[i], partitionedStream[i], _comparer, cancellationToken);
			}
		}
		outputRecipient.Receive(partitionedStream2);
	}

	internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
	{
		IEnumerable<TInputOutput> first = CancellableEnumerable.Wrap(base.LeftChild.AsSequentialQuery(token), token);
		IEnumerable<TInputOutput> second = CancellableEnumerable.Wrap(base.RightChild.AsSequentialQuery(token), token);
		return first.Intersect(second, _comparer);
	}
}
