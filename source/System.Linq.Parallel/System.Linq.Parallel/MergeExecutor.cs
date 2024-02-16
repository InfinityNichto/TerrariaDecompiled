using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal sealed class MergeExecutor<TInputOutput> : IEnumerable<TInputOutput>, IEnumerable
{
	private IMergeHelper<TInputOutput> _mergeHelper;

	private MergeExecutor()
	{
	}

	internal static MergeExecutor<TInputOutput> Execute<TKey>(PartitionedStream<TInputOutput, TKey> partitions, bool ignoreOutput, ParallelMergeOptions options, TaskScheduler taskScheduler, bool isOrdered, CancellationState cancellationState, int queryId)
	{
		MergeExecutor<TInputOutput> mergeExecutor = new MergeExecutor<TInputOutput>();
		if (isOrdered && !ignoreOutput)
		{
			if (options != ParallelMergeOptions.FullyBuffered && !partitions.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing))
			{
				bool autoBuffered = options == ParallelMergeOptions.AutoBuffered;
				if (partitions.PartitionCount > 1)
				{
					mergeExecutor._mergeHelper = new OrderPreservingPipeliningMergeHelper<TInputOutput, TKey>(partitions, taskScheduler, cancellationState, autoBuffered, queryId, partitions.KeyComparer);
				}
				else
				{
					mergeExecutor._mergeHelper = new DefaultMergeHelper<TInputOutput, TKey>(partitions, ignoreOutput: false, options, taskScheduler, cancellationState, queryId);
				}
			}
			else
			{
				mergeExecutor._mergeHelper = new OrderPreservingMergeHelper<TInputOutput, TKey>(partitions, taskScheduler, cancellationState, queryId);
			}
		}
		else
		{
			mergeExecutor._mergeHelper = new DefaultMergeHelper<TInputOutput, TKey>(partitions, ignoreOutput, options, taskScheduler, cancellationState, queryId);
		}
		mergeExecutor.Execute();
		return mergeExecutor;
	}

	private void Execute()
	{
		_mergeHelper.Execute();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IEnumerator<TInputOutput> GetEnumerator()
	{
		return _mergeHelper.GetEnumerator();
	}

	internal TInputOutput[] GetResultsAsArray()
	{
		return _mergeHelper.GetResultsAsArray();
	}

	internal static AsynchronousChannel<TInputOutput>[] MakeAsynchronousChannels(int partitionCount, ParallelMergeOptions options, IntValueEvent consumerEvent, CancellationToken cancellationToken)
	{
		AsynchronousChannel<TInputOutput>[] array = new AsynchronousChannel<TInputOutput>[partitionCount];
		int chunkSize = 0;
		if (options == ParallelMergeOptions.NotBuffered)
		{
			chunkSize = 1;
		}
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new AsynchronousChannel<TInputOutput>(i, chunkSize, cancellationToken, consumerEvent);
		}
		return array;
	}

	internal static SynchronousChannel<TInputOutput>[] MakeSynchronousChannels(int partitionCount)
	{
		SynchronousChannel<TInputOutput>[] array = new SynchronousChannel<TInputOutput>[partitionCount];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new SynchronousChannel<TInputOutput>();
		}
		return array;
	}
}
