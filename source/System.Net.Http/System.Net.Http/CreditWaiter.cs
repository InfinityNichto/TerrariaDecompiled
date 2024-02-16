using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Http;

internal sealed class CreditWaiter : IValueTaskSource<int>
{
	private CancellationToken _cancellationToken;

	private CancellationTokenRegistration _registration;

	private ManualResetValueTaskSourceCore<int> _source;

	public int Amount;

	public CreditWaiter Next;

	public CreditWaiter(CancellationToken cancellationToken)
	{
		_source.RunContinuationsAsynchronously = true;
		RegisterCancellation(cancellationToken);
	}

	public void ResetForAwait(CancellationToken cancellationToken)
	{
		_source.Reset();
		RegisterCancellation(cancellationToken);
	}

	private void RegisterCancellation(CancellationToken cancellationToken)
	{
		_cancellationToken = cancellationToken;
		_registration = cancellationToken.UnsafeRegister(delegate(object s, CancellationToken cancellationToken)
		{
			((CreditWaiter)s)._source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(cancellationToken)));
		}, this);
	}

	public ValueTask<int> AsValueTask()
	{
		return new ValueTask<int>(this, _source.Version);
	}

	public bool TrySetResult(int result)
	{
		if (UnregisterAndOwnCompletion())
		{
			_source.SetResult(result);
			return true;
		}
		return false;
	}

	public void Dispose()
	{
		if (UnregisterAndOwnCompletion())
		{
			_source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException("CreditManager", System.SR.net_http_disposed_while_in_use)));
		}
	}

	private bool UnregisterAndOwnCompletion()
	{
		if (!_registration.Unregister())
		{
			return !_cancellationToken.CanBeCanceled;
		}
		return true;
	}

	int IValueTaskSource<int>.GetResult(short token)
	{
		return _source.GetResult(token);
	}

	ValueTaskSourceStatus IValueTaskSource<int>.GetStatus(short token)
	{
		return _source.GetStatus(token);
	}

	void IValueTaskSource<int>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
	{
		_source.OnCompleted(continuation, state, token, flags);
	}
}
