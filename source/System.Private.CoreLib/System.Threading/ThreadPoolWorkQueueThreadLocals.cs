namespace System.Threading;

internal sealed class ThreadPoolWorkQueueThreadLocals
{
	[ThreadStatic]
	public static ThreadPoolWorkQueueThreadLocals threadLocals;

	public readonly ThreadPoolWorkQueue workQueue;

	public readonly ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue;

	public readonly Thread currentThread;

	public readonly object threadLocalCompletionCountObject;

	public readonly Random.XoshiroImpl random = new Random.XoshiroImpl();

	public ThreadPoolWorkQueueThreadLocals(ThreadPoolWorkQueue tpq)
	{
		workQueue = tpq;
		workStealingQueue = new ThreadPoolWorkQueue.WorkStealingQueue();
		ThreadPoolWorkQueue.WorkStealingQueueList.Add(workStealingQueue);
		currentThread = Thread.CurrentThread;
		threadLocalCompletionCountObject = ThreadPool.GetOrCreateThreadLocalCompletionCountObject();
	}

	public void TransferLocalWork()
	{
		while (true)
		{
			object obj = workStealingQueue.LocalPop();
			if (obj != null)
			{
				workQueue.Enqueue(obj, forceGlobal: true);
				continue;
			}
			break;
		}
	}

	~ThreadPoolWorkQueueThreadLocals()
	{
		if (workStealingQueue != null)
		{
			TransferLocalWork();
			ThreadPoolWorkQueue.WorkStealingQueueList.Remove(workStealingQueue);
		}
	}
}
