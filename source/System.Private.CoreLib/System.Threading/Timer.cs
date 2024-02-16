using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading;

[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(TimerQueueTimer.TimerDebuggerTypeProxy))]
public sealed class Timer : MarshalByRefObject, IDisposable, IAsyncDisposable
{
	internal TimerHolder _timer;

	public static long ActiveCount
	{
		get
		{
			long num = 0L;
			TimerQueue[] instances = TimerQueue.Instances;
			foreach (TimerQueue timerQueue in instances)
			{
				lock (timerQueue)
				{
					num += timerQueue.ActiveCount;
				}
			}
			return num;
		}
	}

	private string DisplayString => _timer._timer.DisplayString;

	private static IEnumerable<TimerQueueTimer> AllTimers
	{
		get
		{
			List<TimerQueueTimer> list = new List<TimerQueueTimer>();
			TimerQueue[] instances = TimerQueue.Instances;
			foreach (TimerQueue timerQueue in instances)
			{
				list.AddRange(timerQueue.GetTimersForDebugger());
			}
			list.Sort((TimerQueueTimer t1, TimerQueueTimer t2) => t1._dueTime.CompareTo(t2._dueTime));
			return list;
		}
	}

	public Timer(TimerCallback callback, object? state, int dueTime, int period)
		: this(callback, state, dueTime, period, flowExecutionContext: true)
	{
	}

	internal Timer(TimerCallback callback, object state, int dueTime, int period, bool flowExecutionContext)
	{
		if (dueTime < -1)
		{
			throw new ArgumentOutOfRangeException("dueTime", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (period < -1)
		{
			throw new ArgumentOutOfRangeException("period", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		TimerSetup(callback, state, (uint)dueTime, (uint)period, flowExecutionContext);
	}

	public Timer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
	{
		long num = (long)dueTime.TotalMilliseconds;
		if (num < -1)
		{
			throw new ArgumentOutOfRangeException("dueTime", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (num > 4294967294u)
		{
			throw new ArgumentOutOfRangeException("dueTime", SR.ArgumentOutOfRange_TimeoutTooLarge);
		}
		long num2 = (long)period.TotalMilliseconds;
		if (num2 < -1)
		{
			throw new ArgumentOutOfRangeException("period", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (num2 > 4294967294u)
		{
			throw new ArgumentOutOfRangeException("period", SR.ArgumentOutOfRange_PeriodTooLarge);
		}
		TimerSetup(callback, state, (uint)num, (uint)num2);
	}

	[CLSCompliant(false)]
	public Timer(TimerCallback callback, object? state, uint dueTime, uint period)
	{
		TimerSetup(callback, state, dueTime, period);
	}

	public Timer(TimerCallback callback, object? state, long dueTime, long period)
	{
		if (dueTime < -1)
		{
			throw new ArgumentOutOfRangeException("dueTime", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (period < -1)
		{
			throw new ArgumentOutOfRangeException("period", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (dueTime > 4294967294u)
		{
			throw new ArgumentOutOfRangeException("dueTime", SR.ArgumentOutOfRange_TimeoutTooLarge);
		}
		if (period > 4294967294u)
		{
			throw new ArgumentOutOfRangeException("period", SR.ArgumentOutOfRange_PeriodTooLarge);
		}
		TimerSetup(callback, state, (uint)dueTime, (uint)period);
	}

	public Timer(TimerCallback callback)
	{
		TimerSetup(callback, this, uint.MaxValue, uint.MaxValue);
	}

	[MemberNotNull("_timer")]
	private void TimerSetup(TimerCallback callback, object state, uint dueTime, uint period, bool flowExecutionContext = true)
	{
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		_timer = new TimerHolder(new TimerQueueTimer(callback, state, dueTime, period, flowExecutionContext));
	}

	public bool Change(int dueTime, int period)
	{
		if (dueTime < -1)
		{
			throw new ArgumentOutOfRangeException("dueTime", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (period < -1)
		{
			throw new ArgumentOutOfRangeException("period", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		return _timer._timer.Change((uint)dueTime, (uint)period);
	}

	public bool Change(TimeSpan dueTime, TimeSpan period)
	{
		return Change((long)dueTime.TotalMilliseconds, (long)period.TotalMilliseconds);
	}

	[CLSCompliant(false)]
	public bool Change(uint dueTime, uint period)
	{
		return _timer._timer.Change(dueTime, period);
	}

	public bool Change(long dueTime, long period)
	{
		if (dueTime < -1)
		{
			throw new ArgumentOutOfRangeException("dueTime", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (period < -1)
		{
			throw new ArgumentOutOfRangeException("period", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (dueTime > 4294967294u)
		{
			throw new ArgumentOutOfRangeException("dueTime", SR.ArgumentOutOfRange_TimeoutTooLarge);
		}
		if (period > 4294967294u)
		{
			throw new ArgumentOutOfRangeException("period", SR.ArgumentOutOfRange_PeriodTooLarge);
		}
		return _timer._timer.Change((uint)dueTime, (uint)period);
	}

	public bool Dispose(WaitHandle notifyObject)
	{
		if (notifyObject == null)
		{
			throw new ArgumentNullException("notifyObject");
		}
		return _timer.Close(notifyObject);
	}

	public void Dispose()
	{
		_timer.Close();
	}

	public ValueTask DisposeAsync()
	{
		return _timer.CloseAsync();
	}
}
