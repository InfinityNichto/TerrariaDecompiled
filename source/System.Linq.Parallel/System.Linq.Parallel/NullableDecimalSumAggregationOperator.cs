using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableDecimalSumAggregationOperator : InlinedAggregationOperator<decimal?, decimal?, decimal?>
{
	private sealed class NullableDecimalSumAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<decimal?>
	{
		private readonly QueryOperatorEnumerator<decimal?, TKey> _source;

		internal NullableDecimalSumAggregationOperatorEnumerator(QueryOperatorEnumerator<decimal?, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref decimal? currentElement)
		{
			decimal? currentElement2 = null;
			TKey currentKey = default(TKey);
			QueryOperatorEnumerator<decimal?, TKey> source = _source;
			if (source.MoveNext(ref currentElement2, ref currentKey))
			{
				decimal value = 0.0m;
				int num = 0;
				do
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					value += currentElement2.GetValueOrDefault();
				}
				while (source.MoveNext(ref currentElement2, ref currentKey));
				currentElement = value;
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	internal NullableDecimalSumAggregationOperator(IEnumerable<decimal?> child)
		: base(child)
	{
	}

	protected override decimal? InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<decimal?> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		decimal value = 0.0m;
		while (enumerator.MoveNext())
		{
			value += enumerator.Current.GetValueOrDefault();
		}
		return value;
	}

	protected override QueryOperatorEnumerator<decimal?, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<decimal?, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new NullableDecimalSumAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
