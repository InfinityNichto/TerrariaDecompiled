using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;
using Internal.Runtime.CompilerServices;

namespace System.Threading.Tasks;

[StructLayout(LayoutKind.Auto)]
[AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder))]
public readonly struct ValueTask : IEquatable<ValueTask>
{
	private sealed class ValueTaskSourceAsTask : Task
	{
		private static readonly Action<object> s_completionAction = delegate(object state)
		{
			if (!(state is ValueTaskSourceAsTask { _source: { } source } valueTaskSourceAsTask))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.state);
				return;
			}
			valueTaskSourceAsTask._source = null;
			ValueTaskSourceStatus status = source.GetStatus(valueTaskSourceAsTask._token);
			try
			{
				source.GetResult(valueTaskSourceAsTask._token);
				valueTaskSourceAsTask.TrySetResult();
			}
			catch (Exception ex)
			{
				if (status == ValueTaskSourceStatus.Canceled)
				{
					if (ex is OperationCanceledException ex2)
					{
						valueTaskSourceAsTask.TrySetCanceled(ex2.CancellationToken, ex2);
					}
					else
					{
						valueTaskSourceAsTask.TrySetCanceled(new CancellationToken(canceled: true));
					}
				}
				else
				{
					valueTaskSourceAsTask.TrySetException(ex);
				}
			}
		};

		private IValueTaskSource _source;

		private readonly short _token;

		internal ValueTaskSourceAsTask(IValueTaskSource source, short token)
		{
			_token = token;
			_source = source;
			source.OnCompleted(s_completionAction, this, token, ValueTaskSourceOnCompletedFlags.None);
		}
	}

	private static volatile Task s_canceledTask;

	internal readonly object _obj;

	internal readonly short _token;

	internal readonly bool _continueOnCapturedContext;

	public static ValueTask CompletedTask => default(ValueTask);

	public bool IsCompleted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			object obj = _obj;
			if (obj == null)
			{
				return true;
			}
			if (obj is Task task)
			{
				return task.IsCompleted;
			}
			return Unsafe.As<IValueTaskSource>(obj).GetStatus(_token) != ValueTaskSourceStatus.Pending;
		}
	}

	public bool IsCompletedSuccessfully
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			object obj = _obj;
			if (obj == null)
			{
				return true;
			}
			if (obj is Task task)
			{
				return task.IsCompletedSuccessfully;
			}
			return Unsafe.As<IValueTaskSource>(obj).GetStatus(_token) == ValueTaskSourceStatus.Succeeded;
		}
	}

	public bool IsFaulted
	{
		get
		{
			object obj = _obj;
			if (obj == null)
			{
				return false;
			}
			if (obj is Task task)
			{
				return task.IsFaulted;
			}
			return Unsafe.As<IValueTaskSource>(obj).GetStatus(_token) == ValueTaskSourceStatus.Faulted;
		}
	}

	public bool IsCanceled
	{
		get
		{
			object obj = _obj;
			if (obj == null)
			{
				return false;
			}
			if (obj is Task task)
			{
				return task.IsCanceled;
			}
			return Unsafe.As<IValueTaskSource>(obj).GetStatus(_token) == ValueTaskSourceStatus.Canceled;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ValueTask(Task task)
	{
		if (task == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.task);
		}
		_obj = task;
		_continueOnCapturedContext = true;
		_token = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ValueTask(IValueTaskSource source, short token)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		_obj = source;
		_token = token;
		_continueOnCapturedContext = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ValueTask(object obj, short token, bool continueOnCapturedContext)
	{
		_obj = obj;
		_token = token;
		_continueOnCapturedContext = continueOnCapturedContext;
	}

	public static ValueTask<TResult> FromResult<TResult>(TResult result)
	{
		return new ValueTask<TResult>(result);
	}

	public static ValueTask FromCanceled(CancellationToken cancellationToken)
	{
		return new ValueTask(Task.FromCanceled(cancellationToken));
	}

	public static ValueTask<TResult> FromCanceled<TResult>(CancellationToken cancellationToken)
	{
		return new ValueTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));
	}

	public static ValueTask FromException(Exception exception)
	{
		return new ValueTask(Task.FromException(exception));
	}

	public static ValueTask<TResult> FromException<TResult>(Exception exception)
	{
		return new ValueTask<TResult>(Task.FromException<TResult>(exception));
	}

	public override int GetHashCode()
	{
		return _obj?.GetHashCode() ?? 0;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ValueTask)
		{
			return Equals((ValueTask)obj);
		}
		return false;
	}

	public bool Equals(ValueTask other)
	{
		if (_obj == other._obj)
		{
			return _token == other._token;
		}
		return false;
	}

	public static bool operator ==(ValueTask left, ValueTask right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ValueTask left, ValueTask right)
	{
		return !left.Equals(right);
	}

	public Task AsTask()
	{
		object obj = _obj;
		object obj2;
		if (obj != null)
		{
			obj2 = obj as Task;
			if (obj2 == null)
			{
				return GetTaskForValueTaskSource(Unsafe.As<IValueTaskSource>(obj));
			}
		}
		else
		{
			obj2 = Task.CompletedTask;
		}
		return (Task)obj2;
	}

	public ValueTask Preserve()
	{
		if (_obj != null)
		{
			return new ValueTask(AsTask());
		}
		return this;
	}

	private Task GetTaskForValueTaskSource(IValueTaskSource t)
	{
		ValueTaskSourceStatus status = t.GetStatus(_token);
		if (status != 0)
		{
			try
			{
				t.GetResult(_token);
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				if (status == ValueTaskSourceStatus.Canceled)
				{
					if (ex is OperationCanceledException ex2)
					{
						Task task = new Task();
						task.TrySetCanceled(ex2.CancellationToken, ex2);
						return task;
					}
					return s_canceledTask ?? (s_canceledTask = Task.FromCanceled(new CancellationToken(canceled: true)));
				}
				return Task.FromException(ex);
			}
		}
		return new ValueTaskSourceAsTask(t, _token);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ThrowIfCompletedUnsuccessfully()
	{
		object obj = _obj;
		if (obj != null)
		{
			if (obj is Task task)
			{
				TaskAwaiter.ValidateEnd(task);
			}
			else
			{
				Unsafe.As<IValueTaskSource>(obj).GetResult(_token);
			}
		}
	}

	public ValueTaskAwaiter GetAwaiter()
	{
		return new ValueTaskAwaiter(in this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConfiguredValueTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
	{
		ValueTask value = new ValueTask(_obj, _token, continueOnCapturedContext);
		return new ConfiguredValueTaskAwaitable(in value);
	}
}
[StructLayout(LayoutKind.Auto)]
[AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder<>))]
public readonly struct ValueTask<TResult> : IEquatable<ValueTask<TResult>>
{
	private sealed class ValueTaskSourceAsTask : Task<TResult>
	{
		private static readonly Action<object> s_completionAction = delegate(object state)
		{
			if (!(state is ValueTaskSourceAsTask { _source: { } source } valueTaskSourceAsTask))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.state);
				return;
			}
			valueTaskSourceAsTask._source = null;
			ValueTaskSourceStatus status = source.GetStatus(valueTaskSourceAsTask._token);
			try
			{
				valueTaskSourceAsTask.TrySetResult(source.GetResult(valueTaskSourceAsTask._token));
			}
			catch (Exception ex)
			{
				if (status == ValueTaskSourceStatus.Canceled)
				{
					if (ex is OperationCanceledException ex2)
					{
						valueTaskSourceAsTask.TrySetCanceled(ex2.CancellationToken, ex2);
					}
					else
					{
						valueTaskSourceAsTask.TrySetCanceled(new CancellationToken(canceled: true));
					}
				}
				else
				{
					valueTaskSourceAsTask.TrySetException(ex);
				}
			}
		};

		private IValueTaskSource<TResult> _source;

		private readonly short _token;

		public ValueTaskSourceAsTask(IValueTaskSource<TResult> source, short token)
		{
			_source = source;
			_token = token;
			source.OnCompleted(s_completionAction, this, token, ValueTaskSourceOnCompletedFlags.None);
		}
	}

	private static volatile Task<TResult> s_canceledTask;

	internal readonly object _obj;

	internal readonly TResult _result;

	internal readonly short _token;

	internal readonly bool _continueOnCapturedContext;

	public bool IsCompleted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			object obj = _obj;
			if (obj == null)
			{
				return true;
			}
			if (obj is Task<TResult> task)
			{
				return task.IsCompleted;
			}
			return Unsafe.As<IValueTaskSource<TResult>>(obj).GetStatus(_token) != ValueTaskSourceStatus.Pending;
		}
	}

	public bool IsCompletedSuccessfully
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			object obj = _obj;
			if (obj == null)
			{
				return true;
			}
			if (obj is Task<TResult> task)
			{
				return task.IsCompletedSuccessfully;
			}
			return Unsafe.As<IValueTaskSource<TResult>>(obj).GetStatus(_token) == ValueTaskSourceStatus.Succeeded;
		}
	}

	public bool IsFaulted
	{
		get
		{
			object obj = _obj;
			if (obj == null)
			{
				return false;
			}
			if (obj is Task<TResult> task)
			{
				return task.IsFaulted;
			}
			return Unsafe.As<IValueTaskSource<TResult>>(obj).GetStatus(_token) == ValueTaskSourceStatus.Faulted;
		}
	}

	public bool IsCanceled
	{
		get
		{
			object obj = _obj;
			if (obj == null)
			{
				return false;
			}
			if (obj is Task<TResult> task)
			{
				return task.IsCanceled;
			}
			return Unsafe.As<IValueTaskSource<TResult>>(obj).GetStatus(_token) == ValueTaskSourceStatus.Canceled;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public TResult Result
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			object obj = _obj;
			if (obj == null)
			{
				return _result;
			}
			if (obj is Task<TResult> task)
			{
				TaskAwaiter.ValidateEnd(task);
				return task.ResultOnSuccess;
			}
			return Unsafe.As<IValueTaskSource<TResult>>(obj).GetResult(_token);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ValueTask(TResult result)
	{
		_result = result;
		_obj = null;
		_continueOnCapturedContext = true;
		_token = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ValueTask(Task<TResult> task)
	{
		if (task == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.task);
		}
		_obj = task;
		_result = default(TResult);
		_continueOnCapturedContext = true;
		_token = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ValueTask(IValueTaskSource<TResult> source, short token)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		_obj = source;
		_token = token;
		_result = default(TResult);
		_continueOnCapturedContext = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ValueTask(object obj, TResult result, short token, bool continueOnCapturedContext)
	{
		_obj = obj;
		_result = result;
		_token = token;
		_continueOnCapturedContext = continueOnCapturedContext;
	}

	public override int GetHashCode()
	{
		if (_obj == null)
		{
			if (_result == null)
			{
				return 0;
			}
			return _result.GetHashCode();
		}
		return _obj.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ValueTask<TResult>)
		{
			return Equals((ValueTask<TResult>)obj);
		}
		return false;
	}

	public bool Equals(ValueTask<TResult> other)
	{
		if (_obj == null && other._obj == null)
		{
			return EqualityComparer<TResult>.Default.Equals(_result, other._result);
		}
		if (_obj == other._obj)
		{
			return _token == other._token;
		}
		return false;
	}

	public static bool operator ==(ValueTask<TResult> left, ValueTask<TResult> right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ValueTask<TResult> left, ValueTask<TResult> right)
	{
		return !left.Equals(right);
	}

	public Task<TResult> AsTask()
	{
		object obj = _obj;
		if (obj == null)
		{
			return Task.FromResult(_result);
		}
		if (obj is Task<TResult> result)
		{
			return result;
		}
		return GetTaskForValueTaskSource(Unsafe.As<IValueTaskSource<TResult>>(obj));
	}

	public ValueTask<TResult> Preserve()
	{
		if (_obj != null)
		{
			return new ValueTask<TResult>(AsTask());
		}
		return this;
	}

	private Task<TResult> GetTaskForValueTaskSource(IValueTaskSource<TResult> t)
	{
		ValueTaskSourceStatus status = t.GetStatus(_token);
		if (status != 0)
		{
			try
			{
				return Task.FromResult(t.GetResult(_token));
			}
			catch (Exception ex)
			{
				if (status == ValueTaskSourceStatus.Canceled)
				{
					if (ex is OperationCanceledException ex2)
					{
						Task<TResult> task = new Task<TResult>();
						task.TrySetCanceled(ex2.CancellationToken, ex2);
						return task;
					}
					return s_canceledTask ?? (s_canceledTask = Task.FromCanceled<TResult>(new CancellationToken(canceled: true)));
				}
				return Task.FromException<TResult>(ex);
			}
		}
		return new ValueTaskSourceAsTask(t, _token);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ValueTaskAwaiter<TResult> GetAwaiter()
	{
		return new ValueTaskAwaiter<TResult>(in this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext)
	{
		ValueTask<TResult> value = new ValueTask<TResult>(_obj, _result, _token, continueOnCapturedContext);
		return new ConfiguredValueTaskAwaitable<TResult>(in value);
	}

	public override string? ToString()
	{
		if (IsCompletedSuccessfully)
		{
			Debugger.NotifyOfCrossThreadDependency();
			TResult result = Result;
			if (result != null)
			{
				return result.ToString();
			}
		}
		return string.Empty;
	}
}
