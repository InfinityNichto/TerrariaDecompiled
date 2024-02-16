using System.Collections.Generic;

namespace System.Threading.Tasks;

internal sealed class ThreadPoolTaskScheduler : TaskScheduler
{
	private static readonly ParameterizedThreadStart s_longRunningThreadWork = delegate(object s)
	{
		((Task)s).ExecuteEntryUnsafe(null);
	};

	internal ThreadPoolTaskScheduler()
	{
		_ = base.Id;
	}

	protected internal override void QueueTask(Task task)
	{
		TaskCreationOptions options = task.Options;
		_ = Thread.IsThreadStartSupported;
		if ((options & TaskCreationOptions.LongRunning) != 0)
		{
			Thread thread = new Thread(s_longRunningThreadWork);
			thread.IsBackground = true;
			thread.Name = ".NET Long Running Task";
			thread.UnsafeStart(task);
		}
		else
		{
			ThreadPool.UnsafeQueueUserWorkItemInternal(task, (options & TaskCreationOptions.PreferFairness) == 0);
		}
	}

	protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
	{
		if (taskWasPreviouslyQueued && !ThreadPool.TryPopCustomWorkItem(task))
		{
			return false;
		}
		try
		{
			task.ExecuteEntryUnsafe(null);
		}
		finally
		{
			if (taskWasPreviouslyQueued)
			{
				NotifyWorkItemProgress();
			}
		}
		return true;
	}

	protected internal override bool TryDequeue(Task task)
	{
		return ThreadPool.TryPopCustomWorkItem(task);
	}

	protected override IEnumerable<Task> GetScheduledTasks()
	{
		return FilterTasksFromWorkItems(ThreadPool.GetQueuedWorkItems());
	}

	private static IEnumerable<Task> FilterTasksFromWorkItems(IEnumerable<object> tpwItems)
	{
		foreach (object tpwItem in tpwItems)
		{
			if (tpwItem is Task task)
			{
				yield return task;
			}
		}
	}

	internal override void NotifyWorkItemProgress()
	{
		ThreadPool.NotifyWorkItemProgress();
	}
}
