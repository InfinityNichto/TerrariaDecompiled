using System.Collections.Generic;

namespace System.Threading.Tasks;

internal sealed class SynchronizationContextTaskScheduler : TaskScheduler
{
	private readonly SynchronizationContext m_synchronizationContext;

	private static readonly SendOrPostCallback s_postCallback = delegate(object s)
	{
		((Task)s).ExecuteEntry();
	};

	public override int MaximumConcurrencyLevel => 1;

	internal SynchronizationContextTaskScheduler()
	{
		m_synchronizationContext = SynchronizationContext.Current ?? throw new InvalidOperationException(SR.TaskScheduler_FromCurrentSynchronizationContext_NoCurrent);
	}

	protected internal override void QueueTask(Task task)
	{
		m_synchronizationContext.Post(s_postCallback, task);
	}

	protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
	{
		if (SynchronizationContext.Current == m_synchronizationContext)
		{
			return TryExecuteTask(task);
		}
		return false;
	}

	protected override IEnumerable<Task> GetScheduledTasks()
	{
		return null;
	}
}
