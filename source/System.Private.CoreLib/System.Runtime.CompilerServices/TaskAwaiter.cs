using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices;

public readonly struct TaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion, ITaskAwaiter
{
	internal readonly Task m_task;

	public bool IsCompleted => m_task.IsCompleted;

	internal TaskAwaiter(Task task)
	{
		m_task = task;
	}

	public void OnCompleted(Action continuation)
	{
		OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: true);
	}

	public void UnsafeOnCompleted(Action continuation)
	{
		OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: false);
	}

	[StackTraceHidden]
	public void GetResult()
	{
		ValidateEnd(m_task);
	}

	[StackTraceHidden]
	internal static void ValidateEnd(Task task)
	{
		if (task.IsWaitNotificationEnabledOrNotRanToCompletion)
		{
			HandleNonSuccessAndDebuggerNotification(task);
		}
	}

	[StackTraceHidden]
	private static void HandleNonSuccessAndDebuggerNotification(Task task)
	{
		if (!task.IsCompleted)
		{
			bool flag = task.InternalWait(-1, default(CancellationToken));
		}
		task.NotifyDebuggerOfWaitCompletionIfNecessary();
		if (!task.IsCompletedSuccessfully)
		{
			ThrowForNonSuccess(task);
		}
	}

	[StackTraceHidden]
	private static void ThrowForNonSuccess(Task task)
	{
		switch (task.Status)
		{
		case TaskStatus.Canceled:
			task.GetCancellationExceptionDispatchInfo()?.Throw();
			throw new TaskCanceledException(task);
		case TaskStatus.Faulted:
		{
			List<ExceptionDispatchInfo> exceptionDispatchInfos = task.GetExceptionDispatchInfos();
			if (exceptionDispatchInfos.Count > 0)
			{
				exceptionDispatchInfos[0].Throw();
				break;
			}
			throw task.Exception;
		}
		}
	}

	internal static void OnCompletedInternal(Task task, Action continuation, bool continueOnCapturedContext, bool flowExecutionContext)
	{
		if (continuation == null)
		{
			throw new ArgumentNullException("continuation");
		}
		if (TplEventSource.Log.IsEnabled() || Task.s_asyncDebuggingEnabled)
		{
			continuation = OutputWaitEtwEvents(task, continuation);
		}
		task.SetContinuationForAwait(continuation, continueOnCapturedContext, flowExecutionContext);
	}

	internal static void UnsafeOnCompletedInternal(Task task, IAsyncStateMachineBox stateMachineBox, bool continueOnCapturedContext)
	{
		if (TplEventSource.Log.IsEnabled() || Task.s_asyncDebuggingEnabled)
		{
			task.SetContinuationForAwait(OutputWaitEtwEvents(task, stateMachineBox.MoveNextAction), continueOnCapturedContext, flowExecutionContext: false);
		}
		else
		{
			task.UnsafeSetContinuationForAwait(stateMachineBox, continueOnCapturedContext);
		}
	}

	private static Action OutputWaitEtwEvents(Task task, Action continuation)
	{
		if (Task.s_asyncDebuggingEnabled)
		{
			Task.AddToActiveTasks(task);
		}
		TplEventSource log = TplEventSource.Log;
		if (log.IsEnabled())
		{
			Task internalCurrent = Task.InternalCurrent;
			Task task2 = AsyncMethodBuilderCore.TryGetContinuationTask(continuation);
			log.TaskWaitBegin(internalCurrent?.m_taskScheduler.Id ?? TaskScheduler.Default.Id, internalCurrent?.Id ?? 0, task.Id, TplEventSource.TaskWaitBehavior.Asynchronous, task2?.Id ?? 0);
		}
		return AsyncMethodBuilderCore.CreateContinuationWrapper(continuation, delegate(Action innerContinuation, Task innerTask)
		{
			if (Task.s_asyncDebuggingEnabled)
			{
				Task.RemoveFromActiveTasks(innerTask);
			}
			TplEventSource log2 = TplEventSource.Log;
			Guid oldActivityThatWillContinue = default(Guid);
			bool flag = log2.IsEnabled();
			if (flag)
			{
				Task internalCurrent2 = Task.InternalCurrent;
				log2.TaskWaitEnd(internalCurrent2?.m_taskScheduler.Id ?? TaskScheduler.Default.Id, internalCurrent2?.Id ?? 0, innerTask.Id);
				if (log2.TasksSetActivityIds && (innerTask.Options & (TaskCreationOptions)1024) != 0)
				{
					EventSource.SetCurrentThreadActivityId(TplEventSource.CreateGuidForTaskID(innerTask.Id), out oldActivityThatWillContinue);
				}
			}
			innerContinuation();
			if (flag)
			{
				log2.TaskWaitContinuationComplete(innerTask.Id);
				if (log2.TasksSetActivityIds && (innerTask.Options & (TaskCreationOptions)1024) != 0)
				{
					EventSource.SetCurrentThreadActivityId(oldActivityThatWillContinue);
				}
			}
		}, task);
	}
}
public readonly struct TaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion, ITaskAwaiter
{
	private readonly Task<TResult> m_task;

	public bool IsCompleted => m_task.IsCompleted;

	internal TaskAwaiter(Task<TResult> task)
	{
		m_task = task;
	}

	public void OnCompleted(Action continuation)
	{
		TaskAwaiter.OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: true);
	}

	public void UnsafeOnCompleted(Action continuation)
	{
		TaskAwaiter.OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: false);
	}

	[StackTraceHidden]
	public TResult GetResult()
	{
		TaskAwaiter.ValidateEnd(m_task);
		return m_task.ResultOnSuccess;
	}
}
