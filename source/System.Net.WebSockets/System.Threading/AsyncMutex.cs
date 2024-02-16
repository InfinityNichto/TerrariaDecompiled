using System.Threading.Tasks;

namespace System.Threading;

internal sealed class AsyncMutex
{
	private sealed class Waiter : TaskCompletionSource
	{
		public AsyncMutex Owner { get; }

		public CancellationTokenRegistration CancellationRegistration { get; set; }

		public Waiter Next { get; set; }

		public Waiter Prev { get; set; }

		public Waiter(AsyncMutex owner)
			: base(TaskCreationOptions.RunContinuationsAsynchronously)
		{
			Owner = owner;
		}
	}

	private int _gate = 1;

	private bool _lockedSemaphoreFull = true;

	private Waiter _waitersTail;

	private object SyncObj => this;

	public Task EnterAsync(CancellationToken cancellationToken)
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			if (Interlocked.Decrement(ref _gate) < 0)
			{
				return Contended(cancellationToken);
			}
			return Task.CompletedTask;
		}
		return Task.FromCanceled(cancellationToken);
		Task Contended(CancellationToken cancellationToken)
		{
			Waiter waiter4 = new Waiter(this);
			waiter4.CancellationRegistration = cancellationToken.UnsafeRegister(delegate(object s, CancellationToken token)
			{
				OnCancellation(s, token);
			}, waiter4);
			lock (SyncObj)
			{
				if (!_lockedSemaphoreFull)
				{
					waiter4.CancellationRegistration.Unregister();
					_lockedSemaphoreFull = true;
					return Task.CompletedTask;
				}
				if (cancellationToken.IsCancellationRequested)
				{
					waiter4.TrySetCanceled(cancellationToken);
					return waiter4.Task;
				}
				if (_waitersTail == null)
				{
					Waiter next2 = (waiter4.Prev = waiter4);
					waiter4.Next = next2;
				}
				else
				{
					waiter4.Next = _waitersTail;
					waiter4.Prev = _waitersTail.Prev;
					Waiter prev = waiter4.Prev;
					Waiter next2 = (waiter4.Next.Prev = waiter4);
					prev.Next = next2;
				}
				_waitersTail = waiter4;
			}
			return waiter4.Task;
		}
		static void OnCancellation(object state, CancellationToken cancellationToken)
		{
			Waiter waiter = (Waiter)state;
			AsyncMutex owner = waiter.Owner;
			lock (owner.SyncObj)
			{
				if (waiter.Next != null)
				{
					Interlocked.Increment(ref owner._gate);
					if (waiter.Next == waiter)
					{
						owner._waitersTail = null;
					}
					else
					{
						waiter.Next.Prev = waiter.Prev;
						waiter.Prev.Next = waiter.Next;
						if (owner._waitersTail == waiter)
						{
							owner._waitersTail = waiter.Next;
						}
					}
					Waiter waiter2 = waiter;
					Waiter next = (waiter.Prev = null);
					waiter2.Next = next;
				}
				else
				{
					waiter = null;
				}
			}
			waiter?.TrySetCanceled(cancellationToken);
		}
	}

	public void Exit()
	{
		if (Interlocked.Increment(ref _gate) < 1)
		{
			Contended();
		}
		void Contended()
		{
			Waiter waiter;
			lock (SyncObj)
			{
				waiter = _waitersTail;
				if (waiter == null)
				{
					_lockedSemaphoreFull = false;
				}
				else
				{
					if (waiter.Next == waiter)
					{
						_waitersTail = null;
					}
					else
					{
						waiter = waiter.Prev;
						waiter.Next.Prev = waiter.Prev;
						waiter.Prev.Next = waiter.Next;
					}
					Waiter waiter2 = waiter;
					Waiter next = (waiter.Prev = null);
					waiter2.Next = next;
				}
			}
			if (waiter != null)
			{
				waiter.CancellationRegistration.Unregister();
				waiter.TrySetResult();
			}
		}
	}
}
