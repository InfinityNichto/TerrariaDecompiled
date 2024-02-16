using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading;

[DebuggerTypeProxy(typeof(SystemThreading_SpinLockDebugView))]
[DebuggerDisplay("IsHeld = {IsHeld}")]
public struct SpinLock
{
	internal sealed class SystemThreading_SpinLockDebugView
	{
		private SpinLock _spinLock;

		public bool? IsHeldByCurrentThread
		{
			get
			{
				try
				{
					return _spinLock.IsHeldByCurrentThread;
				}
				catch (InvalidOperationException)
				{
					return null;
				}
			}
		}

		public int? OwnerThreadID
		{
			get
			{
				if (_spinLock.IsThreadOwnerTrackingEnabled)
				{
					return _spinLock._owner;
				}
				return null;
			}
		}

		public bool IsHeld => _spinLock.IsHeld;

		public SystemThreading_SpinLockDebugView(SpinLock spinLock)
		{
			_spinLock = spinLock;
		}
	}

	private volatile int _owner;

	public bool IsHeld
	{
		get
		{
			if (IsThreadOwnerTrackingEnabled)
			{
				return _owner != 0;
			}
			return (_owner & 1) != 0;
		}
	}

	public bool IsHeldByCurrentThread
	{
		get
		{
			if (!IsThreadOwnerTrackingEnabled)
			{
				throw new InvalidOperationException(SR.SpinLock_IsHeldByCurrentThread);
			}
			return (_owner & 0x7FFFFFFF) == Environment.CurrentManagedThreadId;
		}
	}

	public bool IsThreadOwnerTrackingEnabled => (_owner & int.MinValue) == 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int CompareExchange(ref int location, int value, int comparand, ref bool success)
	{
		int num = Interlocked.CompareExchange(ref location, value, comparand);
		success = num == comparand;
		return num;
	}

	public SpinLock(bool enableThreadOwnerTracking)
	{
		_owner = 0;
		if (!enableThreadOwnerTracking)
		{
			_owner |= int.MinValue;
		}
	}

	public void Enter(ref bool lockTaken)
	{
		int owner = _owner;
		if (lockTaken || (owner & -2147483647) != int.MinValue || CompareExchange(ref _owner, owner | 1, owner, ref lockTaken) != owner)
		{
			ContinueTryEnter(-1, ref lockTaken);
		}
	}

	public void TryEnter(ref bool lockTaken)
	{
		int owner = _owner;
		if (((owner & int.MinValue) == 0) | lockTaken)
		{
			ContinueTryEnter(0, ref lockTaken);
		}
		else if (((uint)owner & (true ? 1u : 0u)) != 0)
		{
			lockTaken = false;
		}
		else
		{
			CompareExchange(ref _owner, owner | 1, owner, ref lockTaken);
		}
	}

