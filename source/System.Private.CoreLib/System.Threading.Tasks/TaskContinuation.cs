namespace System.Threading.Tasks;

internal abstract class TaskContinuation
{
	internal abstract void Run(Task completedTask, bool canInlineContinuationTask);

	protected static void InlineIfPossibleOrElseQueue(Task task, bool needsProtection)
	{
		if (needsProtection)
		{
			if (!task.MarkStarted())
			{
				return;
			}
		}
		else
		{
			task.m_stateFlags |= 65536;
		}
		try
		{
			if (!task.m_taskScheduler.TryRunInline(task, taskWasPreviouslyQueued: false))
			{
				task.m_taskScheduler.InternalQueueTask(task);
			}
		}
		catch (Exception innerException)
		{
			TaskSchedulerException exceptionObject = new TaskSchedulerException(innerException);
			task.AddException(exceptionObject);
			task.Finish(userDelegateExecute: false);
		}
	}

	internal abstract Delegate[] GetDelegateContinuationsForDebugger();
}
