using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal sealed class PartitionedStreamMerger<TOutput> : IPartitionedStreamRecipient<TOutput>
{
	private readonly bool _forEffectMerge;

	private readonly ParallelMergeOptions _mergeOptions;

	private readonly bool _isOrdered;

	private MergeExecutor<TOutput> _mergeExecutor;

	private readonly TaskScheduler _taskScheduler;

	private readonly int _queryId;

	private readonly CancellationState _cancellationState;

	internal MergeExecutor<TOutput> MergeExecutor => _mergeExecutor;

	internal PartitionedStreamMerger(bool forEffectMerge, ParallelMergeOptions mergeOptions, TaskScheduler taskScheduler, bool outputOrdered, CancellationState cancellationState, int queryId)
	{
		_forEffectMerge = forEffectMerge;
		_mergeOptions = mergeOptions;
		_isOrdered = outputOrdered;
		_taskScheduler = taskScheduler;
		_cancellationState = cancellationState;
		_queryId = queryId;
	}

	public void Receive<TKey>(PartitionedStream<TOutput, TKey> partitionedStream)
	{
		_mergeExecutor = MergeExecutor<TOutput>.Execute(partitionedStream, _forEffectMerge, _mergeOptions, _taskScheduler, _isOrdered, _cancellationState, _queryId);
	}
}
