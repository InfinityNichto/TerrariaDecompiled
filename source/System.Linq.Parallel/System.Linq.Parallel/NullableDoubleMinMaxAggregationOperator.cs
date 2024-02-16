using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableDoubleMinMaxAggregationOperator : InlinedAggregationOperator<double?, double?, double?>
{
	private sealed class NullableDoubleMinMaxAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<double?>
	{
		private readonly QueryOperatorEnumerator<double?, TKey> _source;

		private readonly int _sign;

		internal NullableDoubleMinMaxAggregationOperatorEnumerator(QueryOperatorEnumerator<double?, TKey> source, int partitionIndex, int sign, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
			_sign = sign;
		}

		protected override bool MoveNextCore(ref double? currentElement)
		{
			QueryOperatorEnumerator<double?, TKey> source = _source;
			TKey currentKey = default(TKey);
			if (source.MoveNext(ref currentElement, ref currentKey))
			{
				int num = 0;
				if (_sign == -1)
				{
					double? currentElement2 = null;
					while (source.MoveNext(ref currentElement2, ref currentKey))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						if (currentElement2.HasValue && (!currentElement.HasValue || currentElement2 < currentElement || double.IsNaN(currentElement2.GetValueOrDefault())))
						{
							currentElement = currentElement2;
						}
					}
				}
				else
				{
					double? currentElement3 = null;
					while (source.MoveNext(ref currentElement3, ref currentKey))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						if (currentElement3.HasValue && (!currentElement.HasValue || currentElement3 > currentElement || double.IsNaN(currentElement.GetValueOrDefault())))
						{
							currentElement = currentElement3;
						}
					}
				}
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	private readonly int _sign;

	internal NullableDoubleMinMaxAggregationOperator(IEnumerable<double?> child, int sign)
		: base(child)
	{
		_sign = sign;
	}

	protected override double? InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<double?> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		if (!enumerator.MoveNext())
		{
			return null;
		}
		double? num = enumerator.Current;
		if (_sign == -1)
		{
			while (enumerator.MoveNext())
			{
				double? current = enumerator.Current;
				if (current.HasValue && (!num.HasValue || current < num || double.IsNaN(current.GetValueOrDefault())))
				{
					num = current;
				}
			}
		}
		else
		{
			while (enumerator.MoveNext())
			{
				double? current2 = enumerator.Current;
				if (current2.HasValue && (!num.HasValue || current2 > num || double.IsNaN(num.GetValueOrDefault())))
				{
					num = current2;
				}
			}
		}
		return num;
	}

	protected override QueryOperatorEnumerator<double?, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<double?, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new NullableDoubleMinMaxAggregationOperatorEnumerator<TKey>(source, index, _sign, cancellationToken);
	}
}
