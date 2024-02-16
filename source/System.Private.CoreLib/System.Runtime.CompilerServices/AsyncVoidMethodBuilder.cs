using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices;

public struct AsyncVoidMethodBuilder
{
	private SynchronizationContext _synchronizationContext;

	private AsyncTaskMethodBuilder _builder;

	private Task Task => _builder.Task;

	internal object ObjectIdForDebugger => _builder.ObjectIdForDebugger;

	public static AsyncVoidMethodBuilder Create()
	{
		SynchronizationContext current = SynchronizationContext.Current;
		current?.OperationStarted();
		AsyncVoidMethodBuilder result = default(AsyncVoidMethodBuilder);
		result._synchronizationContext = current;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[DebuggerStepThrough]
	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		AsyncMethodBuilderCore.Start(ref stateMachine);
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		_builder.SetStateMachine(stateMachine);
	}

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		_builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
	}

	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		_builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
	}

	public void SetResult()
	{
		if (TplEventSource.Log.IsEnabled())
		{
			TplEventSource.Log.TraceOperationEnd(Task.Id, AsyncCausalityStatus.Completed);
		}
		_builder.SetResult();
		if (_synchronizationContext != null)
		{
			NotifySynchronizationContextOfCompletion();
		}
	}

	public void SetException(Exception exception)
	{
		if (exception == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exception);
		}
		if (TplEventSource.Log.IsEnabled())
		{
			TplEventSource.Log.TraceOperationEnd(Task.Id, AsyncCausalityStatus.Error);
		}
		if (_synchronizationContext != null)
		{
			try
			{
				System.Threading.Tasks.Task.ThrowAsync(exception, _synchronizationContext);
			}
			finally
			{
				NotifySynchronizationContextOfCompletion();
			}
		}
		else
		{
			System.Threading.Tasks.Task.ThrowAsync(exception, null);
		}
		_builder.SetResult();
	}

	private void NotifySynchronizationContextOfCompletion()
	{
		try
		{
			_synchronizationContext.OperationCompleted();
		}
		catch (Exception exception)
		{
			System.Threading.Tasks.Task.ThrowAsync(exception, null);
		}
	}
}
