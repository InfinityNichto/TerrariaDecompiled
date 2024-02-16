using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace System.Threading;

[DebuggerDisplay("Current Count = {m_currentCount}")]
public class SemaphoreSlim : IDisposable
{
	private sealed class TaskNode : Task<bool>
	{
		internal TaskNode Prev;

		internal TaskNode Next;

		internal TaskNode()
			: base((object)null, TaskCreationOptions.RunContinuationsAsynchronously)
		{
		}
	}

	private readonly struct ConfiguredNoThrowAwaiter<T> : ICriticalNotifyCompletion, INotifyCompletion
	{
		private readonly Task<T> _task;

		public bool IsCompleted => _task.IsCompleted;

		public ConfiguredNoThrowAwaiter(Task<T> task)
		{
			_task = task;
		}

		public ConfiguredNoThrowAwaiter<T> GetAwaiter()
		{
			return this;
		}

		public void GetResult()
		{
		}

		public void UnsafeOnCompleted(Action continuation)
		{
			_task.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().UnsafeOnCompleted(continuation);
		}

		public void OnCompleted(Action continuation)
		{
			_task.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().OnCompleted(continuation);
		}
	}

	private volatile int m_currentCount;

	private readonly int m_maxCount;

	private int m_waitCount;

	private int m_countOfWaitersPulsedToWake;

	private readonly StrongBox<bool> m_lockObjAndDisposed;

	private volatile ManualResetEvent m_waitHandle;

	private TaskNode m_asyncHead;

	private TaskNode m_asyncTail;

	private static readonly Action<object> s_cancellationTokenCanceledEventHandler = CancellationTokenCanceledEventHandler;

	public int CurrentCount => m_currentCount;

	public WaitHandle AvailableWaitHandle
	{
		get
		{
			CheckDispose();
			if (m_waitHandle != null)
			{
				return m_waitHandle;
			}
			lock (m_lockObjAndDisposed)
			{
				if (m_waitHandle == null)
				{
					m_waitHandle = new ManualResetEvent(m_currentCount != 0);
				}
			}
			return m_waitHandle;
		}
	}

	public SemaphoreSlim(int initialCount)
		: this(initialCount, int.MaxValue)
	{
	}

