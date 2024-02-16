using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Internal;
using Internal.Runtime.CompilerServices;

namespace System.Threading;

internal sealed class ThreadPoolWorkQueue
{
	internal static class WorkStealingQueueList
	{
		private static volatile WorkStealingQueue[] _queues = new WorkStealingQueue[0];

		public static WorkStealingQueue[] Queues => _queues;

		public static void Add(WorkStealingQueue queue)
		{
			WorkStealingQueue[] queues;
			WorkStealingQueue[] array;
			do
			{
				queues = _queues;
				array = new WorkStealingQueue[queues.Length + 1];
				Array.Copy(queues, array, queues.Length);
				array[^1] = queue;
			}
			while (Interlocked.CompareExchange(ref _queues, array, queues) != queues);
		}

		public static void Remove(WorkStealingQueue queue)
		{
			WorkStealingQueue[] queues;
			WorkStealingQueue[] array;
			do
			{
				queues = _queues;
				if (queues.Length == 0)
				{
					break;
				}
				int num = Array.IndexOf(queues, queue);
				if (num == -1)
				{
					break;
				}
				array = new WorkStealingQueue[queues.Length - 1];
				if (num == 0)
				{
					Array.Copy(queues, 1, array, 0, array.Length);
					continue;
				}
				if (num == queues.Length - 1)
				{
					Array.Copy(queues, array, array.Length);
					continue;
				}
				Array.Copy(queues, array, num);
				Array.Copy(queues, num + 1, array, num, array.Length - num);
			}
			while (Interlocked.CompareExchange(ref _queues, array, queues) != queues);
		}
	}

	internal sealed class WorkStealingQueue
	{
		internal volatile object[] m_array = new object[32];

		private volatile int m_mask = 31;

		private volatile int m_headIndex;

		private volatile int m_tailIndex;

		private SpinLock m_foreignLock = new SpinLock(enableThreadOwnerTracking: false);

		public bool CanSteal => m_headIndex < m_tailIndex;

		public int Count
		{
			get
			{
				bool lockTaken = false;
				try
				{
					m_foreignLock.Enter(ref lockTaken);
					return Math.Max(0, m_tailIndex - m_headIndex);
				}
				finally
				{
					if (lockTaken)
					{
						m_foreignLock.Exit(useMemoryBarrier: false);
					}
				}
			}
		}