	public void TryEnter(TimeSpan timeout, ref bool lockTaken)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1 || num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("timeout", timeout, SR.SpinLock_TryEnter_ArgumentOutOfRange);
		}
		TryEnter((int)timeout.TotalMilliseconds, ref lockTaken);
	}

	public void TryEnter(int millisecondsTimeout, ref bool lockTaken)
	{
		int owner = _owner;
		if (((millisecondsTimeout < -1) | lockTaken) || (owner & -2147483647) != int.MinValue || CompareExchange(ref _owner, owner | 1, owner, ref lockTaken) != owner)
		{
			ContinueTryEnter(millisecondsTimeout, ref lockTaken);
		}
	}

	private void ContinueTryEnter(int millisecondsTimeout, ref bool lockTaken)
	{
		if (lockTaken)
		{
			lockTaken = false;
			throw new ArgumentException(SR.SpinLock_TryReliableEnter_ArgumentException);
		}
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", millisecondsTimeout, SR.SpinLock_TryEnter_ArgumentOutOfRange);
		}
		uint startTime = 0u;
		if (millisecondsTimeout != -1 && millisecondsTimeout != 0)
		{
			startTime = TimeoutHelper.GetTime();
		}
		if (IsThreadOwnerTrackingEnabled)
		{
			ContinueTryEnterWithThreadTracking(millisecondsTimeout, startTime, ref lockTaken);
			return;
		}
		int num = int.MaxValue;
		int owner = _owner;
		if ((owner & 1) == 0)
		{
			if (CompareExchange(ref _owner, owner | 1, owner, ref lockTaken) == owner || millisecondsTimeout == 0)
			{
				return;
			}
		}
		else
		{
			if (millisecondsTimeout == 0)
			{
				return;
			}
			if ((owner & 0x7FFFFFFE) != 2147483646)
			{
				num = (Interlocked.Add(ref _owner, 2) & 0x7FFFFFFE) >> 1;
			}
		}
		SpinWait spinWait = default(SpinWait);
		if (num > Environment.ProcessorCount)
		{
			spinWait.Count = 10;
		}
		do
		{
			spinWait.SpinOnce(40);
			owner = _owner;
			if ((owner & 1) == 0)
			{
				int value = (((owner & 0x7FFFFFFE) == 0) ? (owner | 1) : ((owner - 2) | 1));
				if (CompareExchange(ref _owner, value, owner, ref lockTaken) == owner)
				{
					return;
				}
			}
		}
		while (spinWait.Count % 10 != 0 || millisecondsTimeout == -1 || TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) > 0);
		DecrementWaiters();
	}

	private void DecrementWaiters()
	{
		SpinWait spinWait = default(SpinWait);
		while (true)
		{
			int owner = _owner;
			if (((uint)owner & 0x7FFFFFFEu) != 0 && Interlocked.CompareExchange(ref _owner, owner - 2, owner) != owner)
			{
				spinWait.SpinOnce();
				continue;
			}
			break;
		}
	}

	private void ContinueTryEnterWithThreadTracking(int millisecondsTimeout, uint startTime, ref bool lockTaken)
	{
		int currentManagedThreadId = Environment.CurrentManagedThreadId;
		if (_owner == currentManagedThreadId)
		{
			throw new LockRecursionException(SR.SpinLock_TryEnter_LockRecursionException);
		}
		SpinWait spinWait = default(SpinWait);
		while (true)
		{
			spinWait.SpinOnce();
			if (_owner == 0 && CompareExchange(ref _owner, currentManagedThreadId, 0, ref lockTaken) == 0)
			{
				break;
			}
			switch (millisecondsTimeout)
			{
			case -1:
				continue;
			case 0:
				return;
			}
			if (spinWait.NextSpinWillYield && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0)
			{
				return;
			}
		}
	}

	public void Exit()
	{
		if ((_owner & int.MinValue) == 0)
		{
			ExitSlowPath(useMemoryBarrier: true);
		}
		else
		{
			Interlocked.Decrement(ref _owner);
		}
	}

	public void Exit(bool useMemoryBarrier)
	{
		int owner = _owner;
		if ((owner & int.MinValue) != 0 && !useMemoryBarrier)
		{
			_owner = owner & -2;
		}
		else
		{
			ExitSlowPath(useMemoryBarrier);
		}
	}

	private void ExitSlowPath(bool useMemoryBarrier)
	{
		bool flag = (_owner & int.MinValue) == 0;
		if (flag && !IsHeldByCurrentThread)
		{
			throw new SynchronizationLockException(SR.SpinLock_Exit_SynchronizationLockException);
		}
		if (useMemoryBarrier)
		{
			if (flag)
			{
				Interlocked.Exchange(ref _owner, 0);
			}
			else
			{
				Interlocked.Decrement(ref _owner);
			}
		}
		else if (flag)
		{
			_owner = 0;
		}
		else
		{
			int owner = _owner;
			_owner = owner & -2;
		}
	}
}
