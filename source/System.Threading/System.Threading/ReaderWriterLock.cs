using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.Threading;

public sealed class ReaderWriterLock : CriticalFinalizerObject
{
	[Serializable]
	private sealed class ReaderWriterLockApplicationException : ApplicationException
	{
		public ReaderWriterLockApplicationException(int errorHResult, string message)
			: base(System.SR.Format(message, System.SR.Format(System.SR.ExceptionFromHResult, errorHResult)))
		{
			base.HResult = errorHResult;
		}

		public ReaderWriterLockApplicationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}

	private sealed class ThreadLocalLockEntry
	{
		[ThreadStatic]
		private static ThreadLocalLockEntry t_lockEntryHead;

		private long _lockID;

		private ThreadLocalLockEntry _next;

		public ushort _readerLevel;

		public bool IsFree => _readerLevel == 0;

		private ThreadLocalLockEntry(long lockID)
		{
			_lockID = lockID;
		}

		public static ThreadLocalLockEntry GetCurrent(long lockID)
		{
			ThreadLocalLockEntry threadLocalLockEntry = t_lockEntryHead;
			for (ThreadLocalLockEntry threadLocalLockEntry2 = threadLocalLockEntry; threadLocalLockEntry2 != null; threadLocalLockEntry2 = threadLocalLockEntry2._next)
			{
				if (threadLocalLockEntry2._lockID == lockID)
				{
					if (!threadLocalLockEntry2.IsFree)
					{
						return threadLocalLockEntry2;
					}
					return null;
				}
			}
			return null;
		}

		public static ThreadLocalLockEntry GetOrCreateCurrent(long lockID)
		{
			ThreadLocalLockEntry threadLocalLockEntry = t_lockEntryHead;
			if (threadLocalLockEntry != null)
			{
				if (threadLocalLockEntry._lockID == lockID)
				{
					return threadLocalLockEntry;
				}
				if (threadLocalLockEntry.IsFree)
				{
					threadLocalLockEntry._lockID = lockID;
					return threadLocalLockEntry;
				}
			}
			return GetOrCreateCurrentSlow(lockID, threadLocalLockEntry);
		}

		private static ThreadLocalLockEntry GetOrCreateCurrentSlow(long lockID, ThreadLocalLockEntry headEntry)
		{
			ThreadLocalLockEntry threadLocalLockEntry = null;
			ThreadLocalLockEntry threadLocalLockEntry2 = null;
			ThreadLocalLockEntry threadLocalLockEntry3 = null;
			if (headEntry != null)
			{
				if (headEntry.IsFree)
				{
					threadLocalLockEntry3 = headEntry;
				}
				ThreadLocalLockEntry threadLocalLockEntry4 = headEntry;
				for (ThreadLocalLockEntry next = headEntry._next; next != null; next = next._next)
				{
					if (next._lockID == lockID)
					{
						threadLocalLockEntry4._next = next._next;
						threadLocalLockEntry = next;
						break;
					}
					if (threadLocalLockEntry3 == null && next.IsFree)
					{
						threadLocalLockEntry2 = threadLocalLockEntry4;
						threadLocalLockEntry3 = next;
					}
					threadLocalLockEntry4 = next;
				}
			}
			if (threadLocalLockEntry == null)
			{
				if (threadLocalLockEntry3 != null)
				{
					threadLocalLockEntry3._lockID = lockID;
					if (threadLocalLockEntry2 == null)
					{
						return threadLocalLockEntry3;
					}
					threadLocalLockEntry2._next = threadLocalLockEntry3._next;
					threadLocalLockEntry = threadLocalLockEntry3;
				}
				else
				{
					threadLocalLockEntry = new ThreadLocalLockEntry(lockID);
				}
			}
			threadLocalLockEntry._next = headEntry;
			t_lockEntryHead = threadLocalLockEntry;
			return threadLocalLockEntry;
		}
	}

	private static readonly int DefaultSpinCount = ((Environment.ProcessorCount != 1) ? 500 : 0);

	private static long s_mostRecentLockID;

	private ManualResetEventSlim _readerEvent;

	private AutoResetEvent _writerEvent;

	private readonly long _lockID;

	private volatile int _state;

	private int _writerID = -1;

	private int _writerSeqNum = 1;

	private ushort _writerLevel;

