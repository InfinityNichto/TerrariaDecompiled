using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DistinctQueryOperator<TInputOutput> : UnaryQueryOperator<TInputOutput, TInputOutput>
{
	private sealed class DistinctQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TInputOutput, int>
	{
		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> _source;

		private readonly HashSet<TInputOutput> _hashLookup;

		private readonly CancellationToken _cancellationToken;

		private Shared<int> _outputLoopCount;

		internal DistinctQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> source, IEqualityComparer<TInputOutput> comparer, CancellationToken cancellationToken)
		{
			_source = source;
			_hashLookup = new HashSet<TInputOutput>(comparer);
			_cancellationToken = cancellationToken;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TInputOutput currentElement, ref int currentKey)
		{
			TKey currentKey2 = default(TKey);
			Pair<TInputOutput, NoKeyMemoizationRequired> currentElement2 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
			if (_outputLoopCount == null)
			{
				_outputLoopCount = new Shared<int>(0);
			}
			while (_source.MoveNext(ref currentElement2, ref currentKey2))
			{
				if ((_outputLoopCount.Value++ & 0x3F) == 0)
				{
					_cancellationToken.ThrowIfCancellationRequested();
				}
				if (_hashLookup.Add(currentElement2.First))
				{
					currentElement = currentElement2.First;
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

	private sealed class OrderedDistinctQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TInputOutput, TKey>
	{
		private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> _source;

		private readonly Dictionary<Wrapper<TInputOutput>, TKey> _hashLookup;

		private readonly IComparer<TKey> _keyComparer;

		private IEnumerator<KeyValuePair<Wrapper<TInputOutput>, TKey>> _hashLookupEnumerator;

		private readonly CancellationToken _cancellationToken;

		internal OrderedDistinctQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> source, IEqualityComparer<TInputOutput> comparer, IComparer<TKey> keyComparer, CancellationToken cancellationToken)
		{
			_source = source;
			_keyComparer = keyComparer;
			_hashLookup = new Dictionary<Wrapper<TInputOutput>, TKey>(new WrapperEqualityComparer<TInputOutput>(comparer));
			_cancellationToken = cancellationToken;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TInputOutput currentElement, [AllowNull] ref TKey currentKey)
		{
			if (_hashLookupEnumerator == null)
			{
				Pair<TInputOutput, NoKeyMemoizationRequired> currentElement2 = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
				TKey currentKey2 = default(TKey);
				int num = 0;
				while (_source.MoveNext(ref currentElement2, ref currentKey2))
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					Wrapper<TInputOutput> key = new Wrapper<TInputOutput>(currentElement2.First);
					if (!_hashLookup.TryGetValue(key, out var value) || _keyComparer.Compare(currentKey2, value) < 0)
					{
						_hashLookup[key] = currentKey2;
					}
				}
				_hashLookupEnumerator = _hashLookup.GetEnumerator();
			}
			if (_hashLookupEnumerator.MoveNext())
			{
				KeyValuePair<Wrapper<TInputOutput>, TKey> current = _hashLookupEnumerator.Current;
				currentElement = current.Key.Value;
				currentKey = current.Value;
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
			if (_hashLookupEnumerator != null)
			{
				_hashLookupEnumerator.Dispose();
			}
		}
	}

	private readonly IEqualityComparer<TInputOutput> _comparer;

	internal override bool LimitsParallelism => false;

	internal DistinctQueryOperator(IEnumerable<TInputOutput> source, IEqualityComparer<TInputOutput> comparer)
		: base(source)
	{
		_comparer = comparer;
		SetOrdinalIndexState(OrdinalIndexState.Shuffled);
	}

	internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TInputOutput> childQueryResults = base.Child.Open(settings, preferStriping: false);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping: false);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TInputOutput, TKey> inputStream, IPartitionedStreamRecipient<TInputOutput> recipient, bool preferStriping, QuerySettings settings)
	{
		if (base.OutputOrdered)
		{
			WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartitionOrdered<TInputOutput, NoKeyMemoizationRequired, TKey>(inputStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken), recipient, settings.CancellationState.MergedCancellationToken);
		}
		else
		{
			WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TKey>(inputStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken), recipient, settings.CancellationState.MergedCancellationToken);
		}
	}

	private void WrapPartitionedStreamHelper<TKey>(PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> hashStream, IPartitionedStreamRecipient<TInputOutput> recipient, CancellationToken cancellationToken)
	{
		int partitionCount = hashStream.PartitionCount;
		PartitionedStream<TInputOutput, TKey> partitionedStream = new PartitionedStream<TInputOutput, TKey>(partitionCount, hashStream.KeyComparer, OrdinalIndexState.Shuffled);
		for (int i = 0; i < partitionCount; i++)
		{
			if (base.OutputOrdered)
			{
				partitionedStream[i] = new OrderedDistinctQueryOperatorEnumerator<TKey>(hashStream[i], _comparer, hashStream.KeyComparer, cancellationToken);
			}
			else
			{
				partitionedStream[i] = (QueryOperatorEnumerator<TInputOutput, TKey>)(object)new DistinctQueryOperatorEnumerator<TKey>(hashStream[i], _comparer, cancellationToken);
			}
		}
		recipient.Receive(partitionedStream);
	}

	internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
	{
		IEnumerable<TInputOutput> source = CancellableEnumerable.Wrap(base.Child.AsSequentialQuery(token), token);
		return source.Distinct(_comparer);
	}
}
