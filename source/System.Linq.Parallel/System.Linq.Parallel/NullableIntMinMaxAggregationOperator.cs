using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableIntMinMaxAggregationOperator : InlinedAggregationOperator<int?, int?, int?>
{
	private sealed class NullableIntMinMaxAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<int?>
	{
		private readonly QueryOperatorEnumerator<int?, TKey> _source;

		private readonly int _sign;

		internal NullableIntMinMaxAggregationOperatorEnumerator(QueryOperatorEnumerator<int?, TKey> source, int partitionIndex, int sign, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
			_sign = sign;
		}

		protected override bool MoveNextCore(ref int? currentElement)
		{
			QueryOperatorEnumerator<int?, TKey> source = _source;
			TKey currentKey = default(TKey);
			if (source.MoveNext(ref currentElement, ref currentKey))
			{
				int num = 0;
				if (_sign == -1)
				{
					int? currentElement2 = null;
					while (source.MoveNext(ref currentElement2, ref currentKey))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						if (!currentElement.HasValue || currentElement2 < currentElement)
						{
							currentElement = currentElement2;
						}
					}
				}
				else
				{
					int? currentElement3 = null;
					while (source.MoveNext(ref currentElement3, ref currentKey))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						if (!currentElement.HasValue || currentElement3 > currentElement)
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

	internal NullableIntMinMaxAggregationOperator(IEnumerable<int?> child, int sign)
		: base(child)
	{
		_sign = sign;
	}

	protected override int? InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<int?> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		if (!enumerator.MoveNext())
		{
			return null;
		}
		int? num = enumerator.Current;
		if (_sign == -1)
		{
			while (enumerator.MoveNext())
			{
				int? current = enumerator.Current;
				if (!num.HasValue || current < num)
				{
					num = current;
				}
			}
		}
		else
		{
			while (enumerator.MoveNext())
			{
				int? current2 = enumerator.Current;
				if (!num.HasValue || current2 > num)
				{
					num = current2;
				}
			}
		}
		return num;
	}

	protected override QueryOperatorEnumerator<int?, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<int?, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new NullableIntMinMaxAggregationOperatorEnumerator<TKey>(source, index, _sign, cancellationToken);
	}
}