	public bool IsReaderLockHeld
	{
		get
		{
			ThreadLocalLockEntry current = ThreadLocalLockEntry.GetCurrent(_lockID);
			if (current != null)
			{
				return current._readerLevel > 0;
			}
			return false;
		}
	}

	public bool IsWriterLockHeld => _writerID == GetCurrentThreadID();

	public int WriterSeqNum => _writerSeqNum;

	public ReaderWriterLock()
	{
		_lockID = Interlocked.Increment(ref s_mostRecentLockID);
	}

	public bool AnyWritersSince(int seqNum)
	{
		if (_writerID == GetCurrentThreadID())
		{
			seqNum++;
		}
		return (uint)_writerSeqNum > (uint)seqNum;
	}

	[UnsupportedOSPlatform("browser")]
	public void AcquireReaderLock(int millisecondsTimeout)
	{
		if (millisecondsTimeout < -1)
		{
			throw GetInvalidTimeoutException("millisecondsTimeout");
		}
		ThreadLocalLockEntry orCreateCurrent = ThreadLocalLockEntry.GetOrCreateCurrent(_lockID);
		if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
		{
			if (orCreateCurrent._readerLevel > 0)
			{
				if (orCreateCurrent._readerLevel == ushort.MaxValue)
				{
					throw new OverflowException(System.SR.Overflow_UInt16);
				}
				orCreateCurrent._readerLevel++;
				return;
			}
			if (_writerID == GetCurrentThreadID())
			{
				AcquireWriterLock(millisecondsTimeout);
				return;
			}
			int num = 0;
			int num2 = _state;
			do
			{
				int num3 = num2;
				if (num3 < 1023 || (((uint)num3 & 0x400u) != 0 && (num3 & 0x1000) == 0 && (num3 & 0x3FF) + ((num3 & 0x7FE000) >> 13) <= 1021))
				{
					num2 = Interlocked.CompareExchange(ref _state, num3 + 1, num3);
					if (num2 == num3)
					{
						break;
					}
					continue;
				}
				if ((num3 & 0x3FF) == 1023 || (num3 & 0x7FE000) == 8380416 || (num3 & 0xC00) == 1024)
				{
					int millisecondsTimeout2 = 100;
					if ((num3 & 0x3FF) == 1023 || (num3 & 0x7FE000) == 8380416)
					{
						millisecondsTimeout2 = 1000;
					}
					Thread.Sleep(millisecondsTimeout2);
					num = 0;
					num2 = _state;
					continue;
				}
				num++;
				if ((num3 & 0xC00) == 3072)
				{
					if (num > DefaultSpinCount)
					{
						Thread.Sleep(1);
						num = 0;
					}
					num2 = _state;
					continue;
				}
				if (num <= DefaultSpinCount)
				{
					num2 = _state;
					continue;
				}
				num2 = Interlocked.CompareExchange(ref _state, num3 + 8192, num3);
				if (num2 != num3)
				{
					continue;
				}
				int num4 = -8192;
				ManualResetEventSlim manualResetEventSlim = null;
				bool flag = false;
				try
				{
					manualResetEventSlim = GetOrCreateReaderEvent();
					flag = manualResetEventSlim.Wait(millisecondsTimeout);
					if (flag)
					{
						num4++;
					}
				}
				finally
				{
					num3 = Interlocked.Add(ref _state, num4) - num4;
					if (!flag && ((uint)num3 & 0x400u) != 0 && (num3 & 0x7FE000) == 8192)
					{
						if (manualResetEventSlim == null)
						{
							manualResetEventSlim = _readerEvent;
						}
						manualResetEventSlim.Wait();
						manualResetEventSlim.Reset();
						Interlocked.Add(ref _state, -1023);
						orCreateCurrent._readerLevel++;
						ReleaseReaderLock();
					}
				}
				if (!flag)
				{
					throw GetTimeoutException();
				}
				if ((num3 & 0x7FE000) == 8192)
				{
					manualResetEventSlim.Reset();
					Interlocked.Add(ref _state, -1024);
				}
				break;
			}
			while (YieldProcessor());
		}
		orCreateCurrent._readerLevel++;
	}

	[UnsupportedOSPlatform("browser")]
	public void AcquireReaderLock(TimeSpan timeout)
	{
		AcquireReaderLock(ToTimeoutMilliseconds(timeout));
	}

