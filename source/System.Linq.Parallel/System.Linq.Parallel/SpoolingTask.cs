using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal static class SpoolingTask
{
	internal static void SpoolStopAndGo<TInputOutput, TIgnoreKey>(QueryTaskGroupState groupState, PartitionedStream<TInputOutput, TIgnoreKey> partitions, SynchronousChannel<TInputOutput>[] channels, TaskScheduler taskScheduler)
	{
		Task task = new Task(delegate
		{
			int num = partitions.PartitionCount - 1;
			for (int i = 0; i < num; i++)
			{
				QueryTask queryTask = new StopAndGoSpoolingTask<TInputOutput, TIgnoreKey>(i, groupState, partitions[i], channels[i]);
				queryTask.RunAsynchronously(taskScheduler);
			}
			QueryTask queryTask2 = new StopAndGoSpoolingTask<TInputOutput, TIgnoreKey>(num, groupState, partitions[num], channels[num]);
			queryTask2.RunSynchronously(taskScheduler);
		});
		groupState.QueryBegin(task);
		task.RunSynchronously(taskScheduler);
		groupState.QueryEnd(userInitiatedDispose: false);
	}

	internal static void SpoolPipeline<TInputOutput, TIgnoreKey>(QueryTaskGroupState groupState, PartitionedStream<TInputOutput, TIgnoreKey> partitions, AsynchronousChannel<TInputOutput>[] channels, TaskScheduler taskScheduler)
	{
		Task task = new Task(delegate
		{
			for (int i = 0; i < partitions.PartitionCount; i++)
			{
				QueryTask queryTask = new PipelineSpoolingTask<TInputOutput, TIgnoreKey>(i, groupState, partitions[i], channels[i]);
				queryTask.RunAsynchronously(taskScheduler);
			}
		});
		groupState.QueryBegin(task);
		task.Start(taskScheduler);
	}

	internal static void SpoolForAll<TInputOutput, TIgnoreKey>(QueryTaskGroupState groupState, PartitionedStream<TInputOutput, TIgnoreKey> partitions, TaskScheduler taskScheduler)
	{
		Task task = new Task(delegate
		{
			int num = partitions.PartitionCount - 1;
			for (int i = 0; i < num; i++)
			{
				QueryTask queryTask = new ForAllSpoolingTask<TInputOutput, TIgnoreKey>(i, groupState, partitions[i]);
				queryTask.RunAsynchronously(taskScheduler);
			}
			QueryTask queryTask2 = new ForAllSpoolingTask<TInputOutput, TIgnoreKey>(num, groupState, partitions[num]);
			queryTask2.RunSynchronously(taskScheduler);
		});
		groupState.QueryBegin(task);
		task.RunSynchronously(taskScheduler);
		groupState.QueryEnd(userInitiatedDispose: false);
	}
}
