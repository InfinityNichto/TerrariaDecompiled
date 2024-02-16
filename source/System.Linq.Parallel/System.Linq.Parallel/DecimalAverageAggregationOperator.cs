using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DecimalAverageAggregationOperator : InlinedAggregationOperator<decimal, Pair<decimal, long>, decimal>
{
	private sealed class DecimalAverageAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<Pair<decimal, long>>
	{
		private readonly QueryOperatorEnumerator<decimal, TKey> _source;

		internal DecimalAverageAggregationOperatorEnumerator(QueryOperatorEnumerator<decimal, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref Pair<decimal, long> currentElement)
		{
			decimal first = 0.0m;
			long num = 0L;
			QueryOperatorEnumerator<decimal, TKey> source = _source;
			decimal currentElement2 = default(decimal);
			TKey currentKey = default(TKey);
			if (source.MoveNext(ref currentElement2, ref currentKey))
			{
				int num2 = 0;
				do
				{
					if ((num2++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					first += currentElement2;
					num = checked(num + 1);
				}
				while (source.MoveNext(ref currentElement2, ref currentKey));
				currentElement = new Pair<decimal, long>(first, num);
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	internal DecimalAverageAggregationOperator(IEnumerable<decimal> child)
		: base(child)
	{
	}

	protected override decimal InternalAggregate(ref Exception singularExceptionToThrow)
	{
		checked
		{
			using IEnumerator<Pair<decimal, long>> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
			if (!enumerator.MoveNext())
			{
				singularExceptionToThrow = new InvalidOperationException(System.SR.NoElements);
				return 0m;
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

	protected override QueryOperatorEnumerator<Pair<decimal, long>, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<decimal, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new DecimalAverageAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
