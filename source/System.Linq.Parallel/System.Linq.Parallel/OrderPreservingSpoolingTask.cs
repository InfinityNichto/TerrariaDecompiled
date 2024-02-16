using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal sealed class OrderPreservingSpoolingTask<TInputOutput, TKey> : SpoolingTaskBase
{
	private readonly Shared<TInputOutput[]> _results;

	private readonly SortHelper<TInputOutput> _sortHelper;

	private OrderPreservingSpoolingTask(int taskIndex, QueryTaskGroupState groupState, Shared<TInputOutput[]> results, SortHelper<TInputOutput> sortHelper)
		: base(taskIndex, groupState)
	{
		_results = results;
		_sortHelper = sortHelper;
	}

	internal static void Spool(QueryTaskGroupState groupState, PartitionedStream<TInputOutput, TKey> partitions, Shared<TInputOutput[]> results, TaskScheduler taskScheduler)
	{
		int maxToRunInParallel = partitions.PartitionCount - 1;
		SortHelper<TInputOutput, TKey>[] sortHelpers = SortHelper<TInputOutput, TKey>.GenerateSortHelpers(partitions, groupState);
		Task task = new Task(delegate
		{
			for (int j = 0; j < maxToRunInParallel; j++)
			{
				QueryTask queryTask = new OrderPreservingSpoolingTask<TInputOutput, TKey>(j, groupState, results, sortHelpers[j]);
				queryTask.RunAsynchronously(taskScheduler);
			}
			QueryTask queryTask2 = new OrderPreservingSpoolingTask<TInputOutput, TKey>(maxToRunInParallel, groupState, results, sortHelpers[maxToRunInParallel]);
			queryTask2.RunSynchronously(taskScheduler);
		});
		groupState.QueryBegin(task);
		task.RunSynchronously(taskScheduler);
		for (int i = 0; i < sortHelpers.Length; i++)
		{
			sortHelpers[i].Dispose();
		}
		groupState.QueryEnd(userInitiatedDispose: false);
	}

	protected override void SpoolingWork()
	{
		TInputOutput[] value = _sortHelper.Sort();
		if (!_groupState.CancellationState.MergedCancellationToken.IsCancellationRequested && _taskIndex == 0)
		{
			_results.Value = value;
		}
	}
}
