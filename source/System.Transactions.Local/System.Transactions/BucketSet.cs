namespace System.Transactions;

internal sealed class BucketSet
{
	internal object nextSetWeak;

	internal BucketSet prevSet;

	private readonly TransactionTable _table;

	private readonly long _absoluteTimeout;

	internal Bucket headBucket;

	internal long AbsoluteTimeout => _absoluteTimeout;

	internal BucketSet(TransactionTable table, long absoluteTimeout)
	{
		headBucket = new Bucket(this);
		_table = table;
		_absoluteTimeout = absoluteTimeout;
	}

	internal void Add(InternalTransaction newTx)
	{
		while (!headBucket.Add(newTx))
		{
		}
	}

	internal void TimeoutTransactions()
	{
		Bucket bucket = headBucket;
		do
		{
			bucket.TimeoutTransactions();
			WeakReference nextBucketWeak = bucket.nextBucketWeak;
			bucket = ((nextBucketWeak == null) ? null : ((Bucket)nextBucketWeak.Target));
		}
		while (bucket != null);
	}
}
