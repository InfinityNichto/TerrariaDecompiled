using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

internal class AwaitTaskContinuation : TaskContinuation, IThreadPoolWorkItem
{
	private readonly ExecutionContext m_capturedContext;

	protected readonly Action m_action;

	protected int m_continuationId;

	private static readonly ContextCallback s_invokeContextCallback = delegate(object state)
	{
		((Action)state)();
	};

	private static readonly Action<Action> s_invokeAction = delegate(Action action)
	{
		action();
	};

	internal static bool IsValidLocationForInlining
	{
		get
		{
			SynchronizationContext current = SynchronizationContext.Current;
			if (current != null && current.GetType() != typeof(SynchronizationContext))
			{
				return false;
			}
			TaskScheduler internalCurrent = TaskScheduler.InternalCurrent;
			if (internalCurrent != null)
			{
				return internalCurrent == TaskScheduler.Default;
			}
			return true;
		}
	}

	internal AwaitTaskContinuation(Action action, bool flowExecutionContext)
	{
		m_action = action;
		if (flowExecutionContext)
		{
			m_capturedContext = ExecutionContext.Capture();
		}
	}

	protected Task CreateTask(Action<object> action, object state, TaskScheduler scheduler)
	{
		return new Task(action, state, null, default(CancellationToken), TaskCreationOptions.None, InternalTaskOptions.QueuedByRuntime, scheduler)
		{
			CapturedContext = m_capturedContext
		};
	}

	internal override void Run(Task task, bool canInlineContinuationTask)
	{
		if (canInlineContinuationTask && IsValidLocationForInlining)
		{
			RunCallback(GetInvokeActionCallback(), m_action, ref Task.t_currentTask);
			return;
		}
		TplEventSource log = TplEventSource.Log;
		if (log.IsEnabled())
		{
			m_continuationId = Task.NewId();
			log.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, m_continuationId);
		}
		ThreadPool.UnsafeQueueUserWorkItemInternal(this, preferLocal: true);
	}

	void IThreadPoolWorkItem.Execute()
	{
		TplEventSource log = TplEventSource.Log;
		ExecutionContext capturedContext = m_capturedContext;
		if (!log.IsEnabled() && capturedContext == null)
		{
			m_action();
			return;
		}
		Guid oldActivityThatWillContinue = default(Guid);
		if (log.IsEnabled() && log.TasksSetActivityIds && m_continuationId != 0)
		{
			Guid activityId = TplEventSource.CreateGuidForTaskID(m_continuationId);
			EventSource.SetCurrentThreadActivityId(activityId, out oldActivityThatWillContinue);
		}
		try
		{
			if (capturedContext == null || capturedContext.IsDefault)
			{
				m_action();
			}
			else
			{
				ExecutionContext.RunForThreadPoolUnsafe(capturedContext, s_invokeAction, in m_action);
			}
		}
		finally
		{
			if (log.IsEnabled() && log.TasksSetActivityIds && m_continuationId != 0)
			{
				EventSource.SetCurrentThreadActivityId(oldActivityThatWillContinue);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static ContextCallback GetInvokeActionCallback()
	{
		return s_invokeContextCallback;
	}

	protected void RunCallback(ContextCallback callback, object state, ref Task currentTask)
	{
		Task task = currentTask;
		try
		{
			if (task != null)
			{
				currentTask = null;
			}
			ExecutionContext capturedContext = m_capturedContext;
			if (capturedContext == null)
			{
				callback(state);
			}
			else
			{
				ExecutionContext.RunInternal(capturedContext, callback, state);
			}
		}
		catch (Exception exception)
		{
			Task.ThrowAsync(exception, null);
		}
		finally
		{
			if (task != null)
			{
				currentTask = task;
			}
		}
	}

	internal static void RunOrScheduleAction(Action action, bool allowInlining)
	{
		Task t_currentTask = Task.t_currentTask;
		if (!allowInlining || !IsValidLocationForInlining)
		{
			UnsafeScheduleAction(action, t_currentTask);
			return;
		}
		try
		{
			if (t_currentTask != null)
			{
				Task.t_currentTask = null;
			}
			action();
		}
		catch (Exception exception)
		{
			Task.ThrowAsync(exception, null);
		}
		finally
		{
			if (t_currentTask != null)
			{
				Task.t_currentTask = t_currentTask;
			}
		}
	}

	internal static void RunOrScheduleAction(IAsyncStateMachineBox box, bool allowInlining)
	{
		Task t_currentTask = Task.t_currentTask;
		if (!allowInlining || !IsValidLocationForInlining)
		{
			if (TplEventSource.Log.IsEnabled())
			{
				UnsafeScheduleAction(box.MoveNextAction, t_currentTask);
			}
			else
			{
				ThreadPool.UnsafeQueueUserWorkItemInternal(box, preferLocal: true);
			}
			return;
		}
		try
		{
			if (t_currentTask != null)
			{
				Task.t_currentTask = null;
			}
			box.MoveNext();
		}
		catch (Exception exception)
		{
			Task.ThrowAsync(exception, null);
		}
		finally
		{
			if (t_currentTask != null)
			{
				Task.t_currentTask = t_currentTask;
			}
		}
	}

	internal static void UnsafeScheduleAction(Action action, Task task)
	{
		AwaitTaskContinuation awaitTaskContinuation = new AwaitTaskContinuation(action, flowExecutionContext: false);
		TplEventSource log = TplEventSource.Log;
		if (log.IsEnabled() && task != null)
		{
			awaitTaskContinuation.m_continuationId = Task.NewId();
			log.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, awaitTaskContinuation.m_continuationId);
		}
		ThreadPool.UnsafeQueueUserWorkItemInternal(awaitTaskContinuation, preferLocal: true);
	}

	internal override Delegate[] GetDelegateContinuationsForDebugger()
	{
		return new Delegate[1] { AsyncMethodBuilderCore.TryGetStateMachineForDebugger(m_action) };
	}
}
