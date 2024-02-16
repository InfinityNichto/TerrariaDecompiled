using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

public struct AsyncTaskMethodBuilder
{
	private Task<VoidTaskResult> m_task;

	public Task Task
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return m_task ?? InitializeTaskAsPromise();
		}
	}

	internal object ObjectIdForDebugger => m_task ?? (m_task = AsyncTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox());

	public static AsyncTaskMethodBuilder Create()
	{
		return default(AsyncTaskMethodBuilder);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[DebuggerStepThrough]
	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		AsyncMethodBuilderCore.Start(ref stateMachine);
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		AsyncMethodBuilderCore.SetStateMachine(stateMachine, null);
	}

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AsyncTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AsyncTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private Task<VoidTaskResult> InitializeTaskAsPromise()
	{
		return m_task = new Task<VoidTaskResult>();
	}

	public void SetResult()
	{
		if (m_task == null)
		{
			m_task = System.Threading.Tasks.Task.s_cachedCompleted;
		}
		else
		{
			AsyncTaskMethodBuilder<VoidTaskResult>.SetExistingTaskResult(m_task, default(VoidTaskResult));
		}
	}

	public void SetException(Exception exception)
	{
		AsyncTaskMethodBuilder<VoidTaskResult>.SetException(exception, ref m_task);
	}

	internal void SetNotificationForWaitCompletion(bool enabled)
	{
		AsyncTaskMethodBuilder<VoidTaskResult>.SetNotificationForWaitCompletion(enabled, ref m_task);
	}
}
public struct AsyncTaskMethodBuilder<TResult>
{
	private sealed class DebugFinalizableAsyncStateMachineBox<TStateMachine> : AsyncStateMachineBox<TStateMachine> where TStateMachine : IAsyncStateMachine
	{
		~DebugFinalizableAsyncStateMachineBox()
		{
			if (!base.IsCompleted)
			{
				TplEventSource.Log.IncompleteAsyncMethod(this);
			}
		}
	}

	private class AsyncStateMachineBox<TStateMachine> : Task<TResult>, IAsyncStateMachineBox where TStateMachine : IAsyncStateMachine
	{
		private static readonly ContextCallback s_callback = ExecutionContextCallback;

		private Action _moveNextAction;

		public TStateMachine StateMachine;

		public ExecutionContext Context;

		public Action MoveNextAction => _moveNextAction ?? (_moveNextAction = MoveNext);

		private static void ExecutionContextCallback(object s)
		{
			Unsafe.As<AsyncStateMachineBox<TStateMachine>>(s).StateMachine.MoveNext();
		}

		internal sealed override void ExecuteFromThreadPool(Thread threadPoolThread)
		{
			MoveNext(threadPoolThread);
		}

		public void MoveNext()
		{
			MoveNext(null);
		}

		private void MoveNext(Thread threadPoolThread)
		{
			bool flag = TplEventSource.Log.IsEnabled();
			if (flag)
			{
				TplEventSource.Log.TraceSynchronousWorkBegin(base.Id, CausalitySynchronousWork.Execution);
			}
			ExecutionContext context = Context;
			if (context == null)
			{
				StateMachine.MoveNext();
			}
			else if (threadPoolThread == null)
			{
				ExecutionContext.RunInternal(context, s_callback, this);
			}
			else
			{
				ExecutionContext.RunFromThreadPoolDispatchLoop(threadPoolThread, context, s_callback, this);
			}
			if (base.IsCompleted)
			{
				ClearStateUponCompletion();
			}
			if (flag)
			{
				TplEventSource.Log.TraceSynchronousWorkEnd(CausalitySynchronousWork.Execution);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClearStateUponCompletion()
		{
			if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
			{
				System.Threading.Tasks.Task.RemoveFromActiveTasks(this);
			}
			StateMachine = default(TStateMachine);
			Context = null;
			if (AsyncMethodBuilderCore.TrackAsyncMethodCompletion)
			{
				GC.SuppressFinalize(this);
			}
		}

		IAsyncStateMachine IAsyncStateMachineBox.GetStateMachineObject()
		{
			return StateMachine;
		}
	}

	private Task<TResult> m_task;

	public Task<TResult> Task
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return m_task ?? InitializeTaskAsPromise();
		}
	}

	internal object ObjectIdForDebugger => m_task ?? (m_task = CreateWeaklyTypedStateMachineBox());

