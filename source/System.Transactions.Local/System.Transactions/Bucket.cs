using System.Threading;

namespace System.Transactions;

internal sealed class Bucket
{
	private bool _timedOut;

	private int _index;

	private readonly int _size;

	private readonly InternalTransaction[] _transactions;

	internal WeakReference nextBucketWeak;

	private Bucket _previous;

	private readonly BucketSet _owningSet;

	internal Bucket(BucketSet owningSet)
	{
		_timedOut = false;
		_index = -1;
		_size = 1024;
		_transactions = new InternalTransaction[_size];
		_owningSet = owningSet;
	}

	internal bool Add(InternalTransaction tx)
	{
		int num = Interlocked.Increment(ref _index);
		if (num < _size)
		{
			tx._tableBucket = this;
			tx._bucketIndex = num;
			Interlocked.MemoryBarrier();
			_transactions[num] = tx;
			if (_timedOut)
			{
				lock (tx)
				{
					tx.State.Timeout(tx);
				}
			}
			return true;
		}
		Bucket bucket = new Bucket(_owningSet);
		bucket.nextBucketWeak = new WeakReference(this);
		Bucket bucket2 = Interlocked.CompareExchange(ref _owningSet.headBucket, bucket, this);
		if (bucket2 == this)
		{
			_previous = bucket;
		}
		return false;
	}

	internal void Remove(InternalTransaction tx)
	{
		_transactions[tx._bucketIndex] = null;
	}

	internal void TimeoutTransactions()
	{
		int index = _index;
		_timedOut = true;
		Interlocked.MemoryBarrier();
		for (int i = 0; i <= index && i < _size; i++)
		{
			InternalTransaction internalTransaction = _transactions[i];
			if (internalTransaction != null)
			{
				lock (internalTransaction)
				{
					internalTransaction.State.Timeout(internalTransaction);
				}
			}
		}
	}
}
