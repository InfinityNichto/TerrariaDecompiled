using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace System.Threading;

[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(TimerDebuggerTypeProxy))]
internal sealed class TimerQueueTimer : IThreadPoolWorkItem
{
	internal sealed class TimerDebuggerTypeProxy
	{
		private readonly TimerQueueTimer _timer;

		public DateTime? EstimatedNextTimeUtc
		{
			get
			{
				if (_timer._dueTime != uint.MaxValue)
				{
					long num = _timer._startTicks - TimerQueue.s_tickCountToTimeMap.TickCount + _timer._dueTime;
					return TimerQueue.s_tickCountToTimeMap.Time + TimeSpan.FromMilliseconds(num);
				}
				return null;
			}
		}

		public TimeSpan? DueTime
		{
			get
			{
				if (_timer._dueTime != uint.MaxValue)
				{
					return TimeSpan.FromMilliseconds(_timer._dueTime);
				}
				return null;
			}
		}

		public TimeSpan? Period
		{
			get
			{
				if (_timer._period != uint.MaxValue)
				{
					return TimeSpan.FromMilliseconds(_timer._period);
				}
				return null;
			}
		}

		public TimerCallback Callback => _timer._timerCallback;

		public object State => _timer._state;

		public TimerDebuggerTypeProxy(Timer timer)
		{
			_timer = timer._timer._timer;
		}

		public TimerDebuggerTypeProxy(TimerQueueTimer timer)
		{
			_timer = timer;
		}
	}

	private readonly TimerQueue _associatedTimerQueue;

	internal TimerQueueTimer _next;

	internal TimerQueueTimer _prev;

	internal bool _short;

	internal long _startTicks;

	internal uint _dueTime;

	internal uint _period;

	private readonly TimerCallback _timerCallback;

	private readonly object _state;

	private readonly ExecutionContext _executionContext;

	private int _callbacksRunning;

	private bool _canceled;

	internal bool _everQueued;

	private object _notifyWhenNoCallbacksRunning;

	private static readonly ContextCallback s_callCallbackInContext = delegate(object state)
	{
		TimerQueueTimer timerQueueTimer = (TimerQueueTimer)state;
		timerQueueTimer._timerCallback(timerQueueTimer._state);
	};

	internal string DisplayString
	{
		get
		{
			string text = _timerCallback.Method.DeclaringType?.FullName;
			if (text != null)
			{
				text += ".";
			}
			return "DueTime = " + ((_dueTime == uint.MaxValue) ? "(not set)" : ((object)TimeSpan.FromMilliseconds(_dueTime)))?.ToString() + ", Period = " + ((_period == uint.MaxValue) ? "(not set)" : ((object)TimeSpan.FromMilliseconds(_period)))?.ToString() + ", " + text + _timerCallback.Method.Name + "(" + (_state?.ToString() ?? "null") + ")";
		}
	}

	internal TimerQueueTimer(TimerCallback timerCallback, object state, uint dueTime, uint period, bool flowExecutionContext)
	{
		_timerCallback = timerCallback;
		_state = state;
		_dueTime = uint.MaxValue;
		_period = uint.MaxValue;
		if (flowExecutionContext)
		{
			_executionContext = ExecutionContext.Capture();
		}
		_associatedTimerQueue = TimerQueue.Instances[Thread.GetCurrentProcessorId() % TimerQueue.Instances.Length];
		if (dueTime != uint.MaxValue)
		{
			Change(dueTime, period);
		}
	}

	internal bool Change(uint dueTime, uint period)
	{
		lock (_associatedTimerQueue)
		{
			if (_canceled)
			{
				throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);
			}
			_period = period;
			if (dueTime == uint.MaxValue)
			{
				_associatedTimerQueue.DeleteTimer(this);
				return true;
			}
			if (FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, (EventKeywords)16L))
			{
				FrameworkEventSource.Log.ThreadTransferSendObj(this, 1, string.Empty, multiDequeues: true, (int)dueTime, (int)period);
			}
			return _associatedTimerQueue.UpdateTimer(this, dueTime, period);
		}
	}

	public void Close()
	{
		lock (_associatedTimerQueue)
		{
			if (!_canceled)
			{
				_canceled = true;
				_associatedTimerQueue.DeleteTimer(this);
			}
		}
	}

	public bool Close(WaitHandle toSignal)
	{
		bool flag = false;
		bool result;
		lock (_associatedTimerQueue)
		{
			if (_canceled)
			{
				result = false;
			}
			else
			{
				_canceled = true;
				_notifyWhenNoCallbacksRunning = toSignal;
				_associatedTimerQueue.DeleteTimer(this);
				flag = _callbacksRunning == 0;
				result = true;
			}
		}
		if (flag)
		{
			SignalNoCallbacksRunning();
		}
		return result;
	}

	public ValueTask CloseAsync()
	{
		lock (_associatedTimerQueue)
		{
			object notifyWhenNoCallbacksRunning = _notifyWhenNoCallbacksRunning;
			if (_canceled)
			{
				if (notifyWhenNoCallbacksRunning is WaitHandle)
				{
					InvalidOperationException ex = new InvalidOperationException(SR.InvalidOperation_TimerAlreadyClosed);
					ex.SetCurrentStackTrace();
					return ValueTask.FromException(ex);
				}
			}
			else
			{
				_canceled = true;
				_associatedTimerQueue.DeleteTimer(this);
			}
			if (_callbacksRunning == 0)
			{
				return default(ValueTask);
			}
			if (notifyWhenNoCallbacksRunning == null)
			{
				return new ValueTask((Task)(_notifyWhenNoCallbacksRunning = new Task(null, TaskCreationOptions.RunContinuationsAsynchronously, promiseStyle: true)));
			}
			return new ValueTask((Task)notifyWhenNoCallbacksRunning);
		}
	}

	void IThreadPoolWorkItem.Execute()
	{
		Fire(isThreadPool: true);
	}

	internal void Fire(bool isThreadPool = false)
	{
		bool flag = false;
		lock (_associatedTimerQueue)
		{
			flag = _canceled;
			if (!flag)
			{
				_callbacksRunning++;
			}
		}
		if (!flag)
		{
			CallCallback(isThreadPool);
			bool flag2;
			lock (_associatedTimerQueue)
			{
				_callbacksRunning--;
				flag2 = _canceled && _callbacksRunning == 0 && _notifyWhenNoCallbacksRunning != null;
			}
			if (flag2)
			{
				SignalNoCallbacksRunning();
			}
		}
	}

	internal void SignalNoCallbacksRunning()
	{
		object notifyWhenNoCallbacksRunning = _notifyWhenNoCallbacksRunning;
		if (notifyWhenNoCallbacksRunning is WaitHandle waitHandle)
		{
			EventWaitHandle.Set(waitHandle.SafeWaitHandle);
		}
		else
		{
			((Task)notifyWhenNoCallbacksRunning).TrySetResult();
		}
	}

	internal void CallCallback(bool isThreadPool)
	{
		if (FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, (EventKeywords)16L))
		{
			FrameworkEventSource.Log.ThreadTransferReceiveObj(this, 1, string.Empty);
		}
		ExecutionContext executionContext = _executionContext;
		if (executionContext == null)
		{
			_timerCallback(_state);
		}
		else if (isThreadPool)
		{
			ExecutionContext.RunFromThreadPoolDispatchLoop(Thread.CurrentThread, executionContext, s_callCallbackInContext, this);
		}
		else
		{
			ExecutionContext.RunInternal(executionContext, s_callCallbackInContext, this);
		}
	}
}