	public static AsyncTaskMethodBuilder<TResult> Create()
	{
		return default(AsyncTaskMethodBuilder<TResult>);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[DebuggerStepThrough]
	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		AsyncMethodBuilderCore.Start(ref stateMachine);
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		AsyncMethodBuilderCore.SetStateMachine(stateMachine, m_task);
	}

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	internal static void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, ref Task<TResult> taskField) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		try
		{
			awaiter.OnCompleted(GetStateMachineBox(ref stateMachine, ref taskField).MoveNextAction);
		}
		catch (Exception exception)
		{
			System.Threading.Tasks.Task.ThrowAsync(exception, null);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, [NotNull] ref Task<TResult> taskField) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		IAsyncStateMachineBox stateMachineBox = GetStateMachineBox(ref stateMachine, ref taskField);
		AwaitUnsafeOnCompleted(ref awaiter, stateMachineBox);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void AwaitUnsafeOnCompleted<TAwaiter>(ref TAwaiter awaiter, IAsyncStateMachineBox box) where TAwaiter : ICriticalNotifyCompletion
	{
		if (default(TAwaiter) != null && awaiter is ITaskAwaiter)
		{
			TaskAwaiter.UnsafeOnCompletedInternal(Unsafe.As<TAwaiter, TaskAwaiter>(ref awaiter).m_task, box, continueOnCapturedContext: true);
			return;
		}
		if (default(TAwaiter) != null && awaiter is IConfiguredTaskAwaiter)
		{
			ref ConfiguredTaskAwaitable.ConfiguredTaskAwaiter reference = ref Unsafe.As<TAwaiter, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter>(ref awaiter);
			TaskAwaiter.UnsafeOnCompletedInternal(reference.m_task, box, reference.m_continueOnCapturedContext);
			return;
		}
		if (default(TAwaiter) != null && awaiter is IStateMachineBoxAwareAwaiter)
		{
			try
			{
				((IStateMachineBoxAwareAwaiter)(object)awaiter).AwaitUnsafeOnCompleted(box);
				return;
			}
			catch (Exception exception)
			{
				System.Threading.Tasks.Task.ThrowAsync(exception, null);
				return;
			}
		}
		try
		{
			awaiter.UnsafeOnCompleted(box.MoveNextAction);
		}
		catch (Exception exception2)
		{
			System.Threading.Tasks.Task.ThrowAsync(exception2, null);
		}
	}

	private static IAsyncStateMachineBox GetStateMachineBox<TStateMachine>(ref TStateMachine stateMachine, [NotNull] ref Task<TResult> taskField) where TStateMachine : IAsyncStateMachine
	{
		ExecutionContext executionContext = ExecutionContext.Capture();
		if (taskField is AsyncStateMachineBox<TStateMachine> asyncStateMachineBox)
		{
			if (asyncStateMachineBox.Context != executionContext)
			{
				asyncStateMachineBox.Context = executionContext;
			}
			return asyncStateMachineBox;
		}
		if (taskField is AsyncStateMachineBox<IAsyncStateMachine> asyncStateMachineBox2)
		{
			if (asyncStateMachineBox2.StateMachine == null)
			{
				Debugger.NotifyOfCrossThreadDependency();
				asyncStateMachineBox2.StateMachine = stateMachine;
			}
			asyncStateMachineBox2.Context = executionContext;
			return asyncStateMachineBox2;
		}
		Debugger.NotifyOfCrossThreadDependency();
		AsyncStateMachineBox<TStateMachine> asyncStateMachineBox3 = (AsyncStateMachineBox<TStateMachine>)(taskField = (AsyncMethodBuilderCore.TrackAsyncMethodCompletion ? CreateDebugFinalizableAsyncStateMachineBox<TStateMachine>() : new AsyncStateMachineBox<TStateMachine>()));
		asyncStateMachineBox3.StateMachine = stateMachine;
		asyncStateMachineBox3.Context = executionContext;
		if (TplEventSource.Log.IsEnabled())
		{
			TplEventSource.Log.TraceOperationBegin(asyncStateMachineBox3.Id, "Async: " + stateMachine.GetType().Name, 0L);
		}
		if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
		{
			System.Threading.Tasks.Task.AddToActiveTasks(asyncStateMachineBox3);
		}
		return asyncStateMachineBox3;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static AsyncStateMachineBox<TStateMachine> CreateDebugFinalizableAsyncStateMachineBox<TStateMachine>() where TStateMachine : IAsyncStateMachine
	{
		return new DebugFinalizableAsyncStateMachineBox<TStateMachine>();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private Task<TResult> InitializeTaskAsPromise()
	{
		return m_task = new Task<TResult>();
	}

	internal static Task<TResult> CreateWeaklyTypedStateMachineBox()
	{
		if (!AsyncMethodBuilderCore.TrackAsyncMethodCompletion)
		{
			return new AsyncStateMachineBox<IAsyncStateMachine>();
		}
		return CreateDebugFinalizableAsyncStateMachineBox<IAsyncStateMachine>();
	}

	public void SetResult(TResult result)
	{
		if (m_task == null)
		{
			m_task = System.Threading.Tasks.Task.FromResult(result);
		}
		else
		{
			SetExistingTaskResult(m_task, result);
		}
	}

	internal static void SetExistingTaskResult(Task<TResult> task, TResult result)
	{
		if (TplEventSource.Log.IsEnabled())
		{
			TplEventSource.Log.TraceOperationEnd(task.Id, AsyncCausalityStatus.Completed);
		}
		if (!task.TrySetResult(result))
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
		}
	}

	public void SetException(Exception exception)
	{
		SetException(exception, ref m_task);
	}

	internal static void SetException(Exception exception, ref Task<TResult> taskField)
	{
		if (exception == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exception);
		}
		Task<TResult> task = taskField ?? (taskField = new Task<TResult>());
		if (!((exception is OperationCanceledException ex) ? task.TrySetCanceled(ex.CancellationToken, ex) : task.TrySetException(exception)))
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
		}
	}

	internal void SetNotificationForWaitCompletion(bool enabled)
	{
		SetNotificationForWaitCompletion(enabled, ref m_task);
	}

	internal static void SetNotificationForWaitCompletion(bool enabled, [NotNull] ref Task<TResult> taskField)
	{
		(taskField ?? (taskField = CreateWeaklyTypedStateMachineBox())).SetNotificationForWaitCompletion(enabled);
	}
}
