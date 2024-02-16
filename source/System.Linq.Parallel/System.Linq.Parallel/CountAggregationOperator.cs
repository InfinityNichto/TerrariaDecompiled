using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class CountAggregationOperator<TSource> : InlinedAggregationOperator<TSource, int, int>
{
	private sealed class CountAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<int>
	{
		private readonly QueryOperatorEnumerator<TSource, TKey> _source;

		internal CountAggregationOperatorEnumerator(QueryOperatorEnumerator<TSource, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref int currentElement)
		{
			TSource currentElement2 = default(TSource);
			TKey currentKey = default(TKey);
			QueryOperatorEnumerator<TSource, TKey> source = _source;
			if (source.MoveNext(ref currentElement2, ref currentKey))
			{
				int num = 0;
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

	internal CountAggregationOperator(IEnumerable<TSource> child)
		: base(child)
	{
	}

	protected override int InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<int> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		int num = 0;
		while (enumerator.MoveNext())
		{
			num = checked(num + enumerator.Current);
		}
		return num;
	}

	protected override QueryOperatorEnumerator<int, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<TSource, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new CountAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