	public void AcquireWriterLock(int millisecondsTimeout)
	{
		if (millisecondsTimeout < -1)
		{
			throw GetInvalidTimeoutException("millisecondsTimeout");
		}
		int currentThreadID = GetCurrentThreadID();
		if (Interlocked.CompareExchange(ref _state, 4096, 0) != 0)
		{
			if (_writerID == currentThreadID)
			{
				if (_writerLevel == ushort.MaxValue)
				{
					throw new OverflowException(System.SR.Overflow_UInt16);
				}
				_writerLevel++;
				return;
			}
			int num = 0;
			int num2 = _state;
			do
			{
				int num3 = num2;
				if (num3 == 0 || num3 == 3072)
				{
					num2 = Interlocked.CompareExchange(ref _state, num3 + 4096, num3);
					if (num2 == num3)
					{
						break;
					}
					continue;
				}
				if ((num3 & -8388608) == -8388608)
				{
					Thread.Sleep(1000);
					num = 0;
					num2 = _state;
					continue;
				}
				num++;
				if ((num3 & 0xC00) == 3072)
				{
					if (num > DefaultSpinCount)
					{
						Thread.Sleep(1);
						num = 0;
					}
					num2 = _state;
					continue;
				}
				if (num <= DefaultSpinCount)
				{
					num2 = _state;
					continue;
				}
				num2 = Interlocked.CompareExchange(ref _state, num3 + 8388608, num3);
				if (num2 != num3)
				{
					continue;
				}
				int num4 = -8388608;
				AutoResetEvent autoResetEvent = null;
				bool flag = false;
				try
				{
					autoResetEvent = GetOrCreateWriterEvent();
					flag = autoResetEvent.WaitOne(millisecondsTimeout);
					if (flag)
					{
						num4 += 2048;
					}
				}
				finally
				{
					num3 = Interlocked.Add(ref _state, num4) - num4;
					if (!flag && ((uint)num3 & 0x800u) != 0 && (num3 & -8388608) == 8388608)
					{
						if (autoResetEvent == null)
						{
							autoResetEvent = _writerEvent;
						}
						while (true)
						{
							num3 = _state;
							if (((uint)num3 & 0x800u) != 0 && (num3 & -8388608) == 0)
							{
								if (autoResetEvent.WaitOne(10))
								{
									num4 = 2048;
									num3 = Interlocked.Add(ref _state, num4) - num4;
									_writerID = currentThreadID;
									_writerLevel = 1;
									ReleaseWriterLock();
									break;
								}
								continue;
							}
							break;
						}
					}
				}
				if (flag)
				{
					break;
				}
				throw GetTimeoutException();
			}
			while (YieldProcessor());
		}
		_writerID = currentThreadID;
		_writerLevel = 1;
		_writerSeqNum++;
	}

	public void AcquireWriterLock(TimeSpan timeout)
	{
		AcquireWriterLock(ToTimeoutMilliseconds(timeout));
	}

	public void ReleaseReaderLock()
	{
		if (_writerID == GetCurrentThreadID())
		{
			ReleaseWriterLock();
			return;
		}
		ThreadLocalLockEntry current = ThreadLocalLockEntry.GetCurrent(_lockID);
		if (current == null)
		{
			throw GetNotOwnerException();
		}
		current._readerLevel--;
		if (current._readerLevel > 0)
		{
			return;
		}
		AutoResetEvent autoResetEvent = null;
		ManualResetEventSlim manualResetEventSlim = null;
		int num = _state;
		bool flag;
		bool flag2;
		int num2;
		do
		{
			flag = false;
			flag2 = false;
			num2 = num;
			int num3 = -1;
			if ((num2 & 0x7FF) == 1)
			{
				flag = true;
				if (((uint)num2 & 0xFF800000u) != 0)
				{
					autoResetEvent = TryGetOrCreateWriterEvent();
					if (autoResetEvent == null)
					{
						Thread.Sleep(100);
						num = _state;
						num2 = 0;
						continue;
					}
					num3 += 2048;
				}
				else if (((uint)num2 & 0x7FE000u) != 0)
				{
					manualResetEventSlim = TryGetOrCreateReaderEvent();
					if (manualResetEventSlim == null)
					{
						Thread.Sleep(100);
						num = _state;
						num2 = 0;
						continue;
					}
					num3 += 1024;
				}
				else if (num2 == 1 && (_readerEvent != null || _writerEvent != null))
				{
					flag2 = true;
					num3 += 3072;
				}
			}
			num = Interlocked.CompareExchange(ref _state, num2 + num3, num2);
		}
		while (num != num2);
		if (flag)
		{
			if (((uint)num2 & 0xFF800000u) != 0)
			{
				autoResetEvent.Set();
			}
			else if (((uint)num2 & 0x7FE000u) != 0)
			{
				manualResetEventSlim.Set();
			}
			else if (flag2)
			{
				ReleaseEvents();
			}
		}
	}

