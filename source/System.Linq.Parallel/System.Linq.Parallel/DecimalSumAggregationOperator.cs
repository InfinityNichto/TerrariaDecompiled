using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DecimalSumAggregationOperator : InlinedAggregationOperator<decimal, decimal, decimal>
{
	private sealed class DecimalSumAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<decimal>
	{
		private readonly QueryOperatorEnumerator<decimal, TKey> _source;

		internal DecimalSumAggregationOperatorEnumerator(QueryOperatorEnumerator<decimal, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref decimal currentElement)
		{
			decimal currentElement2 = default(decimal);
			TKey currentKey = default(TKey);
			QueryOperatorEnumerator<decimal, TKey> source = _source;
			if (source.MoveNext(ref currentElement2, ref currentKey))
			{
				decimal num = 0.0m;
				int num2 = 0;
				do
				{
					if ((num2++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					num += currentElement2;
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

	internal DecimalSumAggregationOperator(IEnumerable<decimal> child)
		: base(child)
	{
	}

	protected override decimal InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<decimal> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		decimal result = 0.0m;
		while (enumerator.MoveNext())
		{
			result += enumerator.Current;
		}
		return result;
	}

	protected override QueryOperatorEnumerator<decimal, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<decimal, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new DecimalSumAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
