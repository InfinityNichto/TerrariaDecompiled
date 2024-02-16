using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net;

internal static class TimerThread
{
	internal abstract class Queue
	{
		private readonly int _durationMilliseconds;

		internal int Duration => _durationMilliseconds;

		internal Queue(int durationMilliseconds)
		{
			_durationMilliseconds = durationMilliseconds;
		}

		internal abstract Timer CreateTimer(Callback callback, object context);
	}

	internal abstract class Timer : IDisposable
	{
		private readonly int _startTimeMilliseconds;

		private readonly int _durationMilliseconds;

		internal int StartTime => _startTimeMilliseconds;

		internal int Expiration => _startTimeMilliseconds + _durationMilliseconds;

		internal abstract bool HasExpired { get; }

		internal Timer(int durationMilliseconds)
		{
			_durationMilliseconds = durationMilliseconds;
			_startTimeMilliseconds = Environment.TickCount;
		}

		internal abstract bool Cancel();

		public void Dispose()
		{
			Cancel();
		}
	}

	internal delegate void Callback(Timer timer, int timeNoticed, object context);

	private enum TimerThreadState
	{
		Idle,
		Running,
		Stopped
	}

	private sealed class TimerQueue : Queue
	{
		private IntPtr _thisHandle;

		private readonly TimerNode _timers;

		internal TimerQueue(int durationMilliseconds)
			: base(durationMilliseconds)
		{
			_timers = new TimerNode();
			_timers.Next = _timers;
			_timers.Prev = _timers;
		}

		internal override Timer CreateTimer(Callback callback, object context)
		{
			TimerNode timerNode = new TimerNode(callback, context, base.Duration, _timers);
			bool flag = false;
			lock (_timers)
			{
				if (_timers.Next == _timers)
				{
					if (_thisHandle == IntPtr.Zero)
					{
						_thisHandle = (IntPtr)GCHandle.Alloc(this);
					}
					flag = true;
				}
				timerNode.Next = _timers;
				timerNode.Prev = _timers.Prev;
				_timers.Prev.Next = timerNode;
				_timers.Prev = timerNode;
			}
			if (flag)
			{
				Prod();
			}
			return timerNode;
		}

		internal bool Fire(out int nextExpiration)
		{
			TimerNode next;
			do
			{
				next = _timers.Next;
				if (next != _timers)
				{
					continue;
				}
				lock (_timers)
				{
					next = _timers.Next;
					if (next == _timers)
					{
						if (_thisHandle != IntPtr.Zero)
						{
							((GCHandle)_thisHandle).Free();
							_thisHandle = IntPtr.Zero;
						}
						nextExpiration = 0;
						return false;
					}
				}
			}
			while (next.Fire());
			nextExpiration = next.Expiration;
			return true;
		}
	}

	private sealed class InfiniteTimerQueue : Queue
	{
		internal InfiniteTimerQueue()
			: base(-1)
		{
		}

		internal override Timer CreateTimer(Callback callback, object context)
		{
			return new InfiniteTimer();
		}
	}

	private sealed class TimerNode : Timer
	{
		private enum TimerState
		{
			Ready,
			Fired,
			Cancelled,
			Sentinel
		}

		private TimerState _timerState;

		private Callback _callback;

		private object _context;

		private readonly object _queueLock;

		private TimerNode _next;

		private TimerNode _prev;

		internal override bool HasExpired => _timerState == TimerState.Fired;

		internal TimerNode Next
		{
			get
			{
				return _next;
			}
			set
			{
				_next = value;
			}
		}

		internal TimerNode Prev
		{
			get
			{
				return _prev;
			}
			set
			{
				_prev = value;
			}
		}

