using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Auto)]
public struct AsyncIteratorMethodBuilder
{
	private Task<VoidTaskResult> m_task;

	internal object ObjectIdForDebugger => m_task ?? (m_task = AsyncTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox());

	public static AsyncIteratorMethodBuilder Create()
	{
		return default(AsyncIteratorMethodBuilder);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveNext<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		AsyncMethodBuilderCore.Start(ref stateMachine);
	}

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AsyncTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AsyncTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	public void Complete()
	{
		if (m_task == null)
		{
			m_task = Task.s_cachedCompleted;
			return;
		}
		AsyncTaskMethodBuilder<VoidTaskResult>.SetExistingTaskResult(m_task, default(VoidTaskResult));
		if (m_task is IAsyncStateMachineBox asyncStateMachineBox)
		{
			asyncStateMachineBox.ClearStateUponCompletion();
		}
	}
}