		public void LocalPush(object obj)
		{
			int num = m_tailIndex;
			if (num == int.MaxValue)
			{
				num = LocalPush_HandleTailOverflow();
			}
			if (num < m_headIndex + m_mask)
			{
				Volatile.Write(ref m_array[num & m_mask], obj);
				m_tailIndex = num + 1;
				return;
			}
			bool lockTaken = false;
			try
			{
				m_foreignLock.Enter(ref lockTaken);
				int headIndex = m_headIndex;
				int num2 = m_tailIndex - m_headIndex;
				if (num2 >= m_mask)
				{
					object[] array = new object[m_array.Length << 1];
					for (int i = 0; i < m_array.Length; i++)
					{
						array[i] = m_array[(i + headIndex) & m_mask];
					}
					m_array = array;
					m_headIndex = 0;
					num = (m_tailIndex = num2);
					m_mask = (m_mask << 1) | 1;
				}
				Volatile.Write(ref m_array[num & m_mask], obj);
				m_tailIndex = num + 1;
			}
			finally
			{
				if (lockTaken)
				{
					m_foreignLock.Exit(useMemoryBarrier: false);
				}
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private int LocalPush_HandleTailOverflow()
		{
			bool lockTaken = false;
			try
			{
				m_foreignLock.Enter(ref lockTaken);
				int num = m_tailIndex;
				if (num == int.MaxValue)
				{
					m_headIndex &= m_mask;
					num = (m_tailIndex &= m_mask);
				}
				return num;
			}
			finally
			{
				if (lockTaken)
				{
					m_foreignLock.Exit(useMemoryBarrier: true);
				}
			}
		}

		public bool LocalFindAndPop(object obj)
		{
			if (m_array[(m_tailIndex - 1) & m_mask] == obj)
			{
				object obj2 = LocalPop();
				return obj2 != null;
			}
			for (int num = m_tailIndex - 2; num >= m_headIndex; num--)
			{
				if (m_array[num & m_mask] == obj)
				{
					bool lockTaken = false;
					try
					{
						m_foreignLock.Enter(ref lockTaken);
						if (m_array[num & m_mask] == null)
						{
							return false;
						}
						Volatile.Write(ref m_array[num & m_mask], null);
						if (num == m_tailIndex)
						{
							m_tailIndex--;
						}
						else if (num == m_headIndex)
						{
							m_headIndex++;
						}
						return true;
					}
					finally
					{
						if (lockTaken)
						{
							m_foreignLock.Exit(useMemoryBarrier: false);
						}
					}
				}
			}
			return false;
		}

		public object LocalPop()
		{
			if (m_headIndex >= m_tailIndex)
			{
				return null;
			}
			return LocalPopCore();
		}

		private object LocalPopCore()
		{
			int num;
			object obj;
			while (true)
			{
				int tailIndex = m_tailIndex;
				if (m_headIndex >= tailIndex)
				{
					return null;
				}
				tailIndex--;
				Interlocked.Exchange(ref m_tailIndex, tailIndex);
				if (m_headIndex <= tailIndex)
				{
					num = tailIndex & m_mask;
					obj = Volatile.Read(ref m_array[num]);
					if (obj != null)
					{
						break;
					}
					continue;
				}
				bool lockTaken = false;
				try
				{
					m_foreignLock.Enter(ref lockTaken);
					if (m_headIndex <= tailIndex)
					{
						int num2 = tailIndex & m_mask;
						object obj2 = Volatile.Read(ref m_array[num2]);
						if (obj2 != null)
						{
							m_array[num2] = null;
							return obj2;
						}
						continue;
					}
					m_tailIndex = tailIndex + 1;
					return null;
				}
				finally
				{
					if (lockTaken)
					{
						m_foreignLock.Exit(useMemoryBarrier: false);
					}
				}
			}
			m_array[num] = null;
			return obj;
		}

		public object TrySteal(ref bool missedSteal)
		{
			while (CanSteal)
			{
				bool lockTaken = false;
				try
				{
					m_foreignLock.TryEnter(ref lockTaken);
					if (lockTaken)
					{
						int headIndex = m_headIndex;
						Interlocked.Exchange(ref m_headIndex, headIndex + 1);
						if (headIndex < m_tailIndex)
						{
							int num = headIndex & m_mask;
							object obj = Volatile.Read(ref m_array[num]);
							if (obj == null)
							{
								continue;
							}
							m_array[num] = null;
							return obj;
						}
						m_headIndex = headIndex;
					}
				}
				finally
				{
					if (lockTaken)
					{
						m_foreignLock.Exit(useMemoryBarrier: false);
					}
				}
				missedSteal = true;
				break;
			}
			return null;
		}
	}

	private struct CacheLineSeparated
	{
		private readonly PaddingFor32 pad1;

		public volatile int numOutstandingThreadRequests;

		private readonly PaddingFor32 pad2;
	}

	internal bool loggingEnabled;

	internal readonly ConcurrentQueue<object> workItems = new ConcurrentQueue<object>();

	internal readonly ConcurrentQueue<IThreadPoolWorkItem> timeSensitiveWorkQueue = (ThreadPool.SupportsTimeSensitiveWorkItems ? new ConcurrentQueue<IThreadPoolWorkItem>() : null);

	private CacheLineSeparated _separated;

	public static long LocalCount
	{
		get
		{
			long num = 0L;
			WorkStealingQueue[] queues = WorkStealingQueueList.Queues;
			foreach (WorkStealingQueue workStealingQueue in queues)
			{
				num += workStealingQueue.Count;
			}
			return num;
		}
	}

	public long GlobalCount => (ThreadPool.SupportsTimeSensitiveWorkItems ? timeSensitiveWorkQueue.Count : 0) + workItems.Count;

	public ThreadPoolWorkQueue()
	{
		RefreshLoggingEnabled();
	}

	public ThreadPoolWorkQueueThreadLocals GetOrCreateThreadLocals()
	{
		return ThreadPoolWorkQueueThreadLocals.threadLocals ?? CreateThreadLocals();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private ThreadPoolWorkQueueThreadLocals CreateThreadLocals()
	{
		return ThreadPoolWorkQueueThreadLocals.threadLocals = new ThreadPoolWorkQueueThreadLocals(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RefreshLoggingEnabled()
	{
		if (!FrameworkEventSource.Log.IsEnabled())
		{
			if (loggingEnabled)
			{
				loggingEnabled = false;
			}
		}
		else
		{
			RefreshLoggingEnabledFull();
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void RefreshLoggingEnabledFull()
	{
		loggingEnabled = FrameworkEventSource.Log.IsEnabled(EventLevel.Verbose, (EventKeywords)18L);
	}

	internal void EnsureThreadRequested()
	{
		int num = _separated.numOutstandingThreadRequests;
		while (num < Environment.ProcessorCount)
		{
			int num2 = Interlocked.CompareExchange(ref _separated.numOutstandingThreadRequests, num + 1, num);
			if (num2 == num)
			{
				ThreadPool.RequestWorkerThread();
				break;
			}
			num = num2;
		}
	}

	internal void MarkThreadRequestSatisfied()
	{
		int num = _separated.numOutstandingThreadRequests;
		while (num > 0)
		{
			int num2 = Interlocked.CompareExchange(ref _separated.numOutstandingThreadRequests, num - 1, num);
			if (num2 != num)
			{
				num = num2;
				continue;
			}
			break;
		}
	}

	public void EnqueueTimeSensitiveWorkItem(IThreadPoolWorkItem timeSensitiveWorkItem)
	{
		if (loggingEnabled && FrameworkEventSource.Log.IsEnabled())
		{
			FrameworkEventSource.Log.ThreadPoolEnqueueWorkObject(timeSensitiveWorkItem);
		}
		timeSensitiveWorkQueue.Enqueue(timeSensitiveWorkItem);
		EnsureThreadRequested();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public IThreadPoolWorkItem TryDequeueTimeSensitiveWorkItem()
	{
		IThreadPoolWorkItem result;
		bool flag = timeSensitiveWorkQueue.TryDequeue(out result);
		return result;
	}

	public void Enqueue(object callback, bool forceGlobal)
	{
		if (loggingEnabled && FrameworkEventSource.Log.IsEnabled())
		{
			FrameworkEventSource.Log.ThreadPoolEnqueueWorkObject(callback);
		}
		ThreadPoolWorkQueueThreadLocals threadPoolWorkQueueThreadLocals = null;
		if (!forceGlobal)
		{
			threadPoolWorkQueueThreadLocals = ThreadPoolWorkQueueThreadLocals.threadLocals;
		}
		if (threadPoolWorkQueueThreadLocals != null)
		{
			threadPoolWorkQueueThreadLocals.workStealingQueue.LocalPush(callback);
		}
		else
		{
			workItems.Enqueue(callback);
		}
		EnsureThreadRequested();
	}

	internal static bool LocalFindAndPop(object callback)
	{
		return ThreadPoolWorkQueueThreadLocals.threadLocals?.workStealingQueue.LocalFindAndPop(callback) ?? false;
	}

	public object Dequeue(ThreadPoolWorkQueueThreadLocals tl, ref bool missedSteal)
	{
		WorkStealingQueue workStealingQueue = tl.workStealingQueue;
		object result;
		if ((result = workStealingQueue.LocalPop()) == null && !workItems.TryDequeue(out result))
		{
			WorkStealingQueue[] queues = WorkStealingQueueList.Queues;
			int num = queues.Length;
			int num2 = num - 1;
			uint num3 = tl.random.NextUInt32() % (uint)num;
			while (num > 0)
			{
				num3 = ((num3 < num2) ? (num3 + 1) : 0u);
				WorkStealingQueue workStealingQueue2 = queues[num3];
				if (workStealingQueue2 != workStealingQueue && workStealingQueue2.CanSteal)
				{
					result = workStealingQueue2.TrySteal(ref missedSteal);
					if (result != null)
					{
						return result;
					}
				}
				num--;
			}
			if (ThreadPool.SupportsTimeSensitiveWorkItems)
			{
				result = TryDequeueTimeSensitiveWorkItem();
			}
		}
		return result;
	}

	internal static bool Dispatch()
	{
		ThreadPoolWorkQueue s_workQueue = ThreadPool.s_workQueue;
		s_workQueue.MarkThreadRequestSatisfied();
		s_workQueue.RefreshLoggingEnabled();
		bool flag = true;
		try
		{
			ThreadPoolWorkQueue threadPoolWorkQueue = s_workQueue;
			ThreadPoolWorkQueueThreadLocals orCreateThreadLocals = threadPoolWorkQueue.GetOrCreateThreadLocals();
			object threadLocalCompletionCountObject = orCreateThreadLocals.threadLocalCompletionCountObject;
			Thread currentThread = orCreateThreadLocals.currentThread;
			currentThread._executionContext = null;
			currentThread._synchronizationContext = null;
			int num = Environment.TickCount;
			object obj = null;
			while (true)
			{
				if (obj == null)
				{
					bool missedSteal = false;
					obj = threadPoolWorkQueue.Dequeue(orCreateThreadLocals, ref missedSteal);
					if (obj == null)
					{
						flag = missedSteal;
						return true;
					}
				}
				if (threadPoolWorkQueue.loggingEnabled && FrameworkEventSource.Log.IsEnabled())
				{
					FrameworkEventSource.Log.ThreadPoolDequeueWorkObject(obj);
				}
				threadPoolWorkQueue.EnsureThreadRequested();
				if (ThreadPool.EnableWorkerTracking)
				{
					DispatchWorkItemWithWorkerTracking(obj, currentThread);
				}
				else
				{
					DispatchWorkItem(obj, currentThread);
				}
				obj = null;
				ExecutionContext.ResetThreadPoolThread(currentThread);
				currentThread.ResetThreadPoolThread();
				int tickCount = Environment.TickCount;
				if (!ThreadPool.NotifyWorkItemComplete(threadLocalCompletionCountObject, tickCount))
				{
					orCreateThreadLocals.TransferLocalWork();
					return false;
				}
				if ((uint)(tickCount - num) >= 30u)
				{
					if (!ThreadPool.SupportsTimeSensitiveWorkItems)
					{
						break;
					}
					num = tickCount;
					threadPoolWorkQueue.RefreshLoggingEnabled();
					obj = threadPoolWorkQueue.TryDequeueTimeSensitiveWorkItem();
				}
			}
			return true;
		}
		finally
		{
			if (flag)
			{
				s_workQueue.EnsureThreadRequested();
			}
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void DispatchWorkItemWithWorkerTracking(object workItem, Thread currentThread)
	{
		bool flag = false;
		try
		{
			ThreadPool.ReportThreadStatus(isWorking: true);
			flag = true;
			DispatchWorkItem(workItem, currentThread);
		}
		finally
		{
			if (flag)
			{
				ThreadPool.ReportThreadStatus(isWorking: false);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void DispatchWorkItem(object workItem, Thread currentThread)
	{
		if (workItem is Task task)
		{
			task.ExecuteFromThreadPool(currentThread);
		}
		else
		{
			Unsafe.As<IThreadPoolWorkItem>(workItem).Execute();
		}
	}
}
