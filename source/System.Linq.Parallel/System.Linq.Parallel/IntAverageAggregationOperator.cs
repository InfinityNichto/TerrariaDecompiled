using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class IntAverageAggregationOperator : InlinedAggregationOperator<int, Pair<long, long>, double>
{
	private sealed class IntAverageAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<Pair<long, long>>
	{
		private readonly QueryOperatorEnumerator<int, TKey> _source;

		internal IntAverageAggregationOperatorEnumerator(QueryOperatorEnumerator<int, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref Pair<long, long> currentElement)
		{
			long num = 0L;
			long num2 = 0L;
			QueryOperatorEnumerator<int, TKey> source = _source;
			int currentElement2 = 0;
			TKey currentKey = default(TKey);
			if (source.MoveNext(ref currentElement2, ref currentKey))
			{
				int num3 = 0;
				do
				{
					if ((num3++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					checked
					{
						num += currentElement2;
						num2++;
					}
				}
				while (source.MoveNext(ref currentElement2, ref currentKey));
				currentElement = new Pair<long, long>(num, num2);
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	internal IntAverageAggregationOperator(IEnumerable<int> child)
		: base(child)
	{
	}

	protected override double InternalAggregate(ref Exception singularExceptionToThrow)
	{
		checked
		{
			using IEnumerator<Pair<long, long>> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
			if (!enumerator.MoveNext())
			{
				singularExceptionToThrow = new InvalidOperationException(System.SR.NoElements);
				return 0.0;
			}
			Pair<long, long> current = enumerator.Current;
			while (enumerator.MoveNext())
			{
				current.First += enumerator.Current.First;
				current.Second += enumerator.Current.Second;
			}
			return (double)current.First / (double)current.Second;
		}
	}

	protected override QueryOperatorEnumerator<Pair<long, long>, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<int, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new IntAverageAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
