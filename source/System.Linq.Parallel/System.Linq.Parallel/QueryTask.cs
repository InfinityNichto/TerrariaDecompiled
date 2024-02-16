using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal abstract class QueryTask
{
	protected int _taskIndex;

	protected QueryTaskGroupState _groupState;

	private static readonly Action<object> s_runTaskSynchronouslyDelegate = RunTaskSynchronously;

	private static readonly Action<object> s_baseWorkDelegate = delegate(object o)
	{
		((QueryTask)o).BaseWork(null);
	};

	protected QueryTask(int taskIndex, QueryTaskGroupState groupState)
	{
		_taskIndex = taskIndex;
		_groupState = groupState;
	}

	private static void RunTaskSynchronously(object o)
	{
		((QueryTask)o).BaseWork(null);
	}

	internal Task RunSynchronously(TaskScheduler taskScheduler)
	{
		Task task = new Task(s_runTaskSynchronouslyDelegate, this, TaskCreationOptions.AttachedToParent);
		task.RunSynchronously(taskScheduler);
		return task;
	}

	internal Task RunAsynchronously(TaskScheduler taskScheduler)
	{
		return Task.Factory.StartNew(s_baseWorkDelegate, this, CancellationToken.None, TaskCreationOptions.PreferFairness | TaskCreationOptions.AttachedToParent, taskScheduler);
	}

	private void BaseWork(object unused)
	{
		PlinqEtwProvider.Log.ParallelQueryFork(_groupState.QueryId);
		try
		{
			Work();
		}
		finally
		{
			PlinqEtwProvider.Log.ParallelQueryJoin(_groupState.QueryId);
		}
	}

	protected abstract void Work();
}
