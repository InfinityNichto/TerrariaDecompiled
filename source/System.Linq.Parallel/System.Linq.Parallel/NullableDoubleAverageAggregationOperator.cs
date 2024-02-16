using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableDoubleAverageAggregationOperator : InlinedAggregationOperator<double?, Pair<double, long>, double?>
{
	private sealed class NullableDoubleAverageAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<Pair<double, long>>
	{
		private readonly QueryOperatorEnumerator<double?, TKey> _source;

		internal NullableDoubleAverageAggregationOperatorEnumerator(QueryOperatorEnumerator<double?, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref Pair<double, long> currentElement)
		{
			double num = 0.0;
			long num2 = 0L;
			QueryOperatorEnumerator<double?, TKey> source = _source;
			double? currentElement2 = null;
			TKey currentKey = default(TKey);
			int num3 = 0;
			while (source.MoveNext(ref currentElement2, ref currentKey))
			{
				if (currentElement2.HasValue)
				{
					if ((num3++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					num += currentElement2.GetValueOrDefault();
					num2 = checked(num2 + 1);
				}
			}
			currentElement = new Pair<double, long>(num, num2);
			return num2 > 0;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	internal NullableDoubleAverageAggregationOperator(IEnumerable<double?> child)
		: base(child)
	{
	}

	protected override double? InternalAggregate(ref Exception singularExceptionToThrow)
	{
		checked
		{
			using IEnumerator<Pair<double, long>> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
			if (!enumerator.MoveNext())
			{
				return null;
			}
			Pair<double, long> current = enumerator.Current;
			while (enumerator.MoveNext())
			{
				current.First += enumerator.Current.First;
				current.Second += enumerator.Current.Second;
			}
			return current.First / (double)current.Second;
		}
	}

	protected override QueryOperatorEnumerator<Pair<double, long>, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<double?, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new NullableDoubleAverageAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