	public SemaphoreSlim(int initialCount, int maxCount)
	{
		if (initialCount < 0 || initialCount > maxCount)
		{
			throw new ArgumentOutOfRangeException("initialCount", initialCount, SR.SemaphoreSlim_ctor_InitialCountWrong);
		}
		if (maxCount <= 0)
		{
			throw new ArgumentOutOfRangeException("maxCount", maxCount, SR.SemaphoreSlim_ctor_MaxCountWrong);
		}
		m_maxCount = maxCount;
		m_currentCount = initialCount;
		m_lockObjAndDisposed = new StrongBox<bool>();
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
			throw new ArgumentOutOfRangeException("timeout", timeout, SR.SemaphoreSlim_Wait_TimeoutWrong);
		}
		return Wait((int)timeout.TotalMilliseconds, CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1 || num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("timeout", timeout, SR.SemaphoreSlim_Wait_TimeoutWrong);
		}
		return Wait((int)timeout.TotalMilliseconds, cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(int millisecondsTimeout)
	{
		return Wait(millisecondsTimeout, CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
	{
		CheckDispose();
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", millisecondsTimeout, SR.SemaphoreSlim_Wait_TimeoutWrong);
		}
		cancellationToken.ThrowIfCancellationRequested();
		if (millisecondsTimeout == 0 && m_currentCount == 0)
		{
			return false;
		}
		uint startTime = 0u;
		if (millisecondsTimeout != -1 && millisecondsTimeout > 0)
		{
			startTime = TimeoutHelper.GetTime();
		}
		bool flag = false;
		Task<bool> task = null;
		bool lockTaken = false;
		CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.UnsafeRegister(s_cancellationTokenCanceledEventHandler, this);
		try
		{
			if (m_currentCount == 0)
			{
				int num = SpinWait.SpinCountforSpinBeforeWait * 4;
				SpinWait spinWait = default(SpinWait);
				while (spinWait.Count < num)
				{
					spinWait.SpinOnce(-1);
					if (m_currentCount != 0)
					{
						break;
					}
				}
			}
			Monitor.Enter(m_lockObjAndDisposed, ref lockTaken);
			m_waitCount++;
			if (m_asyncHead != null)
			{
				task = WaitAsync(millisecondsTimeout, cancellationToken);
			}
			else
			{
				OperationCanceledException ex = null;
				if (m_currentCount == 0)
				{
					if (millisecondsTimeout == 0)
					{
						return false;
					}
					try
					{
						flag = WaitUntilCountOrTimeout(millisecondsTimeout, startTime, cancellationToken);
					}
					catch (OperationCanceledException ex2)
					{
						ex = ex2;
					}
				}
				if (m_currentCount > 0)
				{
					flag = true;
					m_currentCount--;
				}
				else if (ex != null)
				{
					throw ex;
				}
				if (m_waitHandle != null && m_currentCount == 0)
				{
					m_waitHandle.Reset();
				}
			}
		}
		finally
		{
			if (lockTaken)
			{
				m_waitCount--;
				Monitor.Exit(m_lockObjAndDisposed);
			}
			cancellationTokenRegistration.Dispose();
		}
		return task?.GetAwaiter().GetResult() ?? flag;
	}

	[UnsupportedOSPlatform("browser")]
	private bool WaitUntilCountOrTimeout(int millisecondsTimeout, uint startTime, CancellationToken cancellationToken)
	{
		int num = -1;
		while (m_currentCount == 0)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (millisecondsTimeout != -1)
			{
				num = TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout);
				if (num <= 0)
				{
					return false;
				}
			}
			bool flag = Monitor.Wait(m_lockObjAndDisposed, num);
			if (m_countOfWaitersPulsedToWake != 0)
			{
				m_countOfWaitersPulsedToWake--;
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public Task WaitAsync()
	{
		return WaitAsync(-1, default(CancellationToken));
	}

	public Task WaitAsync(CancellationToken cancellationToken)
	{
		return WaitAsync(-1, cancellationToken);
	}

	public Task<bool> WaitAsync(int millisecondsTimeout)
	{
		return WaitAsync(millisecondsTimeout, default(CancellationToken));
	}

	public Task<bool> WaitAsync(TimeSpan timeout)
	{
		return WaitAsync(timeout, default(CancellationToken));
	}

	public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1 || num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("timeout", timeout, SR.SemaphoreSlim_Wait_TimeoutWrong);
		}
		return WaitAsync((int)timeout.TotalMilliseconds, cancellationToken);
	}

	public Task<bool> WaitAsync(int millisecondsTimeout, CancellationToken cancellationToken)
	{
		CheckDispose();
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", millisecondsTimeout, SR.SemaphoreSlim_Wait_TimeoutWrong);
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<bool>(cancellationToken);
		}
		lock (m_lockObjAndDisposed)
		{
			if (m_currentCount > 0)
			{
				m_currentCount--;
				if (m_waitHandle != null && m_currentCount == 0)
				{
					m_waitHandle.Reset();
				}
				return Task.FromResult(result: true);
			}
			if (millisecondsTimeout == 0)
			{
				return Task.FromResult(result: false);
			}
			TaskNode taskNode = CreateAndAddAsyncWaiter();
			return (millisecondsTimeout == -1 && !cancellationToken.CanBeCanceled) ? taskNode : WaitUntilCountOrTimeoutAsync(taskNode, millisecondsTimeout, cancellationToken);
		}
	}

