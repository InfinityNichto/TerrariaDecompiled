using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal static class ExchangeUtilities
{
	internal static PartitionedStream<T, int> PartitionDataSource<T>(IEnumerable<T> source, int partitionCount, bool useStriping)
	{
		if (source is IParallelPartitionable<T> parallelPartitionable)
		{
			QueryOperatorEnumerator<T, int>[] partitions = parallelPartitionable.GetPartitions(partitionCount);
			if (partitions == null)
			{
				throw new InvalidOperationException(System.SR.ParallelPartitionable_NullReturn);
			}
			if (partitions.Length != partitionCount)
			{
				throw new InvalidOperationException(System.SR.ParallelPartitionable_IncorretElementCount);
			}
			PartitionedStream<T, int> partitionedStream = new PartitionedStream<T, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
			for (int i = 0; i < partitionCount; i++)
			{
				QueryOperatorEnumerator<T, int> queryOperatorEnumerator = partitions[i];
				if (queryOperatorEnumerator == null)
				{
					throw new InvalidOperationException(System.SR.ParallelPartitionable_NullElement);
				}
				partitionedStream[i] = queryOperatorEnumerator;
			}
			return partitionedStream;
		}
		return new PartitionedDataSource<T>(source, partitionCount, useStriping);
	}

	internal static PartitionedStream<Pair<TElement, THashKey>, int> HashRepartition<TElement, THashKey, TIgnoreKey>(PartitionedStream<TElement, TIgnoreKey> source, Func<TElement, THashKey> keySelector, IEqualityComparer<THashKey> keyComparer, IEqualityComparer<TElement> elementComparer, CancellationToken cancellationToken)
	{
		return new UnorderedHashRepartitionStream<TElement, THashKey, TIgnoreKey>(source, keySelector, keyComparer, elementComparer, cancellationToken);
	}

	internal static PartitionedStream<Pair<TElement, THashKey>, TOrderKey> HashRepartitionOrdered<TElement, THashKey, TOrderKey>(PartitionedStream<TElement, TOrderKey> source, Func<TElement, THashKey> keySelector, IEqualityComparer<THashKey> keyComparer, IEqualityComparer<TElement> elementComparer, CancellationToken cancellationToken)
	{
		return new OrderedHashRepartitionStream<TElement, THashKey, TOrderKey>(source, keySelector, keyComparer, elementComparer, cancellationToken);
	}

	internal static OrdinalIndexState Worse(this OrdinalIndexState state1, OrdinalIndexState state2)
	{
		if ((int)state1 <= (int)state2)
		{
			return state2;
		}
		return state1;
	}

	internal static bool IsWorseThan(this OrdinalIndexState state1, OrdinalIndexState state2)
	{
		return (int)state1 > (int)state2;
	}
}
