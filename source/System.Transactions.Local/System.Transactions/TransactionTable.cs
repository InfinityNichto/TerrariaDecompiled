using System.Threading;

namespace System.Transactions;

internal sealed class TransactionTable
{
	private readonly Timer _timer;

	private bool _timerEnabled;

	private readonly int _timerInterval;

	private long _ticks;

	private long _lastTimerTime;

	private readonly BucketSet _headBucketSet;

	private readonly CheapUnfairReaderWriterLock _rwLock;

	private long CurrentTime
	{
		get
		{
			if (_timerEnabled)
			{
				return _lastTimerTime;
			}
			return DateTime.UtcNow.Ticks;
		}
	}

	internal TransactionTable()
	{
		_timer = new Timer(ThreadTimer, null, -1, _timerInterval);
		_timerEnabled = false;
		_timerInterval = 512;
		_ticks = 0L;
		_headBucketSet = new BucketSet(this, long.MaxValue);
		_rwLock = new CheapUnfairReaderWriterLock();
	}

	internal long TimeoutTicks(TimeSpan timeout)
	{
		if (timeout != TimeSpan.Zero)
		{
			long num = (timeout.Ticks / 10000 >> 9) + _ticks;
			return num + 2;
		}
		return long.MaxValue;
	}

	internal TimeSpan RecalcTimeout(InternalTransaction tx)
	{
		return TimeSpan.FromMilliseconds((tx.AbsoluteTimeout - _ticks) * _timerInterval);
	}

	internal int Add(InternalTransaction txNew)
	{
		int num = 0;
		num = _rwLock.EnterReadLock();
		try
		{
			if (txNew.AbsoluteTimeout != long.MaxValue && !_timerEnabled)
			{
				if (!_timer.Change(_timerInterval, _timerInterval))
				{
					throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.UnexpectedTimerFailure, null);
				}
				_lastTimerTime = DateTime.UtcNow.Ticks;
				_timerEnabled = true;
			}
			txNew.CreationTime = CurrentTime;
			AddIter(txNew);
			return num;
		}
		finally
		{
			_rwLock.ExitReadLock();
		}
	}

	private void AddIter(InternalTransaction txNew)
	{
		BucketSet bucketSet = _headBucketSet;
		while (bucketSet.AbsoluteTimeout != txNew.AbsoluteTimeout)
		{
			BucketSet bucketSet2 = null;
			do
			{
				WeakReference weakReference = (WeakReference)bucketSet.nextSetWeak;
				BucketSet bucketSet3 = null;
				if (weakReference != null)
				{
					bucketSet3 = (BucketSet)weakReference.Target;
				}
				if (bucketSet3 == null)
				{
					BucketSet bucketSet4 = new BucketSet(this, txNew.AbsoluteTimeout);
					WeakReference value = new WeakReference(bucketSet4);
					WeakReference weakReference2 = (WeakReference)Interlocked.CompareExchange(ref bucketSet.nextSetWeak, value, weakReference);
					if (weakReference2 == weakReference)
					{
						bucketSet4.prevSet = bucketSet;
					}
				}
				else
				{
					bucketSet2 = bucketSet;
					bucketSet = bucketSet3;
				}
			}
			while (bucketSet.AbsoluteTimeout > txNew.AbsoluteTimeout);
			if (bucketSet.AbsoluteTimeout == txNew.AbsoluteTimeout)
			{
				continue;
			}
			BucketSet bucketSet5 = new BucketSet(this, txNew.AbsoluteTimeout);
			WeakReference value2 = new WeakReference(bucketSet5);
			bucketSet5.nextSetWeak = bucketSet2.nextSetWeak;
			WeakReference weakReference3 = (WeakReference)Interlocked.CompareExchange(ref bucketSet2.nextSetWeak, value2, bucketSet5.nextSetWeak);
			if (weakReference3 == bucketSet5.nextSetWeak)
			{
				if (weakReference3 != null)
				{
					BucketSet bucketSet6 = (BucketSet)weakReference3.Target;
					if (bucketSet6 != null)
					{
						bucketSet6.prevSet = bucketSet5;
					}
				}
				bucketSet5.prevSet = bucketSet2;
			}
			bucketSet = bucketSet2;
			bucketSet2 = null;
		}
		bucketSet.Add(txNew);
	}

	internal void Remove(InternalTransaction tx)
	{
		tx._tableBucket.Remove(tx);
		tx._tableBucket = null;
	}

	private void ThreadTimer(object state)
	{
		if (!_timerEnabled)
		{
			return;
		}
		_ticks++;
		_lastTimerTime = DateTime.UtcNow.Ticks;
		BucketSet bucketSet = null;
		BucketSet bucketSet2 = _headBucketSet;
		WeakReference weakReference = null;
		BucketSet bucketSet3 = null;
		weakReference = (WeakReference)bucketSet2.nextSetWeak;
		if (weakReference != null)
		{
			bucketSet3 = (BucketSet)weakReference.Target;
		}
		if (bucketSet3 == null)
		{
			_rwLock.EnterWriteLock();
			try
			{
				weakReference = (WeakReference)bucketSet2.nextSetWeak;
				if (weakReference != null)
				{
					bucketSet3 = (BucketSet)weakReference.Target;
				}
				if (bucketSet3 == null)
				{
					if (!_timer.Change(-1, -1))
					{
						throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.UnexpectedTimerFailure, null);
					}
					_timerEnabled = false;
					return;
				}
			}
			finally
			{
				_rwLock.ExitWriteLock();
			}
		}
		WeakReference weakReference2;
		while (true)
		{
			weakReference = (WeakReference)bucketSet2.nextSetWeak;
			if (weakReference == null)
			{
				return;
			}
			bucketSet3 = (BucketSet)weakReference.Target;
			if (bucketSet3 == null)
			{
				return;
			}
			bucketSet = bucketSet2;
			bucketSet2 = bucketSet3;
			if (bucketSet2.AbsoluteTimeout <= _ticks)
			{
				weakReference2 = (WeakReference)Interlocked.CompareExchange(ref bucketSet.nextSetWeak, null, weakReference);
				if (weakReference2 == weakReference)
				{
					break;
				}
				bucketSet2 = bucketSet;
			}
		}
		BucketSet bucketSet4 = null;
		do
		{
			bucketSet4 = ((weakReference2 == null) ? null : ((BucketSet)weakReference2.Target));
			if (bucketSet4 != null)
			{
				bucketSet4.TimeoutTransactions();
				weakReference2 = (WeakReference)bucketSet4.nextSetWeak;
			}
		}
		while (bucketSet4 != null);
	}
}
