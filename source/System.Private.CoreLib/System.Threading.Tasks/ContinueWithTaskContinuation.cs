namespace System.Threading.Tasks;

internal sealed class ContinueWithTaskContinuation : TaskContinuation
{
	internal Task m_task;

	internal readonly TaskContinuationOptions m_options;

	private readonly TaskScheduler m_taskScheduler;

	internal ContinueWithTaskContinuation(Task task, TaskContinuationOptions options, TaskScheduler scheduler)
	{
		m_task = task;
		m_options = options;
		m_taskScheduler = scheduler;
		if (TplEventSource.Log.IsEnabled())
		{
			TplEventSource.Log.TraceOperationBegin(m_task.Id, "Task.ContinueWith: " + task.m_action.Method.Name, 0L);
		}
		if (Task.s_asyncDebuggingEnabled)
		{
			Task.AddToActiveTasks(m_task);
		}
	}

	internal override void Run(Task completedTask, bool canInlineContinuationTask)
	{
		Task task = m_task;
		m_task = null;
		TaskContinuationOptions options = m_options;
		if (completedTask.IsCompletedSuccessfully ? ((options & TaskContinuationOptions.NotOnRanToCompletion) == 0) : (completedTask.IsCanceled ? ((options & TaskContinuationOptions.NotOnCanceled) == 0) : ((options & TaskContinuationOptions.NotOnFaulted) == 0)))
		{
			if (TplEventSource.Log.IsEnabled() && !task.IsCanceled)
			{
				TplEventSource.Log.TraceOperationRelation(task.Id, CausalityRelation.AssignDelegate);
			}
			task.m_taskScheduler = m_taskScheduler;
			if (!canInlineContinuationTask || (options & TaskContinuationOptions.ExecuteSynchronously) == 0)
			{
				try
				{
					task.ScheduleAndStart(needsProtection: true);
					return;
				}
				catch (TaskSchedulerException)
				{
					return;
				}
			}
			TaskContinuation.InlineIfPossibleOrElseQueue(task, needsProtection: true);
		}
		else
		{
			Task.ContingentProperties contingentProperties = task.m_contingentProperties;
			if (contingentProperties == null || contingentProperties.m_cancellationToken == default(CancellationToken))
			{
				task.InternalCancelContinueWithInitialState();
			}
			else
			{
				task.InternalCancel();
			}
		}
	}

	internal override Delegate[] GetDelegateContinuationsForDebugger()
	{
		if (m_task != null)
		{
			if ((object)m_task.m_action != null)
			{
				return new Delegate[1] { m_task.m_action };
			}
			return m_task.GetDelegateContinuationsForDebugger();
		}
		return null;
	}
}
