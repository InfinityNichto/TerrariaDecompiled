using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class IntMinMaxAggregationOperator : InlinedAggregationOperator<int, int, int>
{
	private sealed class IntMinMaxAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<int>
	{
		private readonly QueryOperatorEnumerator<int, TKey> _source;

		private readonly int _sign;

		internal IntMinMaxAggregationOperatorEnumerator(QueryOperatorEnumerator<int, TKey> source, int partitionIndex, int sign, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
			_sign = sign;
		}

		protected override bool MoveNextCore(ref int currentElement)
		{
			QueryOperatorEnumerator<int, TKey> source = _source;
			TKey currentKey = default(TKey);
			if (source.MoveNext(ref currentElement, ref currentKey))
			{
				int num = 0;
				if (_sign == -1)
				{
					int currentElement2 = 0;
					while (source.MoveNext(ref currentElement2, ref currentKey))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						if (currentElement2 < currentElement)
						{
							currentElement = currentElement2;
						}
					}
				}
				else
				{
					int currentElement3 = 0;
					while (source.MoveNext(ref currentElement3, ref currentKey))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						if (currentElement3 > currentElement)
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

	internal IntMinMaxAggregationOperator(IEnumerable<int> child, int sign)
		: base(child)
	{
		_sign = sign;
	}

	protected override int InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<int> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		if (!enumerator.MoveNext())
		{
			singularExceptionToThrow = new InvalidOperationException(System.SR.NoElements);
			return 0;
		}
		int num = enumerator.Current;
		if (_sign == -1)
		{
			while (enumerator.MoveNext())
			{
				int current = enumerator.Current;
				if (current < num)
				{
					num = current;
				}
			}
		}
		else
		{
			while (enumerator.MoveNext())
			{
				int current2 = enumerator.Current;
				if (current2 > num)
				{
					num = current2;
				}
			}
		}
		return num;
	}

	protected override QueryOperatorEnumerator<int, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<int, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new IntMinMaxAggregationOperatorEnumerator<TKey>(source, index, _sign, cancellationToken);
	}
}
