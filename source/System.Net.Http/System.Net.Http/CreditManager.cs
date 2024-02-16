using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class CreditManager
{
	private readonly IHttpTrace _owner;

	private readonly string _name;

	private int _current;

	private bool _disposed;

	private CreditWaiter _waitersTail;

	public bool IsCreditAvailable => Volatile.Read(ref _current) > 0;

	private object SyncObject => this;

	public CreditManager(IHttpTrace owner, string name, int initialCredit)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			owner.Trace($"{name}. {"initialCredit"}={initialCredit}", ".ctor");
		}
		_owner = owner;
		_name = name;
		_current = initialCredit;
	}

	public ValueTask<int> RequestCreditAsync(int amount, CancellationToken cancellationToken)
	{
		lock (SyncObject)
		{
			int num = TryRequestCreditNoLock(amount);
			if (num > 0)
			{
				return new ValueTask<int>(num);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				_owner.Trace($"{_name}. requested={amount}, no credit available.", "RequestCreditAsync");
			}
			CreditWaiter creditWaiter = new CreditWaiter(cancellationToken);
			creditWaiter.Amount = amount;
			if (_waitersTail == null)
			{
				_waitersTail = (creditWaiter.Next = creditWaiter);
			}
			else
			{
				creditWaiter.Next = _waitersTail.Next;
				_waitersTail.Next = creditWaiter;
				_waitersTail = creditWaiter;
			}
			return creditWaiter.AsValueTask();
		}
	}

	public void AdjustCredit(int amount)
	{
		lock (SyncObject)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				_owner.Trace($"{_name}. {"amount"}={amount}, current={_current}", "AdjustCredit");
			}
			if (_disposed)
			{
				return;
			}
			checked
			{
				_current += amount;
			}
			while (_current > 0 && _waitersTail != null)
			{
				CreditWaiter next = _waitersTail.Next;
				int num = Math.Min(next.Amount, _current);
				if (next.Next == next)
				{
					_waitersTail = null;
				}
				else
				{
					_waitersTail.Next = next.Next;
				}
				next.Next = null;
				if (next.TrySetResult(num))
				{
					_current -= num;
				}
				next.Dispose();
			}
		}
	}

	public void Dispose()
	{
		lock (SyncObject)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			CreditWaiter creditWaiter = _waitersTail;
			if (creditWaiter != null)
			{
				do
				{
					CreditWaiter next = creditWaiter.Next;
					creditWaiter.Next = null;
					creditWaiter.Dispose();
					creditWaiter = next;
				}
				while (creditWaiter != _waitersTail);
				_waitersTail = null;
			}
		}
	}

	private int TryRequestCreditNoLock(int amount)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException($"{"CreditManager"}:{_owner.GetType().Name}:{_name}");
		}
		if (_current > 0)
		{
			int num = Math.Min(amount, _current);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				_owner.Trace($"{_name}. requested={amount}, current={_current}, granted={num}", "TryRequestCreditNoLock");
			}
			_current -= num;
			return num;
		}
		return 0;
	}
}
