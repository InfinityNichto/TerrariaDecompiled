using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading;

[DebuggerDisplay("IsCancellationRequested = {IsCancellationRequested}")]
public readonly struct CancellationToken
{
	private readonly CancellationTokenSource _source;

	public static CancellationToken None => default(CancellationToken);

	public bool IsCancellationRequested
	{
		get
		{
			if (_source != null)
			{
				return _source.IsCancellationRequested;
			}
			return false;
		}
	}

	public bool CanBeCanceled => _source != null;

	public WaitHandle WaitHandle => (_source ?? CancellationTokenSource.s_neverCanceledSource).WaitHandle;

	internal CancellationToken(CancellationTokenSource source)
	{
		_source = source;
	}

	public CancellationToken(bool canceled)
		: this(canceled ? CancellationTokenSource.s_canceledSource : null)
	{
	}

	public CancellationTokenRegistration Register(Action callback)
	{
		return Register(callback, useSynchronizationContext: false);
	}

	public CancellationTokenRegistration Register(Action callback, bool useSynchronizationContext)
	{
		return Register((Action<object>)delegate(object obj)
		{
			((Action)obj)();
		}, callback ?? throw new ArgumentNullException("callback"), useSynchronizationContext, useExecutionContext: true);
	}

	public CancellationTokenRegistration Register(Action<object?> callback, object? state)
	{
		return Register(callback, state, useSynchronizationContext: false, useExecutionContext: true);
	}

	public CancellationTokenRegistration Register(Action<object?, CancellationToken> callback, object? state)
	{
		return Register(callback, state, useSynchronizationContext: false, useExecutionContext: true);
	}

	public CancellationTokenRegistration Register(Action<object?> callback, object? state, bool useSynchronizationContext)
	{
		return Register(callback, state, useSynchronizationContext, useExecutionContext: true);
	}

	public CancellationTokenRegistration UnsafeRegister(Action<object?> callback, object? state)
	{
		return Register(callback, state, useSynchronizationContext: false, useExecutionContext: false);
	}

	public CancellationTokenRegistration UnsafeRegister(Action<object?, CancellationToken> callback, object? state)
	{
		return Register(callback, state, useSynchronizationContext: false, useExecutionContext: false);
	}

	private CancellationTokenRegistration Register(Delegate callback, object state, bool useSynchronizationContext, bool useExecutionContext)
	{
		if ((object)callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		return _source?.Register(callback, state, useSynchronizationContext ? SynchronizationContext.Current : null, useExecutionContext ? ExecutionContext.Capture() : null) ?? default(CancellationTokenRegistration);
	}

	public bool Equals(CancellationToken other)
	{
		return _source == other._source;
	}

	public override bool Equals([NotNullWhen(true)] object? other)
	{
		if (other is CancellationToken)
		{
			return Equals((CancellationToken)other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (_source ?? CancellationTokenSource.s_neverCanceledSource).GetHashCode();
	}

	public static bool operator ==(CancellationToken left, CancellationToken right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CancellationToken left, CancellationToken right)
	{
		return !left.Equals(right);
	}

	public void ThrowIfCancellationRequested()
	{
		if (IsCancellationRequested)
		{
			ThrowOperationCanceledException();
		}
	}

	[DoesNotReturn]
	private void ThrowOperationCanceledException()
	{
		throw new OperationCanceledException(SR.OperationCanceled, this);
	}
}