	public void ReleaseWriterLock()
	{
		if (_writerID != GetCurrentThreadID())
		{
			throw GetNotOwnerException();
		}
		_writerLevel--;
		if (_writerLevel > 0)
		{
			return;
		}
		_writerID = -1;
		ManualResetEventSlim manualResetEventSlim = null;
		AutoResetEvent autoResetEvent = null;
		int num = _state;
		bool flag;
		int num2;
		do
		{
			flag = false;
			num2 = num;
			int num3 = -4096;
			if (((uint)num2 & 0x7FE000u) != 0)
			{
				manualResetEventSlim = TryGetOrCreateReaderEvent();
				if (manualResetEventSlim == null)
				{
					Thread.Sleep(100);
					num = _state;
					num2 = 0;
					continue;
				}
				num3 += 1024;
			}
			else if (((uint)num2 & 0xFF800000u) != 0)
			{
				autoResetEvent = TryGetOrCreateWriterEvent();
				if (autoResetEvent == null)
				{
					Thread.Sleep(100);
					num = _state;
					num2 = 0;
					continue;
				}
				num3 += 2048;
			}
			else if (num2 == 4096 && (_readerEvent != null || _writerEvent != null))
			{
				flag = true;
				num3 += 3072;
			}
			num = Interlocked.CompareExchange(ref _state, num2 + num3, num2);
		}
		while (num != num2);
		if (((uint)num2 & 0x7FE000u) != 0)
		{
			manualResetEventSlim.Set();
		}
		else if (((uint)num2 & 0xFF800000u) != 0)
		{
			autoResetEvent.Set();
		}
		else if (flag)
		{
			ReleaseEvents();
		}
	}

	[UnsupportedOSPlatform("browser")]
	public LockCookie UpgradeToWriterLock(int millisecondsTimeout)
	{
		if (millisecondsTimeout < -1)
		{
			throw GetInvalidTimeoutException("millisecondsTimeout");
		}
		LockCookie lockCookie = default(LockCookie);
		int num = (lockCookie._threadID = GetCurrentThreadID());
		if (_writerID == num)
		{
			lockCookie._flags = LockCookieFlags.Upgrade | LockCookieFlags.OwnedWriter;
			lockCookie._writerLevel = _writerLevel;
			AcquireWriterLock(millisecondsTimeout);
			return lockCookie;
		}
		ThreadLocalLockEntry current = ThreadLocalLockEntry.GetCurrent(_lockID);
		if (current == null)
		{
			lockCookie._flags = LockCookieFlags.Upgrade | LockCookieFlags.OwnedNone;
		}
		else
		{
			lockCookie._flags = LockCookieFlags.Upgrade | LockCookieFlags.OwnedReader;
			lockCookie._readerLevel = current._readerLevel;
			int num2 = Interlocked.CompareExchange(ref _state, 4096, 1);
			if (num2 == 1)
			{
				current._readerLevel = 0;
				_writerID = num;
				_writerLevel = 1;
				_writerSeqNum++;
				return lockCookie;
			}
			current._readerLevel = 1;
			ReleaseReaderLock();
		}
		bool flag = false;
		try
		{
			AcquireWriterLock(millisecondsTimeout);
			flag = true;
			return lockCookie;
		}
		finally
		{
			if (!flag)
			{
				LockCookieFlags flags = lockCookie._flags;
				lockCookie._flags = LockCookieFlags.Invalid;
				RecoverLock(ref lockCookie, flags & LockCookieFlags.OwnedReader);
			}
		}
	}