		internal TimerNode(Callback callback, object context, int durationMilliseconds, object queueLock)
			: base(durationMilliseconds)
		{
			if (callback != null)
			{
				_callback = callback;
				_context = context;
			}
			_timerState = TimerState.Ready;
			_queueLock = queueLock;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"TimerThreadTimer#{base.StartTime}", ".ctor");
			}
		}

		internal TimerNode()
			: base(0)
		{
			_timerState = TimerState.Sentinel;
		}

		internal override bool Cancel()
		{
			if (_timerState == TimerState.Ready)
			{
				lock (_queueLock)
				{
					if (_timerState == TimerState.Ready)
					{
						Next.Prev = Prev;
						Prev.Next = Next;
						Next = null;
						Prev = null;
						_callback = null;
						_context = null;
						_timerState = TimerState.Cancelled;
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, $"TimerThreadTimer#{base.StartTime} Cancel (success)", "Cancel");
						}
						return true;
					}
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"TimerThreadTimer#{base.StartTime} Cancel (failure)", "Cancel");
			}
			return false;
		}

		internal bool Fire()
		{
			if (_timerState == TimerState.Sentinel && System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "TimerQueue tried to Fire a Sentinel.", "Fire");
			}
			if (_timerState != 0)
			{
				return true;
			}
			int tickCount = Environment.TickCount;
			if (IsTickBetween(base.StartTime, base.Expiration, tickCount))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"TimerThreadTimer#{base.StartTime}::Fire() Not firing ({base.StartTime} <= {tickCount} < {base.Expiration})", "Fire");
				}
				return false;
			}
			bool flag = false;
			lock (_queueLock)
			{
				if (_timerState == TimerState.Ready)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"TimerThreadTimer#{base.StartTime}::Fire() Firing ({base.StartTime} <= {tickCount} >= " + base.Expiration + ")", "Fire");
					}
					_timerState = TimerState.Fired;
					Next.Prev = Prev;
					Prev.Next = Next;
					Next = null;
					Prev = null;
					flag = _callback != null;
				}
			}
			if (flag)
			{
				try
				{
					Callback callback = _callback;
					object context = _context;
					_callback = null;
					_context = null;
					callback(this, tickCount, context);
				}
				catch (Exception ex)
				{
					if (System.Net.ExceptionCheck.IsFatal(ex))
					{
						throw;
					}
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(this, $"exception in callback: {ex}", "Fire");
					}
				}
			}
			return true;
		}
	}

	private sealed class InfiniteTimer : Timer
	{
		private int _cancelled;

		internal override bool HasExpired => false;

		internal InfiniteTimer()
			: base(-1)
		{
		}

		internal override bool Cancel()
		{
			return Interlocked.Exchange(ref _cancelled, 1) == 0;
		}
	}

	private static readonly LinkedList<WeakReference> s_queues = new LinkedList<WeakReference>();

	private static readonly LinkedList<WeakReference> s_newQueues = new LinkedList<WeakReference>();

	private static int s_threadState = 0;

	private static readonly AutoResetEvent s_threadReadyEvent = new AutoResetEvent(initialState: false);

	private static readonly ManualResetEvent s_threadShutdownEvent = new ManualResetEvent(initialState: false);

	private static readonly WaitHandle[] s_threadEvents = new WaitHandle[2] { s_threadShutdownEvent, s_threadReadyEvent };

	private static int s_cacheScanIteration;

	private static readonly Hashtable s_queuesCache = new Hashtable();

	internal static Queue GetOrCreateQueue(int durationMilliseconds)
	{
		if (durationMilliseconds == -1)
		{
			return new InfiniteTimerQueue();
		}
		if (durationMilliseconds < 0)
		{
			throw new ArgumentOutOfRangeException("durationMilliseconds");
		}
		object key = durationMilliseconds;
		WeakReference weakReference = (WeakReference)s_queuesCache[key];
		TimerQueue timerQueue;
		if (weakReference == null || (timerQueue = (TimerQueue)weakReference.Target) == null)
		{
			lock (s_newQueues)
			{
				weakReference = (WeakReference)s_queuesCache[key];
				if (weakReference == null || (timerQueue = (TimerQueue)weakReference.Target) == null)
				{
					timerQueue = new TimerQueue(durationMilliseconds);
					weakReference = new WeakReference(timerQueue);
					s_newQueues.AddLast(weakReference);
					s_queuesCache[key] = weakReference;
					if (++s_cacheScanIteration % 32 == 0)
					{
						List<object> list = new List<object>();
						IDictionaryEnumerator enumerator = s_queuesCache.GetEnumerator();
						while (enumerator.MoveNext())
						{
							DictionaryEntry entry = enumerator.Entry;
							if (((WeakReference)entry.Value).Target == null)
							{
								list.Add(entry.Key);
							}
						}
						for (int i = 0; i < list.Count; i++)
						{
							s_queuesCache.Remove(list[i]);
						}
					}
				}
			}
		}
		return timerQueue;
	}

	private static void Prod()
	{
		s_threadReadyEvent.Set();
		if (Interlocked.CompareExchange(ref s_threadState, 1, 0) == 0)
		{
			Thread thread = new Thread(ThreadProc);
			thread.IsBackground = true;
			thread.Name = ".NET Network Timer";
			thread.Start();
		}
	}

	private static void ThreadProc()
	{
		lock (s_queues)
		{
			if (Interlocked.CompareExchange(ref s_threadState, 1, 1) != 1)
			{
				return;
			}
			bool flag = true;
			while (flag)
			{
				try
				{
					s_threadReadyEvent.Reset();
					while (true)
					{
						if (s_newQueues.Count > 0)
						{
							lock (s_newQueues)
							{
								for (LinkedListNode<WeakReference> first = s_newQueues.First; first != null; first = s_newQueues.First)
								{
									s_newQueues.Remove(first);
									s_queues.AddLast(first);
								}
							}
						}
						int tickCount = Environment.TickCount;
						int num = 0;
						bool flag2 = false;
						LinkedListNode<WeakReference> linkedListNode = s_queues.First;
						while (linkedListNode != null)
						{
							TimerQueue timerQueue = (TimerQueue)linkedListNode.Value.Target;
							if (timerQueue == null)
							{
								LinkedListNode<WeakReference> next = linkedListNode.Next;
								s_queues.Remove(linkedListNode);
								linkedListNode = next;
								continue;
							}
							if (timerQueue.Fire(out var nextExpiration) && (!flag2 || IsTickBetween(tickCount, num, nextExpiration)))
							{
								num = nextExpiration;
								flag2 = true;
							}
							linkedListNode = linkedListNode.Next;
						}
						int tickCount2 = Environment.TickCount;
						int num2 = (int)((!flag2) ? 30000 : (IsTickBetween(tickCount, num, tickCount2) ? (Math.Min((uint)(num - tickCount2), 2147483632u) + 15) : 0));
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(null, $"Waiting for {num2}ms", "ThreadProc");
						}
						int num3 = WaitHandle.WaitAny(s_threadEvents, num2, exitContext: false);
						if (num3 == 0)
						{
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(null, "Awoke, cause: Shutdown", "ThreadProc");
							}
							flag = false;
							break;
						}
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(null, FormattableStringFactory.Create("Awoke, cause {0}", (num3 == 258) ? "Timeout" : "Prod"), "ThreadProc");
						}
						if (num3 == 258 && !flag2)
						{
							Interlocked.CompareExchange(ref s_threadState, 0, 1);
							if (!s_threadReadyEvent.WaitOne(0, exitContext: false) || Interlocked.CompareExchange(ref s_threadState, 1, 0) != 0)
							{
								flag = false;
								break;
							}
						}
					}
				}
				catch (Exception ex)
				{
					if (System.Net.ExceptionCheck.IsFatal(ex))
					{
						throw;
					}
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(null, ex, "ThreadProc");
					}
					Thread.Sleep(1000);
				}
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, "Stop", "ThreadProc");
		}
	}

	private static bool IsTickBetween(int start, int end, int comparand)
	{
		return start <= comparand == end <= comparand != start <= end;
	}
}
