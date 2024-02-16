using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableFloatSumAggregationOperator : InlinedAggregationOperator<float?, double?, float?>
{
	private sealed class NullableFloatSumAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<double?>
	{
		private readonly QueryOperatorEnumerator<float?, TKey> _source;

		internal NullableFloatSumAggregationOperatorEnumerator(QueryOperatorEnumerator<float?, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref double? currentElement)
		{
			float? currentElement2 = null;
			TKey currentKey = default(TKey);
			QueryOperatorEnumerator<float?, TKey> source = _source;
			if (source.MoveNext(ref currentElement2, ref currentKey))
			{
				double num = 0.0;
				int num2 = 0;
				do
				{
					if ((num2++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					num += (double)currentElement2.GetValueOrDefault();
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

	internal NullableFloatSumAggregationOperator(IEnumerable<float?> child)
		: base(child)
	{
	}

	protected override float? InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<double?> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		double num = 0.0;
		while (enumerator.MoveNext())
		{
			num += enumerator.Current.GetValueOrDefault();
		}
		return (float)num;
	}

	protected override QueryOperatorEnumerator<double?, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<float?, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new NullableFloatSumAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
