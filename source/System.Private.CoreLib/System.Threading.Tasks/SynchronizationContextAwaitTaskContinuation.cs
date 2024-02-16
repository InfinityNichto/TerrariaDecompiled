using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

internal sealed class SynchronizationContextAwaitTaskContinuation : AwaitTaskContinuation
{
	private static readonly SendOrPostCallback s_postCallback = delegate(object state)
	{
		((Action)state)();
	};

	private static ContextCallback s_postActionCallback;

	private readonly SynchronizationContext m_syncContext;

	internal SynchronizationContextAwaitTaskContinuation(SynchronizationContext context, Action action, bool flowExecutionContext)
		: base(action, flowExecutionContext)
	{
		m_syncContext = context;
	}

	internal sealed override void Run(Task task, bool canInlineContinuationTask)
	{
		if (canInlineContinuationTask && m_syncContext == SynchronizationContext.Current)
		{
			RunCallback(AwaitTaskContinuation.GetInvokeActionCallback(), m_action, ref Task.t_currentTask);
			return;
		}
		TplEventSource log = TplEventSource.Log;
		if (log.IsEnabled())
		{
			m_continuationId = Task.NewId();
			log.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, m_continuationId);
		}
		RunCallback(GetPostActionCallback(), this, ref Task.t_currentTask);
	}

	private static void PostAction(object state)
	{
		SynchronizationContextAwaitTaskContinuation synchronizationContextAwaitTaskContinuation = (SynchronizationContextAwaitTaskContinuation)state;
		TplEventSource log = TplEventSource.Log;
		if (log.IsEnabled() && log.TasksSetActivityIds && synchronizationContextAwaitTaskContinuation.m_continuationId != 0)
		{
			synchronizationContextAwaitTaskContinuation.m_syncContext.Post(s_postCallback, GetActionLogDelegate(synchronizationContextAwaitTaskContinuation.m_continuationId, synchronizationContextAwaitTaskContinuation.m_action));
		}
		else
		{
			synchronizationContextAwaitTaskContinuation.m_syncContext.Post(s_postCallback, synchronizationContextAwaitTaskContinuation.m_action);
		}
	}

	private static Action GetActionLogDelegate(int continuationId, Action action)
	{
		return delegate
		{
			Guid activityId = TplEventSource.CreateGuidForTaskID(continuationId);
			EventSource.SetCurrentThreadActivityId(activityId, out var oldActivityThatWillContinue);
			try
			{
				action();
			}
			finally
			{
				EventSource.SetCurrentThreadActivityId(oldActivityThatWillContinue);
			}
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ContextCallback GetPostActionCallback()
	{
		return PostAction;
	}
}