	[UnsupportedOSPlatform("browser")]
	public LockCookie UpgradeToWriterLock(TimeSpan timeout)
	{
		return UpgradeToWriterLock(ToTimeoutMilliseconds(timeout));
	}

	public void DowngradeFromWriterLock(ref LockCookie lockCookie)
	{
		int currentThreadID = GetCurrentThreadID();
		if (_writerID != currentThreadID)
		{
			throw GetNotOwnerException();
		}
		LockCookieFlags flags = lockCookie._flags;
		ushort writerLevel = lockCookie._writerLevel;
		if (((uint)flags & 0xFFF89FFFu) != 0 || lockCookie._threadID != currentThreadID || ((flags & (LockCookieFlags.OwnedNone | LockCookieFlags.OwnedWriter)) != 0 && _writerLevel <= writerLevel))
		{
			throw GetInvalidLockCookieException();
		}
		if ((flags & LockCookieFlags.OwnedReader) != 0)
		{
			ThreadLocalLockEntry orCreateCurrent = ThreadLocalLockEntry.GetOrCreateCurrent(_lockID);
			_writerID = -1;
			_writerLevel = 0;
			ManualResetEventSlim manualResetEventSlim = null;
			int num = _state;
			int num2;
			do
			{
				num2 = num;
				int num3 = -4095;
				if (((uint)num2 & 0x7FE000u) != 0)
				{
					manualResetEventSlim = TryGetOrCreateReaderEvent();
					if (manualResetEventSlim == null)
					{
						Thread.Sleep(100);
						num = _state;
						num2 = 0;
						continue;
					}
					num3 += 1024;
				}
				num = Interlocked.CompareExchange(ref _state, num2 + num3, num2);
			}
			while (num != num2);
			if (((uint)num2 & 0x7FE000u) != 0)
			{
				manualResetEventSlim.Set();
			}
			orCreateCurrent._readerLevel = lockCookie._readerLevel;
		}
		else if ((flags & (LockCookieFlags.OwnedNone | LockCookieFlags.OwnedWriter)) != 0)
		{
			if (writerLevel > 0)
			{
				_writerLevel = writerLevel;
			}
			else
			{
				if (_writerLevel != 1)
				{
					_writerLevel = 1;
				}
				ReleaseWriterLock();
			}
		}
		lockCookie._flags = LockCookieFlags.Invalid;
	}

	public LockCookie ReleaseLock()
	{
		LockCookie result = default(LockCookie);
		int num = (result._threadID = GetCurrentThreadID());
		if (_writerID == num)
		{
			result._flags = LockCookieFlags.Release | LockCookieFlags.OwnedWriter;
			result._writerLevel = _writerLevel;
			_writerLevel = 1;
			ReleaseWriterLock();
			return result;
		}
		ThreadLocalLockEntry current = ThreadLocalLockEntry.GetCurrent(_lockID);
		if (current == null)
		{
			result._flags = LockCookieFlags.Release | LockCookieFlags.OwnedNone;
			return result;
		}
		result._flags = LockCookieFlags.Release | LockCookieFlags.OwnedReader;
		result._readerLevel = current._readerLevel;
		current._readerLevel = 1;
		ReleaseReaderLock();
		return result;
	}

	[UnsupportedOSPlatform("browser")]
	public void RestoreLock(ref LockCookie lockCookie)
	{
		int currentThreadID = GetCurrentThreadID();
		if (lockCookie._threadID != currentThreadID)
		{
			throw GetInvalidLockCookieException();
		}
		if (_writerID == currentThreadID || ThreadLocalLockEntry.GetCurrent(_lockID) != null)
		{
			throw new SynchronizationLockException(System.SR.ReaderWriterLock_RestoreLockWithOwnedLocks);
		}
		LockCookieFlags flags = lockCookie._flags;
		if (((uint)flags & 0xFFF89FFFu) != 0)
		{
			throw GetInvalidLockCookieException();
		}
		if ((flags & LockCookieFlags.OwnedNone) == 0)
		{
			if ((flags & LockCookieFlags.OwnedWriter) != 0)
			{
				if (Interlocked.CompareExchange(ref _state, 4096, 0) == 0)
				{
					_writerID = currentThreadID;
					_writerLevel = lockCookie._writerLevel;
					_writerSeqNum++;
					goto IL_00e5;
				}
			}
			else if ((flags & LockCookieFlags.OwnedReader) != 0)
			{
				ThreadLocalLockEntry orCreateCurrent = ThreadLocalLockEntry.GetOrCreateCurrent(_lockID);
				int state = _state;
				if (state < 1023 && Interlocked.CompareExchange(ref _state, state + 1, state) == state)
				{
					orCreateCurrent._readerLevel = lockCookie._readerLevel;
					goto IL_00e5;
				}
			}
			RecoverLock(ref lockCookie, flags);
		}
		goto IL_00e5;
		IL_00e5:
		lockCookie._flags = LockCookieFlags.Invalid;
	}

