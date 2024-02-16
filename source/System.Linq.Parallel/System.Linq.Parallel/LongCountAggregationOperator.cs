using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class LongCountAggregationOperator<TSource> : InlinedAggregationOperator<TSource, long, long>
{
	private sealed class LongCountAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<long>
	{
		private readonly QueryOperatorEnumerator<TSource, TKey> _source;

		internal LongCountAggregationOperatorEnumerator(QueryOperatorEnumerator<TSource, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref long currentElement)
		{
			TSource currentElement2 = default(TSource);
			TKey currentKey = default(TKey);
			QueryOperatorEnumerator<TSource, TKey> source = _source;
			if (source.MoveNext(ref currentElement2, ref currentKey))
			{
				long num = 0L;
				int num2 = 0;
				do
				{
					if ((num2++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					num = checked(num + 1);
				}
				while (source.MoveNext(ref currentElement2, ref currentKey));
				currentElement = num;
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	internal LongCountAggregationOperator(IEnumerable<TSource> child)
		: base(child)
	{
	}

	protected override long InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<long> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		long num = 0L;
		while (enumerator.MoveNext())
		{
			num = checked(num + enumerator.Current);
		}
		return num;
	}

	protected override QueryOperatorEnumerator<long, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<TSource, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new LongCountAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
