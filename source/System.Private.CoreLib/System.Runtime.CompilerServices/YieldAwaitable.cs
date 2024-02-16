using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct YieldAwaitable
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public readonly struct YieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion, IStateMachineBoxAwareAwaiter
	{
		private static readonly WaitCallback s_waitCallbackRunAction = RunAction;

		private static readonly SendOrPostCallback s_sendOrPostCallbackRunAction = RunAction;

		public bool IsCompleted => false;

		public void OnCompleted(Action continuation)
		{
			QueueContinuation(continuation, flowContext: true);
		}

		public void UnsafeOnCompleted(Action continuation)
		{
			QueueContinuation(continuation, flowContext: false);
		}

		private static void QueueContinuation(Action continuation, bool flowContext)
		{
			if (continuation == null)
			{
				throw new ArgumentNullException("continuation");
			}
			if (TplEventSource.Log.IsEnabled())
			{
				continuation = OutputCorrelationEtwEvent(continuation);
			}
			SynchronizationContext current = SynchronizationContext.Current;
			if (current != null && current.GetType() != typeof(SynchronizationContext))
			{
				current.Post(s_sendOrPostCallbackRunAction, continuation);
				return;
			}
			TaskScheduler current2 = TaskScheduler.Current;
			if (current2 == TaskScheduler.Default)
			{
				if (flowContext)
				{
					ThreadPool.QueueUserWorkItem(s_waitCallbackRunAction, continuation);
				}
				else
				{
					ThreadPool.UnsafeQueueUserWorkItem(s_waitCallbackRunAction, continuation);
				}
			}
			else
			{
				Task.Factory.StartNew(continuation, default(CancellationToken), TaskCreationOptions.PreferFairness, current2);
			}
		}

		void IStateMachineBoxAwareAwaiter.AwaitUnsafeOnCompleted(IAsyncStateMachineBox box)
		{
			if (TplEventSource.Log.IsEnabled())
			{
				QueueContinuation(box.MoveNextAction, flowContext: false);
				return;
			}
			SynchronizationContext current = SynchronizationContext.Current;
			if (current != null && current.GetType() != typeof(SynchronizationContext))
			{
				current.Post(delegate(object s)
				{
					((IAsyncStateMachineBox)s).MoveNext();
				}, box);
				return;
			}
			TaskScheduler current2 = TaskScheduler.Current;
			if (current2 == TaskScheduler.Default)
			{
				ThreadPool.UnsafeQueueUserWorkItemInternal(box, preferLocal: false);
				return;
			}
			Task.Factory.StartNew(delegate(object s)
			{
				((IAsyncStateMachineBox)s).MoveNext();
			}, box, default(CancellationToken), TaskCreationOptions.PreferFairness, current2);
		}

		private static Action OutputCorrelationEtwEvent(Action continuation)
		{
			int num = Task.NewId();
			Task internalCurrent = Task.InternalCurrent;
			TplEventSource.Log.AwaitTaskContinuationScheduled(TaskScheduler.Current.Id, internalCurrent?.Id ?? 0, num);
			return AsyncMethodBuilderCore.CreateContinuationWrapper(continuation, delegate(Action innerContinuation, Task continuationIdTask)
			{
				TplEventSource log = TplEventSource.Log;
				log.TaskWaitContinuationStarted(((Task<int>)continuationIdTask).Result);
				Guid oldActivityThatWillContinue = default(Guid);
				if (log.TasksSetActivityIds)
				{
					EventSource.SetCurrentThreadActivityId(TplEventSource.CreateGuidForTaskID(((Task<int>)continuationIdTask).Result), out oldActivityThatWillContinue);
				}
				innerContinuation();
				if (log.TasksSetActivityIds)
				{
					EventSource.SetCurrentThreadActivityId(oldActivityThatWillContinue);
				}
				log.TaskWaitContinuationComplete(((Task<int>)continuationIdTask).Result);
			}, Task.FromResult(num));
		}

		private static void RunAction(object state)
		{
			((Action)state)();
		}

		public void GetResult()
		{
		}
	}

	public YieldAwaiter GetAwaiter()
	{
		return default(YieldAwaiter);
	}
}