	private TaskNode CreateAndAddAsyncWaiter()
	{
		TaskNode taskNode = new TaskNode();
		if (m_asyncHead == null)
		{
			m_asyncHead = taskNode;
			m_asyncTail = taskNode;
		}
		else
		{
			m_asyncTail.Next = taskNode;
			taskNode.Prev = m_asyncTail;
			m_asyncTail = taskNode;
		}
		return taskNode;
	}

	private bool RemoveAsyncWaiter(TaskNode task)
	{
		bool result = m_asyncHead == task || task.Prev != null;
		if (task.Next != null)
		{
			task.Next.Prev = task.Prev;
		}
		if (task.Prev != null)
		{
			task.Prev.Next = task.Next;
		}
		if (m_asyncHead == task)
		{
			m_asyncHead = task.Next;
		}
		if (m_asyncTail == task)
		{
			m_asyncTail = task.Prev;
		}
		task.Next = (task.Prev = null);
		return result;
	}

	private async Task<bool> WaitUntilCountOrTimeoutAsync(TaskNode asyncWaiter, int millisecondsTimeout, CancellationToken cancellationToken)
	{
		await new ConfiguredNoThrowAwaiter<bool>(asyncWaiter.WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken));
		if (cancellationToken.IsCancellationRequested)
		{
			await TaskScheduler.Default;
		}
		if (asyncWaiter.IsCompleted)
		{
			return true;
		}
		lock (m_lockObjAndDisposed)
		{
			if (RemoveAsyncWaiter(asyncWaiter))
			{
				cancellationToken.ThrowIfCancellationRequested();
				return false;
			}
		}
		return await asyncWaiter.ConfigureAwait(continueOnCapturedContext: false);
	}

	public int Release()
	{
		return Release(1);
	}

	public int Release(int releaseCount)
	{
		CheckDispose();
		if (releaseCount < 1)
		{
			throw new ArgumentOutOfRangeException("releaseCount", releaseCount, SR.SemaphoreSlim_Release_CountWrong);
		}
		int num;
		lock (m_lockObjAndDisposed)
		{
			int currentCount = m_currentCount;
			num = currentCount;
			if (m_maxCount - currentCount < releaseCount)
			{
				throw new SemaphoreFullException();
			}
			currentCount += releaseCount;
			int waitCount = m_waitCount;
			int num2 = Math.Min(currentCount, waitCount) - m_countOfWaitersPulsedToWake;
			if (num2 > 0)
			{
				if (num2 > releaseCount)
				{
					num2 = releaseCount;
				}
				m_countOfWaitersPulsedToWake += num2;
				for (int i = 0; i < num2; i++)
				{
					Monitor.Pulse(m_lockObjAndDisposed);
				}
			}
			if (m_asyncHead != null)
			{
				int num3 = currentCount - waitCount;
				while (num3 > 0 && m_asyncHead != null)
				{
					currentCount--;
					num3--;
					TaskNode asyncHead = m_asyncHead;
					RemoveAsyncWaiter(asyncHead);
					asyncHead.TrySetResult(result: true);
				}
			}
			m_currentCount = currentCount;
			if (m_waitHandle != null && num == 0 && currentCount > 0)
			{
				m_waitHandle.Set();
			}
		}
		return num;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			WaitHandle waitHandle = m_waitHandle;
			if (waitHandle != null)
			{
				waitHandle.Dispose();
				m_waitHandle = null;
			}
			m_lockObjAndDisposed.Value = true;
			m_asyncHead = null;
			m_asyncTail = null;
		}
	}

	private static void CancellationTokenCanceledEventHandler(object obj)
	{
		SemaphoreSlim semaphoreSlim = (SemaphoreSlim)obj;
		lock (semaphoreSlim.m_lockObjAndDisposed)
		{
			Monitor.PulseAll(semaphoreSlim.m_lockObjAndDisposed);
		}
	}

	private void CheckDispose()
	{
		if (m_lockObjAndDisposed.Value)
		{
			throw new ObjectDisposedException(null, SR.SemaphoreSlim_Disposed);
		}
	}
}
