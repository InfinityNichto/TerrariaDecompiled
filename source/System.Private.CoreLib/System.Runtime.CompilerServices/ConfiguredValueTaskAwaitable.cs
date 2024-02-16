using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Auto)]
public readonly struct ConfiguredValueTaskAwaitable
{
	[StructLayout(LayoutKind.Auto)]
	public readonly struct ConfiguredValueTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion, IStateMachineBoxAwareAwaiter
	{
		private readonly ValueTask _value;

		public bool IsCompleted
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return _value.IsCompleted;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ConfiguredValueTaskAwaiter(in ValueTask value)
		{
			_value = value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GetResult()
		{
			_value.ThrowIfCompletedUnsuccessfully();
		}

		public void OnCompleted(Action continuation)
		{
			object obj = _value._obj;
			if (obj is Task task)
			{
				task.ConfigureAwait(_value._continueOnCapturedContext).GetAwaiter().OnCompleted(continuation);
			}
			else if (obj != null)
			{
				Unsafe.As<IValueTaskSource>(obj).OnCompleted(ValueTaskAwaiter.s_invokeActionDelegate, continuation, _value._token, ValueTaskSourceOnCompletedFlags.FlowExecutionContext | (_value._continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None));
			}
			else
			{
				Task.CompletedTask.ConfigureAwait(_value._continueOnCapturedContext).GetAwaiter().OnCompleted(continuation);
			}
		}

		public void UnsafeOnCompleted(Action continuation)
		{
			object obj = _value._obj;
			if (obj is Task task)
			{
				task.ConfigureAwait(_value._continueOnCapturedContext).GetAwaiter().UnsafeOnCompleted(continuation);
			}
			else if (obj != null)
			{
				Unsafe.As<IValueTaskSource>(obj).OnCompleted(ValueTaskAwaiter.s_invokeActionDelegate, continuation, _value._token, _value._continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None);
			}
			else
			{
				Task.CompletedTask.ConfigureAwait(_value._continueOnCapturedContext).GetAwaiter().UnsafeOnCompleted(continuation);
			}
		}

		void IStateMachineBoxAwareAwaiter.AwaitUnsafeOnCompleted(IAsyncStateMachineBox box)
		{
			object obj = _value._obj;
			if (obj is Task task)
			{
				TaskAwaiter.UnsafeOnCompletedInternal(task, box, _value._continueOnCapturedContext);
			}
			else if (obj != null)
			{
				Unsafe.As<IValueTaskSource>(obj).OnCompleted(ThreadPool.s_invokeAsyncStateMachineBox, box, _value._token, _value._continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None);
			}
			else
			{
				TaskAwaiter.UnsafeOnCompletedInternal(Task.CompletedTask, box, _value._continueOnCapturedContext);
			}
		}
	}

	private readonly ValueTask _value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ConfiguredValueTaskAwaitable(in ValueTask value)
	{
		_value = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConfiguredValueTaskAwaiter GetAwaiter()
	{
		return new ConfiguredValueTaskAwaiter(in _value);
	}
}
[StructLayout(LayoutKind.Auto)]
public readonly struct ConfiguredValueTaskAwaitable<TResult>
{
	[StructLayout(LayoutKind.Auto)]
	public readonly struct ConfiguredValueTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion, IStateMachineBoxAwareAwaiter
	{
		private readonly ValueTask<TResult> _value;

		public bool IsCompleted
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return _value.IsCompleted;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ConfiguredValueTaskAwaiter(in ValueTask<TResult> value)
		{
			_value = value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TResult GetResult()
		{
			return _value.Result;
		}

		public void OnCompleted(Action continuation)
		{
			object obj = _value._obj;
			if (obj is Task<TResult> task)
			{
				task.ConfigureAwait(_value._continueOnCapturedContext).GetAwaiter().OnCompleted(continuation);
			}
			else if (obj != null)
			{
				Unsafe.As<IValueTaskSource<TResult>>(obj).OnCompleted(ValueTaskAwaiter.s_invokeActionDelegate, continuation, _value._token, ValueTaskSourceOnCompletedFlags.FlowExecutionContext | (_value._continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None));
			}
			else
			{
				Task.CompletedTask.ConfigureAwait(_value._continueOnCapturedContext).GetAwaiter().OnCompleted(continuation);
			}
		}

		public void UnsafeOnCompleted(Action continuation)
		{
			object obj = _value._obj;
			if (obj is Task<TResult> task)
			{
				task.ConfigureAwait(_value._continueOnCapturedContext).GetAwaiter().UnsafeOnCompleted(continuation);
			}
			else if (obj != null)
			{
				Unsafe.As<IValueTaskSource<TResult>>(obj).OnCompleted(ValueTaskAwaiter.s_invokeActionDelegate, continuation, _value._token, _value._continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None);
			}
			else
			{
				Task.CompletedTask.ConfigureAwait(_value._continueOnCapturedContext).GetAwaiter().UnsafeOnCompleted(continuation);
			}
		}

		void IStateMachineBoxAwareAwaiter.AwaitUnsafeOnCompleted(IAsyncStateMachineBox box)
		{
			object obj = _value._obj;
			if (obj is Task<TResult> task)
			{
				TaskAwaiter.UnsafeOnCompletedInternal(task, box, _value._continueOnCapturedContext);
			}
			else if (obj != null)
			{
				Unsafe.As<IValueTaskSource<TResult>>(obj).OnCompleted(ThreadPool.s_invokeAsyncStateMachineBox, box, _value._token, _value._continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None);
			}
			else
			{
				TaskAwaiter.UnsafeOnCompletedInternal(Task.CompletedTask, box, _value._continueOnCapturedContext);
			}
		}
	}

	private readonly ValueTask<TResult> _value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ConfiguredValueTaskAwaitable(in ValueTask<TResult> value)
	{
		_value = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConfiguredValueTaskAwaiter GetAwaiter()
	{
		return new ConfiguredValueTaskAwaiter(in _value);
	}
}
