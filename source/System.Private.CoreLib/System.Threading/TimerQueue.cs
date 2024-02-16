using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

[DebuggerDisplay("Count = {CountForDebugger}")]
[DebuggerTypeProxy(typeof(TimerQueueDebuggerTypeProxy))]
internal sealed class TimerQueue
{
	private sealed class AppDomainTimerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public AppDomainTimerSafeHandle()
			: base(ownsHandle: true)
		{
		}

		protected override bool ReleaseHandle()
		{
			return DeleteAppDomainTimer(handle);
		}
	}

	private sealed class TimerQueueDebuggerTypeProxy
	{
		private readonly TimerQueue _queue;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public TimerQueueTimer[] Items => new List<TimerQueueTimer>(_queue.GetTimersForDebugger()).ToArray();

		public TimerQueueDebuggerTypeProxy(TimerQueue queue)
		{
			_queue = queue ?? throw new ArgumentNullException("queue");
		}
	}

	private readonly int _id;

	private AppDomainTimerSafeHandle m_appDomainTimer;

	internal static readonly (long TickCount, DateTime Time) s_tickCountToTimeMap = (TickCount: TickCount64, Time: DateTime.UtcNow);

	private bool _isTimerScheduled;

	private long _currentTimerStartTicks;

	private uint _currentTimerDuration;

	private TimerQueueTimer _shortTimers;

	private TimerQueueTimer _longTimers;

	private long _currentAbsoluteThreshold = TickCount64 + 333;

	public static TimerQueue[] Instances { get; } = CreateTimerQueues();


	private int CountForDebugger
	{
		get
		{
			int num = 0;
			foreach (TimerQueueTimer item in GetTimersForDebugger())
			{
				num++;
			}
			return num;
		}
	}

	public long ActiveCount { get; private set; }

	private static long TickCount64
	{
		get
		{
			if (Environment.IsWindows8OrAbove)
			{
				ulong UnbiasedTime;
				bool flag = Interop.Kernel32.QueryUnbiasedInterruptTime(out UnbiasedTime);
				return (long)(UnbiasedTime / 10000);
			}
			return Environment.TickCount64;
		}
	}

	private TimerQueue(int id)
	{
		_id = id;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool SetTimer(uint actualDuration)
	{
		if (m_appDomainTimer == null || m_appDomainTimer.IsInvalid)
		{
			m_appDomainTimer = CreateAppDomainTimer(actualDuration, _id);
			return !m_appDomainTimer.IsInvalid;
		}
		return ChangeAppDomainTimer(m_appDomainTimer, actualDuration);
	}

	internal static void AppDomainTimerCallback(int id)
	{
		Instances[id].FireNextTimers();
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern AppDomainTimerSafeHandle CreateAppDomainTimer(uint dueTime, int id);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern bool ChangeAppDomainTimer(AppDomainTimerSafeHandle handle, uint dueTime);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern bool DeleteAppDomainTimer(IntPtr handle);

	private static TimerQueue[] CreateTimerQueues()
	{
		TimerQueue[] array = new TimerQueue[Environment.ProcessorCount];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new TimerQueue(i);
		}
		return array;
	}

	internal IEnumerable<TimerQueueTimer> GetTimersForDebugger()
	{
		for (TimerQueueTimer timer2 = _shortTimers; timer2 != null; timer2 = timer2._next)
		{
			yield return timer2;
		}
		for (TimerQueueTimer timer2 = _longTimers; timer2 != null; timer2 = timer2._next)
		{
			yield return timer2;
		}
	}

	private bool EnsureTimerFiresBy(uint requestedDuration)
	{
		uint num = Math.Min(requestedDuration, 268435455u);
		if (_isTimerScheduled)
		{
			long num2 = TickCount64 - _currentTimerStartTicks;
			if (num2 >= _currentTimerDuration)
			{
				return true;
			}
			uint num3 = _currentTimerDuration - (uint)(int)num2;
			if (num >= num3)
			{
				return true;
			}
		}
		if (SetTimer(num))
		{
			_isTimerScheduled = true;
			_currentTimerStartTicks = TickCount64;
			_currentTimerDuration = num;
			return true;
		}
		return false;
	}

	private void FireNextTimers()
	{
		TimerQueueTimer timerQueueTimer = null;
		lock (this)
		{
			_isTimerScheduled = false;
			bool flag = false;
			uint num = uint.MaxValue;
			long tickCount = TickCount64;
			TimerQueueTimer timerQueueTimer2 = _shortTimers;
			for (int i = 0; i < 2; i++)
			{
				while (timerQueueTimer2 != null)
				{
					TimerQueueTimer next = timerQueueTimer2._next;
					long num2 = tickCount - timerQueueTimer2._startTicks;
					long num3 = timerQueueTimer2._dueTime - num2;
					if (num3 <= 0)
					{
						timerQueueTimer2._everQueued = true;
						if (timerQueueTimer2._period != uint.MaxValue)
						{
							timerQueueTimer2._startTicks = tickCount;
							long num4 = num2 - timerQueueTimer2._dueTime;
							timerQueueTimer2._dueTime = ((num4 >= timerQueueTimer2._period) ? 1u : (timerQueueTimer2._period - (uint)(int)num4));
							if (timerQueueTimer2._dueTime < num)
							{
								flag = true;
								num = timerQueueTimer2._dueTime;
							}
							bool flag2 = tickCount + timerQueueTimer2._dueTime - _currentAbsoluteThreshold <= 0;
							if (timerQueueTimer2._short != flag2)
							{
								MoveTimerToCorrectList(timerQueueTimer2, flag2);
							}
						}
						else
						{
							DeleteTimer(timerQueueTimer2);
						}
						if (timerQueueTimer == null)
						{
							timerQueueTimer = timerQueueTimer2;
						}
						else
						{
							ThreadPool.UnsafeQueueUserWorkItemInternal(timerQueueTimer2, preferLocal: false);
						}
					}
					else
					{
						if (num3 < num)
						{
							flag = true;
							num = (uint)num3;
						}
						if (!timerQueueTimer2._short && num3 <= 333)
						{
							MoveTimerToCorrectList(timerQueueTimer2, shortList: true);
						}
					}
					timerQueueTimer2 = next;
				}
				if (i != 0)
				{
					continue;
				}
				long num5 = _currentAbsoluteThreshold - tickCount;
				if (num5 > 0)
				{
					if (_shortTimers == null && _longTimers != null)
					{
						num = (uint)((int)num5 + 1);
						flag = true;
					}
					break;
				}
				timerQueueTimer2 = _longTimers;
				_currentAbsoluteThreshold = tickCount + 333;
			}
			if (flag)
			{
				EnsureTimerFiresBy(num);
			}
		}
		timerQueueTimer?.Fire();
	}

	public bool UpdateTimer(TimerQueueTimer timer, uint dueTime, uint period)
	{
		long tickCount = TickCount64;
		long num = tickCount + dueTime;
		bool flag = _currentAbsoluteThreshold - num >= 0;
		if (timer._dueTime == uint.MaxValue)
		{
			timer._short = flag;
			LinkTimer(timer);
			long activeCount = ActiveCount + 1;
			ActiveCount = activeCount;
		}
		else if (timer._short != flag)
		{
			UnlinkTimer(timer);
			timer._short = flag;
			LinkTimer(timer);
		}
		timer._dueTime = dueTime;
		timer._period = ((period == 0) ? uint.MaxValue : period);
		timer._startTicks = tickCount;
		return EnsureTimerFiresBy(dueTime);
	}

	public void MoveTimerToCorrectList(TimerQueueTimer timer, bool shortList)
	{
		UnlinkTimer(timer);
		timer._short = shortList;
		LinkTimer(timer);
	}

	private void LinkTimer(TimerQueueTimer timer)
	{
		ref TimerQueueTimer reference = ref timer._short ? ref _shortTimers : ref _longTimers;
		timer._next = reference;
		if (timer._next != null)
		{
			timer._next._prev = timer;
		}
		timer._prev = null;
		reference = timer;
	}

	private void UnlinkTimer(TimerQueueTimer timer)
	{
		TimerQueueTimer next = timer._next;
		if (next != null)
		{
			next._prev = timer._prev;
		}
		if (_shortTimers == timer)
		{
			_shortTimers = next;
		}
		else if (_longTimers == timer)
		{
			_longTimers = next;
		}
		next = timer._prev;
		if (next != null)
		{
			next._next = timer._next;
		}
	}

	public void DeleteTimer(TimerQueueTimer timer)
	{
		if (timer._dueTime != uint.MaxValue)
		{
			long activeCount = ActiveCount - 1;
			ActiveCount = activeCount;
			UnlinkTimer(timer);
			timer._prev = null;
			timer._next = null;
			timer._dueTime = uint.MaxValue;
			timer._period = uint.MaxValue;
			timer._startTicks = 0L;
			timer._short = false;
		}
	}
}
