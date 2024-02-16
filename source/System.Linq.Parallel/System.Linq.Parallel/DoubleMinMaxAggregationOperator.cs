using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DoubleMinMaxAggregationOperator : InlinedAggregationOperator<double, double, double>
{
	private sealed class DoubleMinMaxAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<double>
	{
		private readonly QueryOperatorEnumerator<double, TKey> _source;

		private readonly int _sign;

		internal DoubleMinMaxAggregationOperatorEnumerator(QueryOperatorEnumerator<double, TKey> source, int partitionIndex, int sign, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
			_sign = sign;
		}

		protected override bool MoveNextCore(ref double currentElement)
		{
			QueryOperatorEnumerator<double, TKey> source = _source;
			TKey currentKey = default(TKey);
			if (source.MoveNext(ref currentElement, ref currentKey))
			{
				int num = 0;
				if (_sign == -1)
				{
					double currentElement2 = 0.0;
					while (source.MoveNext(ref currentElement2, ref currentKey))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						if (currentElement2 < currentElement || double.IsNaN(currentElement2))
						{
							currentElement = currentElement2;
						}
					}
				}
				else
				{
					double currentElement3 = 0.0;
					while (source.MoveNext(ref currentElement3, ref currentKey))
					{
						if ((num++ & 0x3F) == 0)
						{
							_cancellationToken.ThrowIfCancellationRequested();
						}
						if (currentElement3 > currentElement || double.IsNaN(currentElement))
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

	internal DoubleMinMaxAggregationOperator(IEnumerable<double> child, int sign)
		: base(child)
	{
		_sign = sign;
	}

	protected override double InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<double> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		if (!enumerator.MoveNext())
		{
			singularExceptionToThrow = new InvalidOperationException(System.SR.NoElements);
			return 0.0;
		}
		double num = enumerator.Current;
		if (_sign == -1)
		{
			while (enumerator.MoveNext())
			{
				double current = enumerator.Current;
				if (current < num || double.IsNaN(current))
				{
					num = current;
				}
			}
		}
		else
		{
			while (enumerator.MoveNext())
			{
				double current2 = enumerator.Current;
				if (current2 > num || double.IsNaN(num))
				{
					num = current2;
				}
			}
		}
		return num;
	}

	protected override QueryOperatorEnumerator<double, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<double, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new DoubleMinMaxAggregationOperatorEnumerator<TKey>(source, index, _sign, cancellationToken);
	}
}
