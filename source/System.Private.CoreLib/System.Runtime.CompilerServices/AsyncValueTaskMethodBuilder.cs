using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Auto)]
public struct AsyncValueTaskMethodBuilder
{
	private static readonly Task<VoidTaskResult> s_syncSuccessSentinel = AsyncValueTaskMethodBuilder<VoidTaskResult>.s_syncSuccessSentinel;

	private Task<VoidTaskResult> m_task;

	public ValueTask Task
	{
		get
		{
			if (m_task == s_syncSuccessSentinel)
			{
				return default(ValueTask);
			}
			Task<VoidTaskResult> task = m_task ?? (m_task = new Task<VoidTaskResult>());
			return new ValueTask(task);
		}
	}

	internal object ObjectIdForDebugger => m_task ?? (m_task = AsyncTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox());

	public static AsyncValueTaskMethodBuilder Create()
	{
		return default(AsyncValueTaskMethodBuilder);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		AsyncMethodBuilderCore.Start(ref stateMachine);
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		AsyncMethodBuilderCore.SetStateMachine(stateMachine, null);
	}

	public void SetResult()
	{
		if (m_task == null)
		{
			m_task = s_syncSuccessSentinel;
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

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AsyncTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AsyncTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}
}
[StructLayout(LayoutKind.Auto)]
public struct AsyncValueTaskMethodBuilder<TResult>
{
	internal static readonly Task<TResult> s_syncSuccessSentinel = new Task<TResult>(default(TResult));

	private Task<TResult> m_task;

	private TResult _result;

	public ValueTask<TResult> Task
	{
		get
		{
			if (m_task == s_syncSuccessSentinel)
			{
				return new ValueTask<TResult>(_result);
			}
			Task<TResult> task = m_task ?? (m_task = new Task<TResult>());
			return new ValueTask<TResult>(task);
		}
	}

	internal object ObjectIdForDebugger => m_task ?? (m_task = AsyncTaskMethodBuilder<TResult>.CreateWeaklyTypedStateMachineBox());

	public static AsyncValueTaskMethodBuilder<TResult> Create()
	{
		return default(AsyncValueTaskMethodBuilder<TResult>);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		AsyncMethodBuilderCore.Start(ref stateMachine);
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		AsyncMethodBuilderCore.SetStateMachine(stateMachine, null);
	}

	public void SetResult(TResult result)
	{
		if (m_task == null)
		{
			_result = result;
			m_task = s_syncSuccessSentinel;
		}
		else
		{
			AsyncTaskMethodBuilder<TResult>.SetExistingTaskResult(m_task, result);
		}
	}

	public void SetException(Exception exception)
	{
		AsyncTaskMethodBuilder<TResult>.SetException(exception, ref m_task);
	}

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AsyncTaskMethodBuilder<TResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AsyncTaskMethodBuilder<TResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}
}
