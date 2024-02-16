using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices;

internal static class AsyncMethodBuilderCore
{
	private sealed class ContinuationWrapper
	{
		private readonly Action<Action, Task> _invokeAction;

		internal readonly Action _continuation;

		internal readonly Task _innerTask;

		internal ContinuationWrapper(Action continuation, Action<Action, Task> invokeAction, Task innerTask)
		{
			_invokeAction = invokeAction;
			_continuation = continuation;
			_innerTask = innerTask;
		}

		internal void Invoke()
		{
			_invokeAction(_continuation, _innerTask);
		}
	}

	internal static bool TrackAsyncMethodCompletion
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return TplEventSource.Log.IsEnabled(EventLevel.Warning, (EventKeywords)256L);
		}
	}

	[DebuggerStepThrough]
	public static void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		if (stateMachine == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stateMachine);
		}
		Thread currentThread = Thread.CurrentThread;
		ExecutionContext executionContext = currentThread._executionContext;
		SynchronizationContext synchronizationContext = currentThread._synchronizationContext;
		try
		{
			stateMachine.MoveNext();
		}
		finally
		{
			if (synchronizationContext != currentThread._synchronizationContext)
			{
				currentThread._synchronizationContext = synchronizationContext;
			}
			ExecutionContext executionContext2 = currentThread._executionContext;
			if (executionContext != executionContext2)
			{
				ExecutionContext.RestoreChangedContextToThread(currentThread, executionContext, executionContext2);
			}
		}
	}

	public static void SetStateMachine(IAsyncStateMachine stateMachine, Task task)
	{
		if (stateMachine == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stateMachine);
		}
		if (task != null)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.AsyncMethodBuilder_InstanceNotInitialized);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "It's okay if unused fields disappear from debug views")]
	internal static string GetAsyncStateMachineDescription(IAsyncStateMachine stateMachine)
	{
		Type type = stateMachine.GetType();
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(type.FullName);
		FieldInfo[] array = fields;
		foreach (FieldInfo fieldInfo in array)
		{
			stringBuilder.Append("    ").Append(fieldInfo.Name).Append(": ")
				.Append(fieldInfo.GetValue(stateMachine))
				.AppendLine();
		}
		return stringBuilder.ToString();
	}

	internal static Action CreateContinuationWrapper(Action continuation, Action<Action, Task> invokeAction, Task innerTask)
	{
		return new ContinuationWrapper(continuation, invokeAction, innerTask).Invoke;
	}

	internal static Action TryGetStateMachineForDebugger(Action action)
	{
		object target = action.Target;
		if (!(target is IAsyncStateMachineBox asyncStateMachineBox))
		{
			if (!(target is ContinuationWrapper continuationWrapper))
			{
				return action;
			}
			return TryGetStateMachineForDebugger(continuationWrapper._continuation);
		}
		return asyncStateMachineBox.GetStateMachineObject().MoveNext;
	}

	internal static Task TryGetContinuationTask(Action continuation)
	{
		if (!(continuation.Target is ContinuationWrapper continuationWrapper))
		{
			return continuation.Target as Task;
		}
		return continuationWrapper._innerTask;
	}
}
