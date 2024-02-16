using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks;

internal sealed class UnwrapPromise<TResult> : Task<TResult>, ITaskCompletionAction
{
	private byte _state;

	private readonly bool _lookForOce;

	public bool InvokeMayRunArbitraryCode => true;

	public UnwrapPromise(Task outerTask, bool lookForOce)
		: base((object)null, outerTask.CreationOptions & TaskCreationOptions.AttachedToParent)
	{
		_lookForOce = lookForOce;
		if (TplEventSource.Log.IsEnabled())
		{
			TplEventSource.Log.TraceOperationBegin(base.Id, "Task.Unwrap", 0L);
		}
		if (Task.s_asyncDebuggingEnabled)
		{
			Task.AddToActiveTasks(this);
		}
		if (outerTask.IsCompleted)
		{
			ProcessCompletedOuterTask(outerTask);
		}
		else
		{
			outerTask.AddCompletionAction(this);
		}
	}

	public void Invoke(Task completingTask)
	{
		if (RuntimeHelpers.TryEnsureSufficientExecutionStack())
		{
			InvokeCore(completingTask);
		}
		else
		{
			InvokeCoreAsync(completingTask);
		}
	}

	private void InvokeCore(Task completingTask)
	{
		switch (_state)
		{
		case 0:
			ProcessCompletedOuterTask(completingTask);
			break;
		case 1:
		{
			bool flag = TrySetFromTask(completingTask, lookForOce: false);
			_state = 2;
			break;
		}
		}
	}

	private void InvokeCoreAsync(Task completingTask)
	{
		ThreadPool.UnsafeQueueUserWorkItem(delegate(object state)
		{
			TupleSlim<UnwrapPromise<TResult>, Task> tupleSlim = (TupleSlim<UnwrapPromise<TResult>, Task>)state;
			tupleSlim.Item1.InvokeCore(tupleSlim.Item2);
		}, new TupleSlim<UnwrapPromise<TResult>, Task>(this, completingTask));
	}

	private void ProcessCompletedOuterTask(Task task)
	{
		_state = 1;
		switch (task.Status)
		{
		case TaskStatus.Canceled:
		case TaskStatus.Faulted:
		{
			bool flag = TrySetFromTask(task, _lookForOce);
			break;
		}
		case TaskStatus.RanToCompletion:
			ProcessInnerTask((task is Task<Task<TResult>> task2) ? task2.Result : ((Task<Task>)task).Result);
			break;
		}
	}

	private bool TrySetFromTask(Task task, bool lookForOce)
	{
		if (TplEventSource.Log.IsEnabled())
		{
			TplEventSource.Log.TraceOperationRelation(base.Id, CausalityRelation.Join);
		}
		bool result = false;
		switch (task.Status)
		{
		case TaskStatus.Canceled:
			result = TrySetCanceled(task.CancellationToken, task.GetCancellationExceptionDispatchInfo());
			break;
		case TaskStatus.Faulted:
		{
			List<ExceptionDispatchInfo> exceptionDispatchInfos = task.GetExceptionDispatchInfos();
			ExceptionDispatchInfo exceptionDispatchInfo;
			result = ((!lookForOce || exceptionDispatchInfos.Count <= 0 || (exceptionDispatchInfo = exceptionDispatchInfos[0]) == null || !(exceptionDispatchInfo.SourceException is OperationCanceledException ex)) ? TrySetException(exceptionDispatchInfos) : TrySetCanceled(ex.CancellationToken, exceptionDispatchInfo));
			break;
		}
		case TaskStatus.RanToCompletion:
			if (TplEventSource.Log.IsEnabled())
			{
				TplEventSource.Log.TraceOperationEnd(base.Id, AsyncCausalityStatus.Completed);
			}
			if (Task.s_asyncDebuggingEnabled)
			{
				Task.RemoveFromActiveTasks(this);
			}
			result = TrySetResult((task is Task<TResult> task2) ? task2.Result : default(TResult));
			break;
		}
		return result;
	}

	private void ProcessInnerTask(Task task)
	{
		if (task == null)
		{
			TrySetCanceled(default(CancellationToken));
			_state = 2;
		}
		else if (task.IsCompleted)
		{
			TrySetFromTask(task, lookForOce: false);
			_state = 2;
		}
		else
		{
			task.AddCompletionAction(this);
		}
	}
}
