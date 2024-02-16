using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DoubleSumAggregationOperator : InlinedAggregationOperator<double, double, double>
{
	private sealed class DoubleSumAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<double>
	{
		private readonly QueryOperatorEnumerator<double, TKey> _source;

		internal DoubleSumAggregationOperatorEnumerator(QueryOperatorEnumerator<double, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref double currentElement)
		{
			double currentElement2 = 0.0;
			TKey currentKey = default(TKey);
			QueryOperatorEnumerator<double, TKey> source = _source;
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

	internal DoubleSumAggregationOperator(IEnumerable<double> child)
		: base(child)
	{
	}

	protected override double InternalAggregate(ref Exception singularExceptionToThrow)
	{
		using IEnumerator<double> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
		double num = 0.0;
		while (enumerator.MoveNext())
		{
			num += enumerator.Current;
		}
		return num;
	}

	protected override QueryOperatorEnumerator<double, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<double, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new DoubleSumAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
