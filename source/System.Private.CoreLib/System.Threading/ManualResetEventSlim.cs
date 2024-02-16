using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Threading;

[DebuggerDisplay("Set = {IsSet}")]
public class ManualResetEventSlim : IDisposable
{
	private volatile object m_lock;

	private volatile ManualResetEvent m_eventObj;

	private volatile int m_combinedState;

	private static readonly Action<object> s_cancellationTokenCallback = CancellationTokenCallback;

	public WaitHandle WaitHandle
	{
		get
		{
			ThrowIfDisposed();
			if (m_eventObj == null)
			{
				LazyInitializeEvent();
			}
			return m_eventObj;
		}
	}

	public bool IsSet
	{
		get
		{
			return ExtractStatePortion(m_combinedState, int.MinValue) != 0;
		}
		private set
		{
			UpdateStateAtomically((value ? 1 : 0) << 31, int.MinValue);
		}
	}

	public int SpinCount
	{
		get
		{
			return ExtractStatePortionAndShiftRight(m_combinedState, 1073217536, 19);
		}
		private set
		{
			m_combinedState = (m_combinedState & -1073217537) | (value << 19);
		}
	}

	private int Waiters
	{
		get
		{
			return ExtractStatePortionAndShiftRight(m_combinedState, 524287, 0);
		}
		set
		{
			if (value >= 524287)
			{
				throw new InvalidOperationException(SR.Format(SR.ManualResetEventSlim_ctor_TooManyWaiters, 524287));
			}
			UpdateStateAtomically(value, 524287);
		}
	}

	public ManualResetEventSlim()
		: this(initialState: false)
	{
	}

	public ManualResetEventSlim(bool initialState)
	{
		Initialize(initialState, SpinWait.SpinCountforSpinBeforeWait);
	}

	public ManualResetEventSlim(bool initialState, int spinCount)
	{
		if (spinCount < 0)
		{
			throw new ArgumentOutOfRangeException("spinCount");
		}
		if (spinCount > 2047)
		{
			throw new ArgumentOutOfRangeException("spinCount", SR.Format(SR.ManualResetEventSlim_ctor_SpinCountOutOfRange, 2047));
		}
		Initialize(initialState, spinCount);
	}

	private void Initialize(bool initialState, int spinCount)
	{
		m_combinedState = (initialState ? int.MinValue : 0);
		SpinCount = (Environment.IsSingleProcessor ? 1 : spinCount);
	}

	private void EnsureLockObjectCreated()
	{
		if (m_lock == null)
		{
			object value = new object();
			Interlocked.CompareExchange(ref m_lock, value, null);
		}
	}

	private void LazyInitializeEvent()
	{
		bool isSet = IsSet;
		ManualResetEvent manualResetEvent = new ManualResetEvent(isSet);
		if (Interlocked.CompareExchange(ref m_eventObj, manualResetEvent, null) != null)
		{
			manualResetEvent.Dispose();
			return;
		}
		bool isSet2 = IsSet;
		if (isSet2 == isSet)
		{
			return;
		}
		lock (manualResetEvent)
		{
			if (m_eventObj == manualResetEvent)
			{
				manualResetEvent.Set();
			}
		}
	}

	public void Set()
	{
		Set(duringCancellation: false);
	}

	private void Set(bool duringCancellation)
	{
		IsSet = true;
		if (Waiters > 0)
		{
			lock (m_lock)
			{
				Monitor.PulseAll(m_lock);
			}
		}
		ManualResetEvent eventObj = m_eventObj;
		if (eventObj == null || duringCancellation)
		{
			return;
		}
		lock (eventObj)
		{
			if (m_eventObj != null)
			{
				m_eventObj.Set();
			}
		}
	}

	public void Reset()
	{
		ThrowIfDisposed();
		if (m_eventObj != null)
		{
			m_eventObj.Reset();
		}
		IsSet = false;
	}

	[UnsupportedOSPlatform("browser")]
	public void Wait()
	{
		Wait(-1, CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public void Wait(CancellationToken cancellationToken)
	{
		Wait(-1, cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(TimeSpan timeout)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1 || num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("timeout");
		}
		return Wait((int)num, CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1 || num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("timeout");
		}
		return Wait((int)num, cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(int millisecondsTimeout)
	{
		return Wait(millisecondsTimeout, CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		cancellationToken.ThrowIfCancellationRequested();
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout");
		}
		if (!IsSet)
		{
			if (millisecondsTimeout == 0)
			{
				return false;
			}
			uint startTime = 0u;
			bool flag = false;
			int num = millisecondsTimeout;
			if (millisecondsTimeout != -1)
			{
				startTime = TimeoutHelper.GetTime();
				flag = true;
			}
			int spinCount = SpinCount;
			SpinWait spinWait = default(SpinWait);
			while (spinWait.Count < spinCount)
			{
				spinWait.SpinOnce(-1);
				if (IsSet)
				{
					return true;
				}
				if (spinWait.Count >= 100 && spinWait.Count % 10 == 0)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
			}
			EnsureLockObjectCreated();
			using (cancellationToken.UnsafeRegister(s_cancellationTokenCallback, this))
			{
				lock (m_lock)
				{
					while (!IsSet)
					{
						cancellationToken.ThrowIfCancellationRequested();
						if (flag)
						{
							num = TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout);
							if (num <= 0)
							{
								return false;
							}
						}
						Waiters++;
						if (IsSet)
						{
							Waiters--;
							return true;
						}
						try
						{
							if (!Monitor.Wait(m_lock, num))
							{
								return false;
							}
						}
						finally
						{
							Waiters--;
						}
					}
				}
			}
		}
		return true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (((uint)m_combinedState & 0x40000000u) != 0)
		{
			return;
		}
		m_combinedState |= 1073741824;
		if (!disposing)
		{
			return;
		}
		ManualResetEvent eventObj = m_eventObj;
		if (eventObj == null)
		{
			return;
		}
		lock (eventObj)
		{
			eventObj.Dispose();
			m_eventObj = null;
		}
	}

	private void ThrowIfDisposed()
	{
		if (((uint)m_combinedState & 0x40000000u) != 0)
		{
			throw new ObjectDisposedException(SR.ManualResetEventSlim_Disposed);
		}
	}

	private static void CancellationTokenCallback(object obj)
	{
		ManualResetEventSlim manualResetEventSlim = (ManualResetEventSlim)obj;
		lock (manualResetEventSlim.m_lock)
		{
			Monitor.PulseAll(manualResetEventSlim.m_lock);
		}
	}

	private void UpdateStateAtomically(int newBits, int updateBitsMask)
	{
		SpinWait spinWait = default(SpinWait);
		while (true)
		{
			int combinedState = m_combinedState;
			int value = (combinedState & ~updateBitsMask) | newBits;
			if (Interlocked.CompareExchange(ref m_combinedState, value, combinedState) == combinedState)
			{
				break;
			}
			spinWait.SpinOnce(-1);
		}
	}

	private static int ExtractStatePortionAndShiftRight(int state, int mask, int rightBitShiftCount)
	{
		return (state & mask) >>> rightBitShiftCount;
	}

	private static int ExtractStatePortion(int state, int mask)
	{
		return state & mask;
	}
}