	[UnsupportedOSPlatform("browser")]
	private void RecoverLock(ref LockCookie lockCookie, LockCookieFlags flags)
	{
		if ((flags & LockCookieFlags.OwnedWriter) != 0)
		{
			AcquireWriterLock(-1);
			_writerLevel = lockCookie._writerLevel;
		}
		else if ((flags & LockCookieFlags.OwnedReader) != 0)
		{
			AcquireReaderLock(-1);
			ThreadLocalLockEntry current = ThreadLocalLockEntry.GetCurrent(_lockID);
			current._readerLevel = lockCookie._readerLevel;
		}
	}

	private static int GetCurrentThreadID()
	{
		return Environment.CurrentManagedThreadId;
	}

	private static bool YieldProcessor()
	{
		Thread.SpinWait(1);
		return true;
	}

	private ManualResetEventSlim GetOrCreateReaderEvent()
	{
		ManualResetEventSlim readerEvent = _readerEvent;
		if (readerEvent != null)
		{
			return readerEvent;
		}
		readerEvent = new ManualResetEventSlim(initialState: false, 0);
		ManualResetEventSlim manualResetEventSlim = Interlocked.CompareExchange(ref _readerEvent, readerEvent, null);
		if (manualResetEventSlim == null)
		{
			return readerEvent;
		}
		readerEvent.Dispose();
		return manualResetEventSlim;
	}

	private AutoResetEvent GetOrCreateWriterEvent()
	{
		AutoResetEvent writerEvent = _writerEvent;
		if (writerEvent != null)
		{
			return writerEvent;
		}
		writerEvent = new AutoResetEvent(initialState: false);
		AutoResetEvent autoResetEvent = Interlocked.CompareExchange(ref _writerEvent, writerEvent, null);
		if (autoResetEvent == null)
		{
			return writerEvent;
		}
		writerEvent.Dispose();
		return autoResetEvent;
	}

	private ManualResetEventSlim TryGetOrCreateReaderEvent()
	{
		try
		{
			return GetOrCreateReaderEvent();
		}
		catch
		{
			return null;
		}
	}

	private AutoResetEvent TryGetOrCreateWriterEvent()
	{
		try
		{
			return GetOrCreateWriterEvent();
		}
		catch
		{
			return null;
		}
	}

	private void ReleaseEvents()
	{
		AutoResetEvent writerEvent = _writerEvent;
		_writerEvent = null;
		ManualResetEventSlim readerEvent = _readerEvent;
		_readerEvent = null;
		Interlocked.Add(ref _state, -3072);
		writerEvent?.Dispose();
		readerEvent?.Dispose();
	}

	private static ArgumentOutOfRangeException GetInvalidTimeoutException(string parameterName)
	{
		return new ArgumentOutOfRangeException(parameterName, System.SR.ArgumentOutOfRange_TimeoutMilliseconds);
	}

	private static int ToTimeoutMilliseconds(TimeSpan timeout)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1 || num > int.MaxValue)
		{
			throw GetInvalidTimeoutException("timeout");
		}
		return (int)num;
	}

	private static ApplicationException GetTimeoutException()
	{
		return new ReaderWriterLockApplicationException(-2147023436, System.SR.ReaderWriterLock_Timeout);
	}

	private static ApplicationException GetNotOwnerException()
	{
		return new ReaderWriterLockApplicationException(288, System.SR.ReaderWriterLock_NotOwner);
	}

	private static ApplicationException GetInvalidLockCookieException()
	{
		return new ReaderWriterLockApplicationException(-2147024809, System.SR.ReaderWriterLock_InvalidLockCookie);
	}
}
