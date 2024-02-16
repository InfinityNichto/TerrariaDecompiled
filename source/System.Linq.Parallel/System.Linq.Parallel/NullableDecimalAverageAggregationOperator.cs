using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableDecimalAverageAggregationOperator : InlinedAggregationOperator<decimal?, Pair<decimal, long>, decimal?>
{
	private sealed class NullableDecimalAverageAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<Pair<decimal, long>>
	{
		private readonly QueryOperatorEnumerator<decimal?, TKey> _source;

		internal NullableDecimalAverageAggregationOperatorEnumerator(QueryOperatorEnumerator<decimal?, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref Pair<decimal, long> currentElement)
		{
			decimal first = 0.0m;
			long num = 0L;
			QueryOperatorEnumerator<decimal?, TKey> source = _source;
			decimal? currentElement2 = null;
			TKey currentKey = default(TKey);
			int num2 = 0;
			while (source.MoveNext(ref currentElement2, ref currentKey))
			{
				if ((num2++ & 0x3F) == 0)
				{
					_cancellationToken.ThrowIfCancellationRequested();
				}
				if (currentElement2.HasValue)
				{
					first += currentElement2.GetValueOrDefault();
					num = checked(num + 1);
				}
			}
			currentElement = new Pair<decimal, long>(first, num);
			return num > 0;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	internal NullableDecimalAverageAggregationOperator(IEnumerable<decimal?> child)
		: base(child)
	{
	}

	protected override decimal? InternalAggregate(ref Exception singularExceptionToThrow)
	{
		checked
		{
			using IEnumerator<Pair<decimal, long>> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
			if (!enumerator.MoveNext())
			{
				return null;
			}
			Pair<decimal, long> current = enumerator.Current;
			while (enumerator.MoveNext())
			{
				current.First += enumerator.Current.First;
				current.Second += enumerator.Current.Second;
			}
			return current.First / (decimal)current.Second;
		}
	}

	protected override QueryOperatorEnumerator<Pair<decimal, long>, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<decimal?, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new NullableDecimalAverageAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
