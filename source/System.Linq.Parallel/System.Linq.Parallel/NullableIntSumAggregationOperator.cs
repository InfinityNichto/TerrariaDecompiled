using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableIntSumAggregationOperator : InlinedAggregationOperator<int?, int?, int?>
{
	private sealed class NullableIntSumAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<int?>
	{
		private readonly QueryOperatorEnumerator<int?, TKey> _source;

		internal NullableIntSumAggregationOperatorEnumerator(QueryOperatorEnumerator<int?, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref int? currentElement)
		{
			int? currentElement2 = null;
			TKey currentKey = default(TKey);
			QueryOperatorEnumerator<int?, TKey> source = _source;
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
					num = checked(num + currentElement2.GetValueOrDefault());
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

	internal NullableIntSumAggregationOperator(IEnumerable<int?> child)
		: base(child)
	{
	}

	protected override int? InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<int?> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		int num = 0;
		while (enumerator.MoveNext())
		{
			num = checked(num + enumerator.Current.GetValueOrDefault());
		}
		return num;
	}

	protected override QueryOperatorEnumerator<int?, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<int?, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new NullableIntSumAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
