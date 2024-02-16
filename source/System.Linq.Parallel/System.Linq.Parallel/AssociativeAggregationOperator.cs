using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class AssociativeAggregationOperator<TInput, TIntermediate, TOutput> : UnaryQueryOperator<TInput, TIntermediate>
{
	private sealed class AssociativeAggregationOperatorEnumerator<TKey> : QueryOperatorEnumerator<TIntermediate, int>
	{
		private readonly QueryOperatorEnumerator<TInput, TKey> _source;

		private readonly AssociativeAggregationOperator<TInput, TIntermediate, TOutput> _reduceOperator;

		private readonly int _partitionIndex;

		private readonly CancellationToken _cancellationToken;

		private bool _accumulated;

		internal AssociativeAggregationOperatorEnumerator(QueryOperatorEnumerator<TInput, TKey> source, AssociativeAggregationOperator<TInput, TIntermediate, TOutput> reduceOperator, int partitionIndex, CancellationToken cancellationToken)
		{
			_source = source;
			_reduceOperator = reduceOperator;
			_partitionIndex = partitionIndex;
			_cancellationToken = cancellationToken;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TIntermediate currentElement, ref int currentKey)
		{
			if (_accumulated)
			{
				return false;
			}
			_accumulated = true;
			bool flag = false;
			TIntermediate val = default(TIntermediate);
			if (_reduceOperator._seedIsSpecified)
			{
				val = ((_reduceOperator._seedFactory == null) ? _reduceOperator._seed : _reduceOperator._seedFactory());
			}
			else
			{
				TInput currentElement2 = default(TInput);
				TKey currentKey2 = default(TKey);
				if (!_source.MoveNext(ref currentElement2, ref currentKey2))
				{
					return false;
				}
				flag = true;
				val = (TIntermediate)(object)currentElement2;
			}
			TInput currentElement3 = default(TInput);
			TKey currentKey3 = default(TKey);
			int num = 0;
			while (_source.MoveNext(ref currentElement3, ref currentKey3))
			{
				if ((num++ & 0x3F) == 0)
				{
					_cancellationToken.ThrowIfCancellationRequested();
				}
				flag = true;
				val = _reduceOperator._intermediateReduce(val, currentElement3);
			}
			if (flag)
			{
				currentElement = val;
				currentKey = _partitionIndex;
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	private readonly TIntermediate _seed;

	private readonly bool _seedIsSpecified;

	private readonly bool _throwIfEmpty;

	private readonly Func<TIntermediate, TInput, TIntermediate> _intermediateReduce;

	private readonly Func<TIntermediate, TIntermediate, TIntermediate> _finalReduce;

	private readonly Func<TIntermediate, TOutput> _resultSelector;

	private readonly Func<TIntermediate> _seedFactory;

	internal override bool LimitsParallelism => false;

	internal AssociativeAggregationOperator(IEnumerable<TInput> child, TIntermediate seed, Func<TIntermediate> seedFactory, bool seedIsSpecified, Func<TIntermediate, TInput, TIntermediate> intermediateReduce, Func<TIntermediate, TIntermediate, TIntermediate> finalReduce, Func<TIntermediate, TOutput> resultSelector, bool throwIfEmpty, QueryAggregationOptions options)
		: base(child)
	{
		_seed = seed;
		_seedFactory = seedFactory;
		_seedIsSpecified = seedIsSpecified;
		_intermediateReduce = intermediateReduce;
		_finalReduce = finalReduce;
		_resultSelector = resultSelector;
		_throwIfEmpty = throwIfEmpty;
	}

	internal TOutput Aggregate()
	{
		TIntermediate val = default(TIntermediate);
		bool flag = false;
		using (IEnumerator<TIntermediate> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true))
		{
			while (enumerator.MoveNext())
			{
				if (flag)
				{
					try
					{
						val = _finalReduce(val, enumerator.Current);
					}
					catch (Exception ex)
					{
						throw new AggregateException(ex);
					}
				}
				else
				{
					val = enumerator.Current;
					flag = true;
				}
			}
			if (!flag)
			{
				if (_throwIfEmpty)
				{
					throw new InvalidOperationException(System.SR.NoElements);
				}
				val = ((_seedFactory == null) ? _seed : _seedFactory());
			}
		}
		try
		{
			return _resultSelector(val);
		}
		catch (Exception ex2)
		{
			throw new AggregateException(ex2);
		}
	}

	internal override QueryResults<TIntermediate> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TInput> childQueryResults = base.Child.Open(settings, preferStriping);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<TIntermediate> recipient, bool preferStriping, QuerySettings settings)
	{
		int partitionCount = inputStream.PartitionCount;
		PartitionedStream<TIntermediate, int> partitionedStream = new PartitionedStream<TIntermediate, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new AssociativeAggregationOperatorEnumerator<TKey>(inputStream[i], this, i, settings.CancellationState.MergedCancellationToken);
		}
		recipient.Receive(partitionedStream);
	}

	[ExcludeFromCodeCoverage(Justification = "This method should never be called. Associative aggregation can always be parallelized")]
	internal override IEnumerable<TIntermediate> AsSequentialQuery(CancellationToken token)
	{
		throw new NotSupportedException();
	}
}
