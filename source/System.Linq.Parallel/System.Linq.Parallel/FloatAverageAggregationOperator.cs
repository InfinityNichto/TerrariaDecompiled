using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class FloatAverageAggregationOperator : InlinedAggregationOperator<float, Pair<double, long>, float>
{
	private sealed class FloatAverageAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<Pair<double, long>>
	{
		private readonly QueryOperatorEnumerator<float, TKey> _source;

		internal FloatAverageAggregationOperatorEnumerator(QueryOperatorEnumerator<float, TKey> source, int partitionIndex, CancellationToken cancellationToken)
			: base(partitionIndex, cancellationToken)
		{
			_source = source;
		}

		protected override bool MoveNextCore(ref Pair<double, long> currentElement)
		{
			double num = 0.0;
			long num2 = 0L;
			QueryOperatorEnumerator<float, TKey> source = _source;
			float currentElement2 = 0f;
			TKey currentKey = default(TKey);
			if (source.MoveNext(ref currentElement2, ref currentKey))
			{
				int num3 = 0;
				do
				{
					if ((num3++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					num += (double)currentElement2;
					num2 = checked(num2 + 1);
				}
				while (source.MoveNext(ref currentElement2, ref currentKey));
				currentElement = new Pair<double, long>(num, num2);
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	internal FloatAverageAggregationOperator(IEnumerable<float> child)
		: base(child)
	{
	}

	protected override float InternalAggregate(ref Exception singularExceptionToThrow)
	{
		checked
		{
			using IEnumerator<Pair<double, long>> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true);
			if (!enumerator.MoveNext())
			{
				singularExceptionToThrow = new InvalidOperationException(System.SR.NoElements);
				return 0f;
			}
			Pair<double, long> current = enumerator.Current;
			while (enumerator.MoveNext())
			{
				current.First += enumerator.Current.First;
				current.Second += enumerator.Current.Second;
			}
			return (float)(current.First / (double)current.Second);
		}
	}

	protected override QueryOperatorEnumerator<Pair<double, long>, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<float, TKey> source, object sharedData, CancellationToken cancellationToken)
	{
		return new FloatAverageAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
	}
}
