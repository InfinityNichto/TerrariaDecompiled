using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableFloatMinMaxAggregationOperator : InlinedAggregationOperator<float?, float?, float?>
{
	private sealed class NullableFloatMinMaxAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<float?>
	{
		private readonly QueryOperatorEnumerator<float?, TKey> _source;

		private readonly int _sign;

		internal NullableFloatMinMaxAggregationOperatorEnumerator(QueryOperatorEnumerator<float?, TKey> source, int partitionIndex, int sign, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
			_sign = sign;
		}

		protected override bool MoveNextCore(ref float? currentElement)
		{
			QueryOperatorEnumerator<float?, TKey> source = _source;
			TKey currentKey = default(TKey);
			if (source.MoveNext(ref currentElement, ref currentKey))
			{
				int num = 0;
				if (_sign == -1)
				{
					float? currentElement2 = null;
					while (source.MoveNext(ref currentElement2, ref currentKey))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						if (currentElement2.HasValue && (!currentElement.HasValue || currentElement2 < currentElement || float.IsNaN(currentElement2.GetValueOrDefault())))
						{
							currentElement = currentElement2;
						}
					}
				}
				else
				{
					float? currentElement3 = null;
					while (source.MoveNext(ref currentElement3, ref currentKey))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						if (currentElement3.HasValue && (!currentElement.HasValue || currentElement3 > currentElement || float.IsNaN(currentElement.GetValueOrDefault())))
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

	internal NullableFloatMinMaxAggregationOperator(IEnumerable<float?> child, int sign)
		: base(child)
	{
		_sign = sign;
	}

	protected override float? InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<float?> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		if (!enumerator.MoveNext())
		{
			return null;
		}
		float? num = enumerator.Current;
		if (_sign == -1)
		{
			while (enumerator.MoveNext())
			{
				float? current = enumerator.Current;
				if (current.HasValue && (!num.HasValue || current < num || float.IsNaN(current.GetValueOrDefault())))
				{
					num = current;
				}
			}
		}
		else
		{
			while (enumerator.MoveNext())
			{
				float? current2 = enumerator.Current;
				if (current2.HasValue && (!num.HasValue || current2 > num || float.IsNaN(num.GetValueOrDefault())))
				{
					num = current2;
				}
			}
		}
		return num;
	}

	protected override QueryOperatorEnumerator<float?, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<float?, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new NullableFloatMinMaxAggregationOperatorEnumerator<TKey>(source, index, _sign, cancellationToken);
	}
}
