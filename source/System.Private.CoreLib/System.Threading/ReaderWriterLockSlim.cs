using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Threading;

public class ReaderWriterLockSlim : IDisposable
{
	private struct TimeoutTracker
	{
		private readonly int _total;

		private readonly int _start;

		public int RemainingMilliseconds
		{
			get
			{
				if (_total == -1 || _total == 0)
				{
					return _total;
				}
				int num = Environment.TickCount - _start;
				if (num < 0 || num >= _total)
				{
					return 0;
				}
				return _total - num;
			}
		}

		public bool IsExpired => RemainingMilliseconds == 0;

		public TimeoutTracker(TimeSpan timeout)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1 || num > int.MaxValue)
			{
				throw new ArgumentOutOfRangeException("timeout");
			}
			_total = (int)num;
			if (_total != -1 && _total != 0)
			{
				_start = Environment.TickCount;
			}
			else
			{
				_start = 0;
			}
		}

		public TimeoutTracker(int millisecondsTimeout)
		{
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout");
			}
			_total = millisecondsTimeout;
			if (_total != -1 && _total != 0)
			{
				_start = Environment.TickCount;
			}
			else
			{
				_start = 0;
			}
		}
	}

	private struct SpinLock
	{
		private int _isLocked;

		private int _enterDeprioritizationState;

		private ushort EnterForEnterAnyReadDeprioritizedCount => (ushort)((uint)_enterDeprioritizationState >> 16);

		private ushort EnterForEnterAnyWriteDeprioritizedCount => (ushort)_enterDeprioritizationState;

		private static int GetEnterDeprioritizationStateChange(EnterSpinLockReason reason)
		{
			return (reason & EnterSpinLockReason.OperationMask) switch
			{
				EnterSpinLockReason.EnterAnyRead => 0, 
				EnterSpinLockReason.ExitAnyRead => 1, 
				EnterSpinLockReason.EnterWrite => 65536, 
				_ => 65537, 
			};
		}

		private bool IsEnterDeprioritized(EnterSpinLockReason reason)
		{
			return reason switch
			{
				EnterSpinLockReason.EnterAnyRead => EnterForEnterAnyReadDeprioritizedCount != 0, 
				EnterSpinLockReason.EnterWrite => EnterForEnterAnyWriteDeprioritizedCount != 0, 
				EnterSpinLockReason.UpgradeToWrite => EnterForEnterAnyWriteDeprioritizedCount > 1, 
				_ => false, 
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryEnter()
		{
			return Interlocked.CompareExchange(ref _isLocked, 1, 0) == 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Enter(EnterSpinLockReason reason)
		{
			if (!TryEnter())
			{
				EnterSpin(reason);
			}
		}

		private void EnterSpin(EnterSpinLockReason reason)
		{
			int enterDeprioritizationStateChange = GetEnterDeprioritizationStateChange(reason);
			if (enterDeprioritizationStateChange != 0)
			{
				Interlocked.Add(ref _enterDeprioritizationState, enterDeprioritizationStateChange);
			}
			int processorCount = Environment.ProcessorCount;
			int num = 0;
			while (true)
			{
				if (num < 10 && processorCount > 1)
				{
					Thread.SpinWait(20 * (num + 1));
				}
				else if (num < 15)
				{
					Thread.Sleep(0);
				}
				else
				{
					Thread.Sleep(1);
				}
				if (!IsEnterDeprioritized(reason))
				{
					if (_isLocked == 0 && TryEnter())
					{
						break;
					}
				}
				else if (num >= 20)
				{
					reason |= EnterSpinLockReason.Wait;
					num = -1;
				}
				num++;
			}
			if (enterDeprioritizationStateChange != 0)
			{
				Interlocked.Add(ref _enterDeprioritizationState, -enterDeprioritizationStateChange);
			}
		}

		public void Exit()
		{
			Volatile.Write(ref _isLocked, 0);
		}
	}

	[Flags]
	private enum WaiterStates : byte
	{
		None = 0,
		NoWaiters = 1,
		WriteWaiterSignaled = 2,
		UpgradeableReadWaiterSignaled = 4
	}

	private enum EnterSpinLockReason
	{
		EnterAnyRead = 0,
		ExitAnyRead = 1,
		EnterWrite = 2,
		UpgradeToWrite = 3,
		EnterRecursiveWrite = 4,
		ExitAnyWrite = 5,
		OperationMask = 7,
		Wait = 8
	}

	private enum EnterLockType
	{
		Read,
		UpgradeableRead,
		Write,
		UpgradeToWrite
	}

	private readonly bool _fIsReentrant;

	private SpinLock _spinLock;

	private uint _numWriteWaiters;

	private uint _numReadWaiters;

	private uint _numWriteUpgradeWaiters;

	private uint _numUpgradeWaiters;

	private WaiterStates _waiterStates;

	private int _upgradeLockOwnerId;

	private int _writeLockOwnerId;

	private EventWaitHandle _writeEvent;

	private EventWaitHandle _readEvent;

	private EventWaitHandle _upgradeEvent;

	private EventWaitHandle _waitUpgradeEvent;

	private static long s_nextLockID;

	private readonly long _lockID;

	[ThreadStatic]
	private static ReaderWriterCount t_rwc;

	private bool _fUpgradeThreadHoldingRead;

	private uint _owners;

	private bool _fDisposed;

	private bool HasNoWaiters
	{
		get
		{
			return (_waiterStates & WaiterStates.NoWaiters) != 0;
		}
		set
		{
			if (value)
			{
				_waiterStates |= WaiterStates.NoWaiters;
			}
			else
			{
				_waiterStates &= ~WaiterStates.NoWaiters;
			}
		}
	}

	public bool IsReadLockHeld
	{
		get
		{
			if (RecursiveReadCount > 0)
			{
				return true;
			}
			return false;
		}
	}

	public bool IsUpgradeableReadLockHeld
	{
		get
		{
			if (RecursiveUpgradeCount > 0)
			{
				return true;
			}
			return false;
		}
	}

	public bool IsWriteLockHeld
	{
		get
		{
			if (RecursiveWriteCount > 0)
			{
				return true;
			}
			return false;
		}
	}

	public LockRecursionPolicy RecursionPolicy
	{
		get
		{
			if (_fIsReentrant)
			{
				return LockRecursionPolicy.SupportsRecursion;
			}
			return LockRecursionPolicy.NoRecursion;
		}
	}

	public int CurrentReadCount
	{
		get
		{
			int numReaders = (int)GetNumReaders();
			if (_upgradeLockOwnerId != -1)
			{
				return numReaders - 1;
			}
			return numReaders;
		}
	}

	public int RecursiveReadCount
	{
		get
		{
			int result = 0;
			ReaderWriterCount threadRWCount = GetThreadRWCount(dontAllocate: true);
			if (threadRWCount != null)
			{
				result = threadRWCount.readercount;
			}
			return result;
		}
	}

	public int RecursiveUpgradeCount
	{
		get
		{
			if (_fIsReentrant)
			{
				int result = 0;
				ReaderWriterCount threadRWCount = GetThreadRWCount(dontAllocate: true);
				if (threadRWCount != null)
				{
					result = threadRWCount.upgradecount;
				}
				return result;
			}
			if (Environment.CurrentManagedThreadId == _upgradeLockOwnerId)
			{
				return 1;
			}
			return 0;
		}
	}

	public int RecursiveWriteCount
	{
		get
		{
			if (_fIsReentrant)
			{
				int result = 0;
				ReaderWriterCount threadRWCount = GetThreadRWCount(dontAllocate: true);
				if (threadRWCount != null)
				{
					result = threadRWCount.writercount;
				}
				return result;
			}
			if (Environment.CurrentManagedThreadId == _writeLockOwnerId)
			{
				return 1;
			}
			return 0;
		}
	}

	public int WaitingReadCount => (int)_numReadWaiters;

	public int WaitingUpgradeCount => (int)_numUpgradeWaiters;

	public int WaitingWriteCount => (int)_numWriteWaiters;

	private void InitializeThreadCounts()
	{
		_upgradeLockOwnerId = -1;
		_writeLockOwnerId = -1;
	}

	public ReaderWriterLockSlim()
		: this(LockRecursionPolicy.NoRecursion)
	{
	}

	public ReaderWriterLockSlim(LockRecursionPolicy recursionPolicy)
	{
		if (recursionPolicy == LockRecursionPolicy.SupportsRecursion)
		{
			_fIsReentrant = true;
		}
		InitializeThreadCounts();
		_waiterStates = WaiterStates.NoWaiters;
		_lockID = Interlocked.Increment(ref s_nextLockID);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsRWEntryEmpty(ReaderWriterCount rwc)
	{
		if (rwc.lockID == 0L)
		{
			return true;
		}
		if (rwc.readercount == 0 && rwc.writercount == 0 && rwc.upgradecount == 0)
		{
			return true;
		}
		return false;
	}

	private bool IsRwHashEntryChanged(ReaderWriterCount lrwc)
	{
		return lrwc.lockID != _lockID;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ReaderWriterCount GetThreadRWCount(bool dontAllocate)
	{
		ReaderWriterCount next = t_rwc;
		ReaderWriterCount readerWriterCount = null;
		while (next != null)
		{
			if (next.lockID == _lockID)
			{
				return next;
			}
			if (!dontAllocate && readerWriterCount == null && IsRWEntryEmpty(next))
			{
				readerWriterCount = next;
			}
			next = next.next;
		}
		if (dontAllocate)
		{
			return null;
		}
		if (readerWriterCount == null)
		{
			readerWriterCount = new ReaderWriterCount();
			readerWriterCount.next = t_rwc;
			t_rwc = readerWriterCount;
		}
		readerWriterCount.lockID = _lockID;
		return readerWriterCount;
	}

	public void EnterReadLock()
	{
		TryEnterReadLock(-1);
	}

	public bool TryEnterReadLock(TimeSpan timeout)
	{
		return TryEnterReadLock(new TimeoutTracker(timeout));
	}

	public bool TryEnterReadLock(int millisecondsTimeout)
	{
		return TryEnterReadLock(new TimeoutTracker(millisecondsTimeout));
	}

	private bool TryEnterReadLock(TimeoutTracker timeout)
	{
		return TryEnterReadLockCore(timeout);
	}

	private bool TryEnterReadLockCore(TimeoutTracker timeout)
	{
		if (_fDisposed)
		{
			throw new ObjectDisposedException(null);
		}
		int currentManagedThreadId = Environment.CurrentManagedThreadId;
		ReaderWriterCount threadRWCount;
		if (!_fIsReentrant)
		{
			if (currentManagedThreadId == _writeLockOwnerId)
			{
				throw new LockRecursionException(SR.LockRecursionException_ReadAfterWriteNotAllowed);
			}
			_spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
			threadRWCount = GetThreadRWCount(dontAllocate: false);
			if (threadRWCount.readercount > 0)
			{
				_spinLock.Exit();
				throw new LockRecursionException(SR.LockRecursionException_RecursiveReadNotAllowed);
			}
			if (currentManagedThreadId == _upgradeLockOwnerId)
			{
				threadRWCount.readercount++;
				_owners++;
				_spinLock.Exit();
				return true;
			}
		}
		else
		{
			_spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
			threadRWCount = GetThreadRWCount(dontAllocate: false);
			if (threadRWCount.readercount > 0)
			{
				threadRWCount.readercount++;
				_spinLock.Exit();
				return true;
			}
			if (currentManagedThreadId == _upgradeLockOwnerId)
			{
				threadRWCount.readercount++;
				_owners++;
				_spinLock.Exit();
				_fUpgradeThreadHoldingRead = true;
				return true;
			}
			if (currentManagedThreadId == _writeLockOwnerId)
			{
				threadRWCount.readercount++;
				_owners++;
				_spinLock.Exit();
				return true;
			}
		}
		bool flag = true;
		int num = 0;
		while (true)
		{
			if (_owners < 268435454)
			{
				_owners++;
				threadRWCount.readercount++;
				_spinLock.Exit();
				return flag;
			}
			if (timeout.IsExpired)
			{
				_spinLock.Exit();
				return false;
			}
			if (num < 20 && ShouldSpinForEnterAnyRead())
			{
				_spinLock.Exit();
				num++;
				SpinWait(num);
				_spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
				if (IsRwHashEntryChanged(threadRWCount))
				{
					threadRWCount = GetThreadRWCount(dontAllocate: false);
				}
			}
			else if (_readEvent == null)
			{
				LazyCreateEvent(ref _readEvent, EnterLockType.Read);
				if (IsRwHashEntryChanged(threadRWCount))
				{
					threadRWCount = GetThreadRWCount(dontAllocate: false);
				}
			}
			else
			{
				flag = WaitOnEvent(_readEvent, ref _numReadWaiters, timeout, EnterLockType.Read);
				if (!flag)
				{
					break;
				}
				if (IsRwHashEntryChanged(threadRWCount))
				{
					threadRWCount = GetThreadRWCount(dontAllocate: false);
				}
			}
		}
		return false;
	}

	public void EnterWriteLock()
	{
		TryEnterWriteLock(-1);
	}

	public bool TryEnterWriteLock(TimeSpan timeout)
	{
		return TryEnterWriteLock(new TimeoutTracker(timeout));
	}

	public bool TryEnterWriteLock(int millisecondsTimeout)
	{
		return TryEnterWriteLock(new TimeoutTracker(millisecondsTimeout));
	}

	private bool TryEnterWriteLock(TimeoutTracker timeout)
	{
		return TryEnterWriteLockCore(timeout);
	}

	private bool TryEnterWriteLockCore(TimeoutTracker timeout)
	{
		if (_fDisposed)
		{
			throw new ObjectDisposedException(null);
		}
		int currentManagedThreadId = Environment.CurrentManagedThreadId;
		bool flag = false;
		ReaderWriterCount threadRWCount;
		if (!_fIsReentrant)
		{
			if (currentManagedThreadId == _writeLockOwnerId)
			{
				throw new LockRecursionException(SR.LockRecursionException_RecursiveWriteNotAllowed);
			}
			EnterSpinLockReason reason;
			if (currentManagedThreadId == _upgradeLockOwnerId)
			{
				flag = true;
				reason = EnterSpinLockReason.UpgradeToWrite;
			}
			else
			{
				reason = EnterSpinLockReason.EnterWrite;
			}
			_spinLock.Enter(reason);
			threadRWCount = GetThreadRWCount(dontAllocate: true);
			if (threadRWCount != null && threadRWCount.readercount > 0)
			{
				_spinLock.Exit();
				throw new LockRecursionException(SR.LockRecursionException_WriteAfterReadNotAllowed);
			}
		}
		else
		{
			EnterSpinLockReason reason2 = ((currentManagedThreadId == _writeLockOwnerId) ? EnterSpinLockReason.EnterRecursiveWrite : ((currentManagedThreadId != _upgradeLockOwnerId) ? EnterSpinLockReason.EnterWrite : EnterSpinLockReason.UpgradeToWrite));
			_spinLock.Enter(reason2);
			threadRWCount = GetThreadRWCount(dontAllocate: false);
			if (currentManagedThreadId == _writeLockOwnerId)
			{
				threadRWCount.writercount++;
				_spinLock.Exit();
				return true;
			}
			if (currentManagedThreadId == _upgradeLockOwnerId)
			{
				flag = true;
			}
			else if (threadRWCount.readercount > 0)
			{
				_spinLock.Exit();
				throw new LockRecursionException(SR.LockRecursionException_WriteAfterReadNotAllowed);
			}
		}
		bool flag2 = true;
		int num = 0;
		while (true)
		{
			if (IsWriterAcquired())
			{
				SetWriterAcquired();
				break;
			}
			if (flag)
			{
				uint numReaders = GetNumReaders();
				if (numReaders == 1)
				{
					SetWriterAcquired();
					break;
				}
				if (numReaders == 2 && threadRWCount != null)
				{
					if (IsRwHashEntryChanged(threadRWCount))
					{
						threadRWCount = GetThreadRWCount(dontAllocate: false);
					}
					if (threadRWCount.readercount > 0)
					{
						SetWriterAcquired();
						break;
					}
				}
			}
			if (timeout.IsExpired)
			{
				_spinLock.Exit();
				return false;
			}
			if (num < 20 && ShouldSpinForEnterAnyWrite(flag))
			{
				_spinLock.Exit();
				num++;
				SpinWait(num);
				_spinLock.Enter(flag ? EnterSpinLockReason.UpgradeToWrite : EnterSpinLockReason.EnterWrite);
			}
			else if (flag)
			{
				if (_waitUpgradeEvent == null)
				{
					LazyCreateEvent(ref _waitUpgradeEvent, EnterLockType.UpgradeToWrite);
				}
				else if (!WaitOnEvent(_waitUpgradeEvent, ref _numWriteUpgradeWaiters, timeout, EnterLockType.UpgradeToWrite))
				{
					return false;
				}
			}
			else if (_writeEvent == null)
			{
				LazyCreateEvent(ref _writeEvent, EnterLockType.Write);
			}
			else if (!WaitOnEvent(_writeEvent, ref _numWriteWaiters, timeout, EnterLockType.Write))
			{
				return false;
			}
		}
		if (_fIsReentrant)
		{
			if (IsRwHashEntryChanged(threadRWCount))
			{
				threadRWCount = GetThreadRWCount(dontAllocate: false);
			}
			threadRWCount.writercount++;
		}
		_spinLock.Exit();
		_writeLockOwnerId = currentManagedThreadId;
		return true;
	}

	public void EnterUpgradeableReadLock()
	{
		TryEnterUpgradeableReadLock(-1);
	}

	public bool TryEnterUpgradeableReadLock(TimeSpan timeout)
	{
		return TryEnterUpgradeableReadLock(new TimeoutTracker(timeout));
	}

	public bool TryEnterUpgradeableReadLock(int millisecondsTimeout)
	{
		return TryEnterUpgradeableReadLock(new TimeoutTracker(millisecondsTimeout));
	}

	private bool TryEnterUpgradeableReadLock(TimeoutTracker timeout)
	{
		return TryEnterUpgradeableReadLockCore(timeout);
	}

	private bool TryEnterUpgradeableReadLockCore(TimeoutTracker timeout)
	{
		if (_fDisposed)
		{
			throw new ObjectDisposedException(null);
		}
		int currentManagedThreadId = Environment.CurrentManagedThreadId;
		ReaderWriterCount threadRWCount;
		if (!_fIsReentrant)
		{
			if (currentManagedThreadId == _upgradeLockOwnerId)
			{
				throw new LockRecursionException(SR.LockRecursionException_RecursiveUpgradeNotAllowed);
			}
			if (currentManagedThreadId == _writeLockOwnerId)
			{
				throw new LockRecursionException(SR.LockRecursionException_UpgradeAfterWriteNotAllowed);
			}
			_spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
			threadRWCount = GetThreadRWCount(dontAllocate: true);
			if (threadRWCount != null && threadRWCount.readercount > 0)
			{
				_spinLock.Exit();
				throw new LockRecursionException(SR.LockRecursionException_UpgradeAfterReadNotAllowed);
			}
		}
		else
		{
			_spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
			threadRWCount = GetThreadRWCount(dontAllocate: false);
			if (currentManagedThreadId == _upgradeLockOwnerId)
			{
				threadRWCount.upgradecount++;
				_spinLock.Exit();
				return true;
			}
			if (currentManagedThreadId == _writeLockOwnerId)
			{
				_owners++;
				_upgradeLockOwnerId = currentManagedThreadId;
				threadRWCount.upgradecount++;
				if (threadRWCount.readercount > 0)
				{
					_fUpgradeThreadHoldingRead = true;
				}
				_spinLock.Exit();
				return true;
			}
			if (threadRWCount.readercount > 0)
			{
				_spinLock.Exit();
				throw new LockRecursionException(SR.LockRecursionException_UpgradeAfterReadNotAllowed);
			}
		}
		bool flag = true;
		int num = 0;
		while (true)
		{
			if (_upgradeLockOwnerId == -1 && _owners < 268435454)
			{
				_owners++;
				_upgradeLockOwnerId = currentManagedThreadId;
				if (_fIsReentrant)
				{
					if (IsRwHashEntryChanged(threadRWCount))
					{
						threadRWCount = GetThreadRWCount(dontAllocate: false);
					}
					threadRWCount.upgradecount++;
				}
				break;
			}
			if (timeout.IsExpired)
			{
				_spinLock.Exit();
				return false;
			}
			if (num < 20 && ShouldSpinForEnterAnyRead())
			{
				_spinLock.Exit();
				num++;
				SpinWait(num);
				_spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
			}
			else if (_upgradeEvent == null)
			{
				LazyCreateEvent(ref _upgradeEvent, EnterLockType.UpgradeableRead);
			}
			else if (!WaitOnEvent(_upgradeEvent, ref _numUpgradeWaiters, timeout, EnterLockType.UpgradeableRead))
			{
				return false;
			}
		}
		_spinLock.Exit();
		return true;
	}

	public void ExitReadLock()
	{
		_spinLock.Enter(EnterSpinLockReason.ExitAnyRead);
		ReaderWriterCount threadRWCount = GetThreadRWCount(dontAllocate: true);
		if (threadRWCount == null || threadRWCount.readercount < 1)
		{
			_spinLock.Exit();
			throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedRead);
		}
		if (_fIsReentrant)
		{
			if (threadRWCount.readercount > 1)
			{
				threadRWCount.readercount--;
				_spinLock.Exit();
				return;
			}
			if (Environment.CurrentManagedThreadId == _upgradeLockOwnerId)
			{
				_fUpgradeThreadHoldingRead = false;
			}
		}
		_owners--;
		threadRWCount.readercount--;
		ExitAndWakeUpAppropriateWaiters();
	}

	public void ExitWriteLock()
	{
		if (!_fIsReentrant)
		{
			if (Environment.CurrentManagedThreadId != _writeLockOwnerId)
			{
				throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedWrite);
			}
			_spinLock.Enter(EnterSpinLockReason.ExitAnyWrite);
		}
		else
		{
			_spinLock.Enter(EnterSpinLockReason.ExitAnyWrite);
			ReaderWriterCount threadRWCount = GetThreadRWCount(dontAllocate: false);
			if (threadRWCount == null)
			{
				_spinLock.Exit();
				throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedWrite);
			}
			if (threadRWCount.writercount < 1)
			{
				_spinLock.Exit();
				throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedWrite);
			}
			threadRWCount.writercount--;
			if (threadRWCount.writercount > 0)
			{
				_spinLock.Exit();
				return;
			}
		}
		ClearWriterAcquired();
		_writeLockOwnerId = -1;
		ExitAndWakeUpAppropriateWaiters();
	}

	public void ExitUpgradeableReadLock()
	{
		if (!_fIsReentrant)
		{
			if (Environment.CurrentManagedThreadId != _upgradeLockOwnerId)
			{
				throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedUpgrade);
			}
			_spinLock.Enter(EnterSpinLockReason.ExitAnyRead);
		}
		else
		{
			_spinLock.Enter(EnterSpinLockReason.ExitAnyRead);
			ReaderWriterCount threadRWCount = GetThreadRWCount(dontAllocate: true);
			if (threadRWCount == null)
			{
				_spinLock.Exit();
				throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedUpgrade);
			}
			if (threadRWCount.upgradecount < 1)
			{
				_spinLock.Exit();
				throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedUpgrade);
			}
			threadRWCount.upgradecount--;
			if (threadRWCount.upgradecount > 0)
			{
				_spinLock.Exit();
				return;
			}
			_fUpgradeThreadHoldingRead = false;
		}
		_owners--;
		_upgradeLockOwnerId = -1;
		ExitAndWakeUpAppropriateWaiters();
	}

	private void LazyCreateEvent([NotNull] ref EventWaitHandle waitEvent, EnterLockType enterLockType)
	{
		_spinLock.Exit();
		EventWaitHandle eventWaitHandle = new EventWaitHandle(initialState: false, (enterLockType == EnterLockType.Read) ? EventResetMode.ManualReset : EventResetMode.AutoReset);
		EnterSpinLockReason reason;
		switch (enterLockType)
		{
		case EnterLockType.Read:
		case EnterLockType.UpgradeableRead:
			reason = EnterSpinLockReason.Wait;
			break;
		case EnterLockType.Write:
			reason = (EnterSpinLockReason)10;
			break;
		default:
			reason = (EnterSpinLockReason)11;
			break;
		}
		_spinLock.Enter(reason);
		if (waitEvent == null)
		{
			waitEvent = eventWaitHandle;
		}
		else
		{
			eventWaitHandle.Dispose();
		}
	}

	private bool WaitOnEvent(EventWaitHandle waitEvent, ref uint numWaiters, TimeoutTracker timeout, EnterLockType enterLockType)
	{
		WaiterStates waiterStates = WaiterStates.None;
		EnterSpinLockReason reason;
		switch (enterLockType)
		{
		case EnterLockType.UpgradeableRead:
			waiterStates = WaiterStates.UpgradeableReadWaiterSignaled;
			goto case EnterLockType.Read;
		case EnterLockType.Read:
			reason = EnterSpinLockReason.EnterAnyRead;
			break;
		case EnterLockType.Write:
			waiterStates = WaiterStates.WriteWaiterSignaled;
			reason = EnterSpinLockReason.EnterWrite;
			break;
		default:
			reason = EnterSpinLockReason.UpgradeToWrite;
			break;
		}
		if (waiterStates != 0 && (_waiterStates & waiterStates) != 0)
		{
			_waiterStates &= (WaiterStates)(byte)(~(int)waiterStates);
		}
		waitEvent.Reset();
		numWaiters++;
		HasNoWaiters = false;
		if (_numWriteWaiters == 1)
		{
			SetWritersWaiting();
		}
		if (_numWriteUpgradeWaiters == 1)
		{
			SetUpgraderWaiting();
		}
		bool flag = false;
		_spinLock.Exit();
		try
		{
			flag = waitEvent.WaitOne(timeout.RemainingMilliseconds);
		}
		finally
		{
			_spinLock.Enter(reason);
			numWaiters--;
			if (flag && waiterStates != 0 && (_waiterStates & waiterStates) != 0)
			{
				_waiterStates &= (WaiterStates)(byte)(~(int)waiterStates);
			}
			if (_numWriteWaiters == 0 && _numWriteUpgradeWaiters == 0 && _numUpgradeWaiters == 0 && _numReadWaiters == 0)
			{
				HasNoWaiters = true;
			}
			if (_numWriteWaiters == 0)
			{
				ClearWritersWaiting();
			}
			if (_numWriteUpgradeWaiters == 0)
			{
				ClearUpgraderWaiting();
			}
			if (!flag)
			{
				if (enterLockType >= EnterLockType.Write)
				{
					ExitAndWakeUpAppropriateReadWaiters();
				}
				else
				{
					_spinLock.Exit();
				}
			}
		}
		return flag;
	}

	private void ExitAndWakeUpAppropriateWaiters()
	{
		if (HasNoWaiters)
		{
			_spinLock.Exit();
		}
		else
		{
			ExitAndWakeUpAppropriateWaitersPreferringWriters();
		}
	}

	private void ExitAndWakeUpAppropriateWaitersPreferringWriters()
	{
		uint numReaders = GetNumReaders();
		if (_fIsReentrant && _numWriteUpgradeWaiters != 0 && _fUpgradeThreadHoldingRead && numReaders == 2)
		{
			_spinLock.Exit();
			_waitUpgradeEvent.Set();
		}
		else if (numReaders == 1 && _numWriteUpgradeWaiters != 0)
		{
			_spinLock.Exit();
			_waitUpgradeEvent.Set();
		}
		else if (numReaders == 0 && _numWriteWaiters != 0)
		{
			WaiterStates waiterStates = _waiterStates & WaiterStates.WriteWaiterSignaled;
			if (waiterStates == WaiterStates.None)
			{
				_waiterStates |= WaiterStates.WriteWaiterSignaled;
			}
			_spinLock.Exit();
			if (waiterStates == WaiterStates.None)
			{
				_writeEvent.Set();
			}
		}
		else
		{
			ExitAndWakeUpAppropriateReadWaiters();
		}
	}

	private void ExitAndWakeUpAppropriateReadWaiters()
	{
		if (_numWriteWaiters != 0 || _numWriteUpgradeWaiters != 0 || HasNoWaiters)
		{
			_spinLock.Exit();
			return;
		}
		bool flag = _numReadWaiters != 0;
		bool flag2 = _numUpgradeWaiters != 0 && _upgradeLockOwnerId == -1;
		if (flag2)
		{
			if ((_waiterStates & WaiterStates.UpgradeableReadWaiterSignaled) == 0)
			{
				_waiterStates |= WaiterStates.UpgradeableReadWaiterSignaled;
			}
			else
			{
				flag2 = false;
			}
		}
		_spinLock.Exit();
		if (flag)
		{
			_readEvent.Set();
		}
		if (flag2)
		{
			_upgradeEvent.Set();
		}
	}

	private bool IsWriterAcquired()
	{
		return (_owners & 0xBFFFFFFFu) == 0;
	}

	private void SetWriterAcquired()
	{
		_owners |= 2147483648u;
	}

	private void ClearWriterAcquired()
	{
		_owners &= 2147483647u;
	}

	private void SetWritersWaiting()
	{
		_owners |= 1073741824u;
	}

	private void ClearWritersWaiting()
	{
		_owners &= 3221225471u;
	}

	private void SetUpgraderWaiting()
	{
		_owners |= 536870912u;
	}

	private void ClearUpgraderWaiting()
	{
		_owners &= 3758096383u;
	}

	private uint GetNumReaders()
	{
		return _owners & 0xFFFFFFFu;
	}

	private bool ShouldSpinForEnterAnyRead()
	{
		if (!HasNoWaiters)
		{
			if (_numWriteWaiters == 0)
			{
				return _numWriteUpgradeWaiters == 0;
			}
			return false;
		}
		return true;
	}

	private bool ShouldSpinForEnterAnyWrite(bool isUpgradeToWrite)
	{
		if (!isUpgradeToWrite)
		{
			return _numWriteUpgradeWaiters == 0;
		}
		return true;
	}

	private static void SpinWait(int spinCount)
	{
		if (spinCount < 5 && Environment.ProcessorCount > 1)
		{
			Thread.SpinWait(20 * spinCount);
		}
		else
		{
			Thread.Sleep(0);
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	private void Dispose(bool disposing)
	{
		if (disposing && !_fDisposed)
		{
			if (WaitingReadCount > 0 || WaitingUpgradeCount > 0 || WaitingWriteCount > 0)
			{
				throw new SynchronizationLockException(SR.SynchronizationLockException_IncorrectDispose);
			}
			if (IsReadLockHeld || IsUpgradeableReadLockHeld || IsWriteLockHeld)
			{
				throw new SynchronizationLockException(SR.SynchronizationLockException_IncorrectDispose);
			}
			if (_writeEvent != null)
			{
				_writeEvent.Dispose();
				_writeEvent = null;
			}
			if (_readEvent != null)
			{
				_readEvent.Dispose();
				_readEvent = null;
			}
			if (_upgradeEvent != null)
			{
				_upgradeEvent.Dispose();
				_upgradeEvent = null;
			}
			if (_waitUpgradeEvent != null)
			{
				_waitUpgradeEvent.Dispose();
				_waitUpgradeEvent = null;
			}
			_fDisposed = true;
		}
	}
}
