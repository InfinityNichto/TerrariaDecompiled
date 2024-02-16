using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class LongMinMaxAggregationOperator : InlinedAggregationOperator<long, long, long>
{
	private sealed class LongMinMaxAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<long>
	{
		private readonly QueryOperatorEnumerator<long, TKey> _source;

		private readonly int _sign;

		internal LongMinMaxAggregationOperatorEnumerator(QueryOperatorEnumerator<long, TKey> source, int partitionIndex, int sign, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
			_sign = sign;
		}

		protected override bool MoveNextCore(ref long currentElement)
		{
			QueryOperatorEnumerator<long, TKey> source = _source;
			TKey currentKey = default(TKey);
			if (source.MoveNext(ref currentElement, ref currentKey))
			{
				int num = 0;
				if (_sign == -1)
				{
					long currentElement2 = 0L;
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
					long currentElement3 = 0L;
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

	internal LongMinMaxAggregationOperator(IEnumerable<long> child, int sign)
		: base(child)
	{
		_sign = sign;
	}

	protected override long InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<long> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		if (!enumerator.MoveNext())
		{
			singularExceptionToThrow = new InvalidOperationException(System.SR.NoElements);
			return 0L;
		}
		long num = enumerator.Current;
		if (_sign == -1)
		{
			while (enumerator.MoveNext())
			{
				long current = enumerator.Current;
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
				long current2 = enumerator.Current;
				if (current2 > num)
				{
					num = current2;
				}
			}
		}
		return num;
	}

	protected override QueryOperatorEnumerator<long, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<long, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new LongMinMaxAggregationOperatorEnumerator<TKey>(source, index, _sign, cancellationToken);
	}
}
