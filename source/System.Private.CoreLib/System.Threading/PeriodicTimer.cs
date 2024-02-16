using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading;

public sealed class PeriodicTimer : IDisposable
{
	private sealed class State : IValueTaskSource<bool>
	{
		private PeriodicTimer _owner;

		private ManualResetValueTaskSourceCore<bool> _mrvtsc;

		private CancellationTokenRegistration _ctr;

		private bool _stopped;

		private bool _signaled;

		private bool _activeWait;

		public ValueTask<bool> WaitForNextTickAsync(PeriodicTimer owner, CancellationToken cancellationToken)
		{
			lock (this)
			{
				if (_activeWait)
				{
					ThrowHelper.ThrowInvalidOperationException();
				}
				if (cancellationToken.IsCancellationRequested)
				{
					return ValueTask.FromCanceled<bool>(cancellationToken);
				}
				if (_signaled)
				{
					if (!_stopped)
					{
						_signaled = false;
					}
					return new ValueTask<bool>(!_stopped);
				}
				_owner = owner;
				_activeWait = true;
				_ctr = cancellationToken.UnsafeRegister(delegate(object state, CancellationToken cancellationToken)
				{
					((State)state).Signal(stopping: false, cancellationToken);
				}, this);
				return new ValueTask<bool>(this, _mrvtsc.Version);
			}
		}

		public void Signal(bool stopping = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			bool flag = false;
			lock (this)
			{
				_stopped |= stopping;
				if (!_signaled)
				{
					_signaled = true;
					flag = _activeWait;
				}
			}
			if (flag)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					_mrvtsc.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(cancellationToken)));
				}
				else
				{
					_mrvtsc.SetResult(result: true);
				}
			}
		}

		bool IValueTaskSource<bool>.GetResult(short token)
		{
			_ctr.Dispose();
			lock (this)
			{
				try
				{
					_mrvtsc.GetResult(token);
				}
				finally
				{
					_mrvtsc.Reset();
					_ctr = default(CancellationTokenRegistration);
					_activeWait = false;
					_owner = null;
					if (!_stopped)
					{
						_signaled = false;
					}
				}
				return !_stopped;
			}
		}

		ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token)
		{
			return _mrvtsc.GetStatus(token);
		}

		void IValueTaskSource<bool>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_mrvtsc.OnCompleted(continuation, state, token, flags);
		}
	}

	private readonly TimerQueueTimer _timer;

	private readonly State _state;

	public PeriodicTimer(TimeSpan period)
	{
		long num = (long)period.TotalMilliseconds;
		if (num < 1 || num > 4294967294u)
		{
			GC.SuppressFinalize(this);
			throw new ArgumentOutOfRangeException("period");
		}
		_state = new State();
		_timer = new TimerQueueTimer(delegate(object s)
		{
			((State)s).Signal();
		}, _state, (uint)num, (uint)num, flowExecutionContext: false);
	}

	public ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return _state.WaitForNextTickAsync(this, cancellationToken);
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		_timer.Close();
		_state.Signal(stopping: true);
	}

	~PeriodicTimer()
	{
		Dispose();
	}
}
